from __future__ import annotations

import json
import hashlib
import math
import random
import re
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import joblib
import pandas as pd

from app.config import Settings
from app.repositories.postgres_repository import PostgresRepository, TemplateConfigRow
from app.schemas import (
    MlCacheResponse,
    MlEvaluationResponse,
    MlBenchmarkModeResult,
    MlBenchmarkResponse,
    MlStatusResponse,
    ModelArtifactStatusResponse,
    PredictSuccessRequest,
    PredictSuccessResponse,
    RecommendDebugCandidate,
    RecommendDebugResponse,
    RecommendedParameter,
    RecommendedPhase,
)
from app.services.intent_router import normalize_text
from app.services.synthetic_validation import validate_synthetic_record
from app.services.bigquery_ml_service import BigQueryMlService


EGG_PROFILES = {
    "chicken": {
        "phase_days": [(1, 7), (8, 18), (19, 21)],
        "temperature": [37.7, 37.5, 37.2],
        "humidity": [60.0, 60.0, 70.0],
    },
    "duck": {
        "phase_days": [(1, 10), (11, 25), (26, 28)],
        "temperature": [37.6, 37.4, 37.1],
        "humidity": [60.0, 62.0, 72.0],
    },
    "quail": {
        "phase_days": [(1, 6), (7, 14), (15, 18)],
        "temperature": [37.7, 37.5, 37.2],
        "humidity": [58.0, 60.0, 70.0],
    },
    "goose": {
        "phase_days": [(1, 10), (11, 27), (28, 30)],
        "temperature": [37.6, 37.4, 37.1],
        "humidity": [60.0, 62.0, 72.0],
    },
}


@dataclass
class RecommendationResult:
    answer: str
    phases: list[RecommendedPhase]
    template_source: str
    scoring_mode: str
    estimated_success_rate: float | None
    estimated_success_confidence: float | None
    model_artifact_version: int | None
    model_training_dataset_sha256: str | None
    validation_warnings: list[str]


@dataclass
class ParsedRecommendationRequest:
    egg_type: str
    total_eggs: int
    ambient_temperature: float | None
    ambient_humidity: float | None
    notes: str | None


class RecommendService:
    def __init__(
        self,
        settings: Settings,
        repository: PostgresRepository,
        bigquery_ml_service: BigQueryMlService | None = None,
    ):
        self._settings = settings
        self._repository = repository
        self._bigquery_ml_service = bigquery_ml_service
        self._model_path = Path(settings.model_dir) / "recommendation_model.joblib"
        self._feature_columns_path = Path(settings.model_dir) / "recommendation_features.json"
        self._synthetic_records_cache: tuple[int, list[dict]] | None = None
        self._db_reference_cache: dict[str, tuple[float, list[tuple[dict[str, float], float, float]]]] = {}
        self._prebuilt_model_cache: tuple[int, int, int, int, Any, list[str]] | None = None

    def recommend(self, message: str, user_context: dict | None) -> RecommendationResult:
        request = self._parse_request(message, user_context)
        templates, template_fetch_error = self._fetch_templates_safe()
        phases, template_source = self._select_template(request.egg_type, templates)
        if template_fetch_error:
            template_source = f"{template_source}:db_fallback"
        candidates = self._generate_candidates(phases, request)
        candidates.extend(self._load_synthetic_candidates(request))
        best_candidate, scoring_mode, ranking_score = self._score_candidates(candidates, request)
        hardware_limits = self._fetch_hardware_limits_safe(user_context)
        adjusted = self._apply_rules(best_candidate, hardware_limits, request.egg_type)
        validation_warnings = self._validate_recommended_config(adjusted)
        if validation_warnings:
            adjusted = self._apply_rules(self._default_phases(request.egg_type), hardware_limits, request.egg_type)
            fallback_warnings = self._validate_recommended_config(adjusted)
            validation_warnings = [
                "selected_candidate_invalid_fallback_applied",
                *validation_warnings,
                *(f"fallback_{warning}" for warning in fallback_warnings),
            ]
            scoring_mode = "heuristic"
            estimated_success_rate, estimated_success_confidence = None, None
        else:
            estimated_success_rate, estimated_success_confidence = self._estimated_success(
                adjusted,
                request,
                scoring_mode,
                ranking_score,
            )
        answer = self._build_answer(
            request,
            adjusted,
            template_source,
            scoring_mode,
            estimated_success_rate,
            estimated_success_confidence,
        )
        artifact_version, training_dataset_sha256 = self._prebuilt_trace() if scoring_mode == "prebuilt_model" else (None, None)
        return RecommendationResult(
            answer=answer,
            phases=adjusted,
            template_source=template_source,
            scoring_mode=scoring_mode,
            estimated_success_rate=estimated_success_rate,
            estimated_success_confidence=estimated_success_confidence,
            model_artifact_version=artifact_version,
            model_training_dataset_sha256=training_dataset_sha256,
            validation_warnings=validation_warnings,
        )

    def predict_success(self, payload: PredictSuccessRequest) -> PredictSuccessResponse:
        cloud_prediction = self._predict_success_with_bigquery(payload)
        if cloud_prediction is not None:
            prediction = round(cloud_prediction.success_rate, 4)
            return PredictSuccessResponse(
                egg_type=self._resolve_egg_type(normalize_text(payload.egg_type)),
                total_eggs=payload.total_eggs,
                predicted_success_rate=prediction,
                predicted_success_percent=round(prediction * 100, 2),
                confidence=None,
                prediction_mode="bigquery_ml",
                db_completed_seasons=0,
                synthetic_references=0,
                message="Dự đoán được tạo bởi model BigQuery ML đã train trên Google Cloud.",
            )

        egg_type = self._resolve_egg_type(normalize_text(payload.egg_type))
        request = ParsedRecommendationRequest(
            egg_type=egg_type,
            total_eggs=payload.total_eggs,
            ambient_temperature=_as_float(payload.ambient_temperature),
            ambient_humidity=_as_float(payload.ambient_humidity),
            notes=None,
        )
        templates, _ = self._fetch_templates_safe()
        phases, _ = self._select_template(egg_type, templates)
        hardware_limits = self._fetch_hardware_limits_safe(
            {"incubator_id": payload.incubator_id} if payload.incubator_id else None
        )
        candidate = self._apply_rules(phases, hardware_limits, egg_type)

        db_references = self._deduplicate_references(self._load_db_labeled_references(request))
        synthetic_references = self._deduplicate_references(self._load_synthetic_labeled_references(request))
        minimum = max(1, self._settings.knn_min_samples)
        if len(db_references) >= minimum:
            references = db_references
            mode = "db_knn"
        elif len(db_references) + len(synthetic_references) >= minimum:
            references = self._deduplicate_references(db_references + synthetic_references)
            mode = "blended_knn" if db_references else "synthetic_knn"
        else:
            return PredictSuccessResponse(
                egg_type=egg_type,
                total_eggs=payload.total_eggs,
                prediction_mode="insufficient_data",
                db_completed_seasons=len(db_references),
                synthetic_references=len(synthetic_references),
                message="Chưa đủ mùa ấp hoàn thành để dự đoán. Hệ thống sẽ tự dùng dữ liệu mới sau khi mùa ấp được cập nhật kết quả.",
            )

        features = self._candidate_to_distance_features(candidate, request)
        prediction, confidence = self._predict_success_from_references(features, references)
        prediction = round(min(1.0, max(0.0, prediction)), 4)
        return PredictSuccessResponse(
            egg_type=egg_type,
            total_eggs=payload.total_eggs,
            predicted_success_rate=prediction,
            predicted_success_percent=round(prediction * 100, 2),
            confidence=round(confidence, 4),
            prediction_mode=mode,
            db_completed_seasons=len(db_references),
            synthetic_references=len(synthetic_references),
            message=(
                "Dự đoán ưu tiên các mùa ấp hoàn thành trong DB."
                if mode == "db_knn"
                else "DB chưa đủ dữ liệu thật; dự đoán đang dùng dữ liệu synthetic làm cold-start."
            ),
        )

    def _predict_success_with_bigquery(self, payload: PredictSuccessRequest):
        if self._bigquery_ml_service is None or not self._bigquery_ml_service.is_enabled():
            return None
        try:
            return self._bigquery_ml_service.predict_success(payload)
        except Exception:
            return None

    def debug_recommend(self, message: str, user_context: dict | None) -> RecommendDebugResponse:
        request = self._parse_request(message, user_context)
        templates, template_fetch_error = self._fetch_templates_safe()
        phases, template_source = self._select_template(request.egg_type, templates)
        if template_fetch_error:
            template_source = f"{template_source}:db_fallback"
        candidates = self._generate_candidates(phases, request)
        candidates.extend(self._load_synthetic_candidates(request))
        scored_candidates, scoring_mode = self._score_all_candidates(candidates, request)
        hardware_limits = self._fetch_hardware_limits_safe(user_context)
        top_candidates = []
        for index, (score, candidate) in enumerate(scored_candidates[:3]):
            adjusted_candidate = self._apply_rules(candidate, hardware_limits, request.egg_type)
            candidate_warnings = self._validate_recommended_config(adjusted_candidate)
            top_candidates.append(
                RecommendDebugCandidate(
                    rank=index + 1,
                    score=round(score, 4),
                    phases=adjusted_candidate,
                    passed_output_validation=not candidate_warnings,
                    validation_warnings=candidate_warnings,
                )
            )
        best_candidate = top_candidates[0].phases if top_candidates else self._apply_rules(phases, egg_type=request.egg_type)
        best_score = scored_candidates[0][0] if scored_candidates else 0.0
        estimated_success_rate, estimated_success_confidence = self._estimated_success(
            best_candidate,
            request,
            scoring_mode,
            best_score,
        )
        db_references = self._load_db_labeled_references(request)
        synthetic_references = self._load_synthetic_labeled_references(request)
        effective_references = self._deduplicate_references(db_references + synthetic_references)
        artifact_version, training_dataset_sha256 = self._prebuilt_trace() if scoring_mode == "prebuilt_model" else (None, None)
        answer_preview = self._build_answer(
            request,
            best_candidate,
            template_source,
            scoring_mode,
            estimated_success_rate,
            estimated_success_confidence,
        )
        return RecommendDebugResponse(
            message=message,
            egg_type=request.egg_type,
            total_eggs=request.total_eggs,
            template_source=template_source,
            scoring_mode=scoring_mode,
            estimated_success_rate=estimated_success_rate,
            estimated_success_confidence=estimated_success_confidence,
            model_artifact_version=artifact_version,
            model_training_dataset_sha256=training_dataset_sha256,
            db_labeled_references=len(db_references),
            synthetic_labeled_references=len(synthetic_references),
            effective_unique_references=len(effective_references),
            answer_preview=answer_preview,
            top_candidates=top_candidates,
        )

    def ml_status(self, egg_type: str | None = None) -> MlStatusResponse:
        resolved_egg_type = self._resolve_egg_type(normalize_text(egg_type or self._settings.default_egg_type))
        request = ParsedRecommendationRequest(
            egg_type=resolved_egg_type,
            total_eggs=100,
            ambient_temperature=None,
            ambient_humidity=None,
            notes=None,
        )
        db_count = len(self._load_db_labeled_references(request))
        synthetic_count = len(self._load_synthetic_labeled_references(request))
        total = db_count + synthetic_count
        effective = len(
            self._deduplicate_references(
                self._load_db_labeled_references(request) + self._load_synthetic_labeled_references(request)
            )
        )
        minimum = max(1, self._settings.knn_min_samples)
        return MlStatusResponse(
            egg_type=resolved_egg_type,
            db_labeled_references=db_count,
            synthetic_labeled_references=synthetic_count,
            total_labeled_references=total,
            effective_unique_references=effective,
            minimum_references=minimum,
            ready_for_knn=effective >= minimum,
            db_reference_weight=self._settings.knn_db_reference_weight,
            synthetic_reference_weight=self._settings.knn_synthetic_reference_weight,
            synthetic_label_gemini_weight=self._settings.synthetic_label_gemini_weight,
            max_synthetic_references=self._settings.knn_max_synthetic_references,
            scoring_precedence=["db_knn", "blended_or_synthetic_knn", "heuristic"],
        )

    def evaluate_knn(self, egg_type: str | None = None) -> MlEvaluationResponse:
        resolved_egg_type = self._resolve_egg_type(normalize_text(egg_type or self._settings.default_egg_type))
        request = ParsedRecommendationRequest(
            egg_type=resolved_egg_type,
            total_eggs=100,
            ambient_temperature=None,
            ambient_humidity=None,
            notes=None,
        )
        references = self._load_db_labeled_references(request) + self._load_synthetic_labeled_references(request)
        references = self._deduplicate_references(references)
        if len(references) < 2:
            return MlEvaluationResponse(
                egg_type=resolved_egg_type,
                sample_count=len(references),
                mae=None,
                baseline_mae=None,
                mean_confidence=None,
                evaluation_mode="leave_one_out",
            )

        labels = [success_rate for _, success_rate, _ in references]
        baseline = sum(labels) / len(labels)
        errors: list[float] = []
        confidences: list[float] = []
        for index, (features, actual_success_rate, _) in enumerate(references):
            other_references = references[:index] + references[index + 1 :]
            prediction, confidence = self._predict_success_from_references(features, other_references)
            errors.append(abs(prediction - actual_success_rate))
            confidences.append(confidence)
        return MlEvaluationResponse(
            egg_type=resolved_egg_type,
            sample_count=len(references),
            mae=round(sum(errors) / len(errors), 4),
            baseline_mae=round(sum(abs(label - baseline) for label in labels) / len(labels), 4),
            mean_confidence=round(sum(confidences) / len(confidences), 4),
            evaluation_mode="leave_one_out",
        )

    def clear_ml_cache(self) -> MlCacheResponse:
        db_groups = len(self._db_reference_cache)
        synthetic_cached = self._synthetic_records_cache is not None
        model_cached = self._prebuilt_model_cache is not None
        self._db_reference_cache.clear()
        self._synthetic_records_cache = None
        self._prebuilt_model_cache = None
        return MlCacheResponse(
            cleared_db_reference_groups=db_groups,
            cleared_synthetic_cache=synthetic_cached,
            cleared_prebuilt_model_cache=model_cached,
        )

    def model_artifact_status(self) -> ModelArtifactStatusResponse:
        metrics_path = Path(self._settings.model_dir) / "recommendation_metrics.json"
        manifest_path = Path(self._settings.model_dir) / "recommendation_manifest.json"
        loaded = self._load_prebuilt_model() if self._model_path.exists() and self._feature_columns_path.exists() else None
        metrics = None
        manifest = None
        if metrics_path.exists():
            try:
                parsed = json.loads(metrics_path.read_text(encoding="utf-8"))
                metrics = parsed if isinstance(parsed, dict) else None
            except (OSError, json.JSONDecodeError):
                metrics = None
        if manifest_path.exists():
            try:
                parsed = json.loads(manifest_path.read_text(encoding="utf-8"))
                manifest = parsed if isinstance(parsed, dict) else None
            except (OSError, json.JSONDecodeError):
                manifest = None
        schema_hash_matches = None
        artifact_mae = None
        baseline_mae = None
        parsed_feature_columns: list[str] = []
        if self._feature_columns_path.exists():
            try:
                parsed = json.loads(self._feature_columns_path.read_text(encoding="utf-8"))
                parsed_feature_columns = parsed if isinstance(parsed, list) else []
            except (OSError, json.JSONDecodeError):
                parsed_feature_columns = []
        if parsed_feature_columns and manifest:
            schema_json = json.dumps(parsed_feature_columns, ensure_ascii=False, separators=(",", ":"))
            schema_hash = hashlib.sha256(schema_json.encode("utf-8")).hexdigest()
            schema_hash_matches = schema_hash == manifest.get("feature_schema_sha256")
        if metrics:
            artifact_mae = _as_float(metrics.get("mae"))
            baseline_mae = _as_float(metrics.get("baseline_mae"))
        supported_egg_types = set(manifest.get("supported_egg_types") or []) if manifest else set()
        validation_egg_types = set(metrics.get("validation_egg_types") or []) if metrics else set()
        validation_coverage_matches = bool(supported_egg_types) and validation_egg_types == supported_egg_types
        per_egg_type_mae = metrics.get("per_egg_type_mae") if metrics else None
        per_egg_type_mae_valid = (
            isinstance(per_egg_type_mae, dict)
            and set(per_egg_type_mae) == supported_egg_types
            and all(
                (value := _as_float(mae)) is not None and value <= self._settings.prebuilt_max_mae
                for mae in per_egg_type_mae.values()
            )
        )
        quality_gate_passed = (
            artifact_mae is not None and artifact_mae <= self._settings.prebuilt_max_mae
            if self._settings.prebuilt_require_metrics
            else artifact_mae is None or artifact_mae <= self._settings.prebuilt_max_mae
        )
        if self._settings.prebuilt_require_baseline_improvement:
            quality_gate_passed = (
                quality_gate_passed
                and artifact_mae is not None
                and baseline_mae is not None
                and artifact_mae < baseline_mae
            )
        quality_gate_passed = (
            quality_gate_passed and validation_coverage_matches and per_egg_type_mae_valid and loaded is not None
        )
        training_dataset_sha256 = manifest.get("training_dataset_sha256") if manifest else None
        current_export_sha256 = None
        export_manifest_path = Path(self._settings.ml_export_path).with_suffix(".manifest.json")
        if export_manifest_path.exists():
            try:
                export_manifest = json.loads(export_manifest_path.read_text(encoding="utf-8"))
                if isinstance(export_manifest, dict):
                    current_export_sha256 = export_manifest.get("dataset_sha256")
            except (OSError, json.JSONDecodeError):
                current_export_sha256 = None
        matches_current_export = (
            training_dataset_sha256 == current_export_sha256
            if training_dataset_sha256 and current_export_sha256
            else None
        )
        rejection_reasons: list[str] = []
        if not self._model_path.exists():
            rejection_reasons.append("model_missing")
        if not self._feature_columns_path.exists():
            rejection_reasons.append("features_missing")
        if (
            self._settings.prebuilt_require_metrics or self._settings.prebuilt_require_baseline_improvement
        ) and not metrics_path.exists():
            rejection_reasons.append("metrics_missing")
        if manifest and schema_hash_matches is False:
            rejection_reasons.append("schema_hash_mismatch")
        if artifact_mae is not None and artifact_mae > self._settings.prebuilt_max_mae:
            rejection_reasons.append("mae_above_limit")
        if (
            self._settings.prebuilt_require_baseline_improvement
            and artifact_mae is not None
            and baseline_mae is not None
            and artifact_mae >= baseline_mae
        ):
            rejection_reasons.append("does_not_improve_baseline")
        if not validation_coverage_matches:
            rejection_reasons.append("validation_coverage_mismatch")
        if not per_egg_type_mae_valid:
            rejection_reasons.append("per_egg_type_mae_invalid")
        if loaded is None and not rejection_reasons:
            rejection_reasons.append("artifact_not_loadable")
        return ModelArtifactStatusResponse(
            enabled=self._settings.use_prebuilt_model,
            model_exists=self._model_path.exists(),
            features_exists=self._feature_columns_path.exists(),
            metrics_exists=metrics_path.exists(),
            loadable=loaded is not None,
            feature_count=len(parsed_feature_columns),
            metrics=metrics,
            manifest=manifest,
            schema_hash_matches=schema_hash_matches,
            validation_coverage_matches=validation_coverage_matches,
            artifact_mae=artifact_mae,
            baseline_mae=baseline_mae,
            max_allowed_mae=self._settings.prebuilt_max_mae,
            quality_gate_passed=quality_gate_passed,
            rejection_reasons=rejection_reasons,
            training_dataset_sha256=training_dataset_sha256,
            current_export_sha256=current_export_sha256,
            matches_current_export=matches_current_export,
        )

    def benchmark_recommend(self, message: str, user_context: dict | None) -> MlBenchmarkResponse:
        request = self._parse_request(message, user_context)
        templates, _ = self._fetch_templates_safe()
        phases, _ = self._select_template(request.egg_type, templates)
        candidates = self._generate_candidates(phases, request)
        candidates.extend(self._load_synthetic_candidates(request))
        hardware_limits = self._fetch_hardware_limits_safe(user_context)

        scored_by_mode = [
            ("heuristic", self._score_candidates_heuristically(candidates, request)),
            ("knn_inference", self._score_candidates_with_knn(candidates, request)),
            ("prebuilt_model", self._score_candidates_with_prebuilt_model(candidates, request)),
        ]
        results: list[MlBenchmarkModeResult] = []
        for scoring_mode, scored in scored_by_mode:
            if not scored:
                results.append(MlBenchmarkModeResult(scoring_mode=scoring_mode, available=False))
                continue
            ranking_score, candidate = scored[0]
            adjusted = self._apply_rules(candidate, hardware_limits, request.egg_type)
            validation_warnings = self._validate_recommended_config(adjusted)
            estimated_success_rate, estimated_success_confidence = self._estimated_success(
                adjusted,
                request,
                scoring_mode,
                ranking_score,
            )
            results.append(
                MlBenchmarkModeResult(
                    scoring_mode=scoring_mode,
                    available=True,
                    ranking_score=round(ranking_score, 4),
                    estimated_success_rate=estimated_success_rate,
                    estimated_success_confidence=estimated_success_confidence,
                    recommended_config=adjusted,
                    passed_output_validation=not validation_warnings,
                    validation_warnings=validation_warnings,
                )
            )
        return MlBenchmarkResponse(
            egg_type=request.egg_type,
            total_eggs=request.total_eggs,
            candidate_count=len(candidates),
            results=results,
        )

    def add_synthetic_record(self, record: dict) -> "AdminTrainingAddResponse":
        from app.schemas import AdminTrainingAddResponse

        issues = validate_synthetic_record(record)
        if issues:
            return AdminTrainingAddResponse(
                records_added=0,
                total_records=len(self._load_synthetic_records()),
                validation_issues=issues,
                message=f"Dữ liệu không hợp lệ: {', '.join(issues)}",
            )

        path = Path(self._settings.synthetic_data_path)
        records = self._load_synthetic_records()
        records.append(record)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")
        self._synthetic_records_cache = None

        return AdminTrainingAddResponse(
            records_added=1,
            total_records=len(records),
            validation_issues=[],
            message=f"Đã thêm 1 record training. Tổng: {len(records)} records",
        )

    def export_training_rows(self) -> list[dict[str, float | str]]:
        rows = self._export_db_training_rows()
        rows.extend(self._export_synthetic_training_rows())
        return rows

    def _export_db_training_rows(self) -> list[dict[str, float | str]]:
        try:
            frame = self._repository.fetch_training_dataset()
        except Exception:
            return []
        if frame.empty or "season_id" not in frame.columns:
            return []

        rows: list[dict[str, float | str]] = []
        for season_id, group in frame.groupby("season_id"):
            base = group.iloc[0]
            egg_type = str(base.get("egg_type", "")).lower()
            total_eggs = _as_float(base.get("total_eggs")) or 0.0
            success_count = _as_float(base.get("success_count")) or 0.0
            phases = self._training_group_to_phases(group)
            if egg_type not in EGG_PROFILES or total_eggs <= 0 or not phases:
                continue
            request = ParsedRecommendationRequest(
                egg_type=egg_type,
                total_eggs=int(total_eggs),
                ambient_temperature=_as_float(base.get("ambient_temperature")),
                ambient_humidity=_as_float(base.get("ambient_humidity")),
                notes=None,
            )
            row = self._candidate_to_features(phases, request)
            row["success_rate"] = min(1.0, max(0.0, success_count / total_eggs))
            row["data_source"] = "db"
            row["training_group"] = f"db:{season_id}"
            rows.append(row)
        return rows

    def _export_synthetic_training_rows(self) -> list[dict[str, float | str]]:
        rows: list[dict[str, float | str]] = []
        for index, record in enumerate(self._load_synthetic_records()):
            if validate_synthetic_record(record):
                continue
            egg_type = str(record["egg_type"]).lower()
            phases = self._synthetic_record_to_phases(record)
            success_rate = _as_float(record.get("expected_success_rate"))
            if not phases or success_rate is None:
                continue
            request = ParsedRecommendationRequest(
                egg_type=egg_type,
                total_eggs=int(_as_float(record.get("total_eggs")) or 100),
                ambient_temperature=_as_float(record.get("ambient_temperature")),
                ambient_humidity=_as_float(record.get("ambient_humidity")),
                notes=None,
            )
            row = self._candidate_to_features(phases, request)
            row["success_rate"] = self._calibrate_synthetic_label(success_rate, phases, request)
            row["data_source"] = "synthetic"
            row["training_group"] = f"synthetic:{record.get('_synthetic_batch_id') or index}"
            rows.append(row)
        return rows

    def _fetch_templates_safe(self) -> tuple[list[TemplateConfigRow], str | None]:
        try:
            return self._repository.fetch_template_configs(), None
        except Exception as exc:
            return [], exc.__class__.__name__

    def _fetch_hardware_limits_safe(
        self, user_context: dict | None
    ) -> dict[str, tuple[float | None, float | None]]:
        incubator_id = (user_context or {}).get("incubator_id")
        if not incubator_id:
            return {}
        try:
            return self._repository.fetch_incubator_config_limits(str(incubator_id))
        except Exception:
            return {}

    def _parse_request(self, message: str, user_context: dict | None) -> ParsedRecommendationRequest:
        normalized = normalize_text(message)
        egg_type = self._resolve_egg_type(normalized)

        total_eggs = 100
        match = re.search(r"(\d{1,5})\s*(trung|eggs?)", normalized)
        if match:
            total_eggs = int(match.group(1))

        user_context = user_context or {}
        ambient_temperature = _as_float(user_context.get("ambient_temperature"))
        ambient_humidity = _as_float(user_context.get("ambient_humidity"))
        notes = user_context.get("notes")
        return ParsedRecommendationRequest(
            egg_type=egg_type,
            total_eggs=total_eggs,
            ambient_temperature=ambient_temperature,
            ambient_humidity=ambient_humidity,
            notes=notes,
        )

    def _resolve_egg_type(self, normalized: str) -> str:
        if "vit" in normalized or "duck" in normalized:
            return "duck"
        if "cut" in normalized or "quail" in normalized:
            return "quail"
        if "ngong" in normalized or "goose" in normalized:
            return "goose"
        if "ga" in normalized or "chicken" in normalized:
            return "chicken"
        return self._settings.default_egg_type

    def _select_template(self, egg_type: str, templates: list[TemplateConfigRow]) -> tuple[list[RecommendedPhase], str]:
        filtered = [row for row in templates if row.egg_type == egg_type]
        if not filtered:
            filtered = [row for row in templates if row.egg_type in {self._settings.default_egg_type, "unknown"}]
        if not filtered:
            return self._default_phases(egg_type), "built_in_default"

        chosen_template_id = filtered[0].template_id
        rows = [row for row in filtered if row.template_id == chosen_template_id]
        template_name = rows[0].template_name if rows else "db_template"
        return self._group_rows_into_phases(rows), f"db_template:{template_name}"

    def _group_rows_into_phases(self, rows: list[TemplateConfigRow]) -> list[RecommendedPhase]:
        phases: list[RecommendedPhase] = []
        day_cursor = 1
        for batch_index in sorted({row.batch_index for row in rows}):
            group = [row for row in rows if row.batch_index == batch_index]
            days = group[0].number_of_days
            phase = RecommendedPhase(
                phase_index=batch_index,
                phase_name=group[0].phase_name or f"Phase {batch_index}",
                day_start=day_cursor,
                day_end=day_cursor + days - 1,
                parameters=[
                    RecommendedParameter(
                        config_code=row.config_code,
                        config_name=row.config_name,
                        target_value=_as_float(row.target_value),
                        min_value=_as_float(row.min_value),
                        max_value=_as_float(row.max_value),
                        unit=row.config_unit,
                    )
                    for row in group
                ],
            )
            phases.append(phase)
            day_cursor = phase.day_end + 1
        return phases

    def _generate_candidates(
        self, template_phases: list[RecommendedPhase], request: ParsedRecommendationRequest
    ) -> list[list[RecommendedPhase]]:
        candidates = [template_phases]
        seed = abs(hash((request.egg_type, request.total_eggs))) % 10_000
        rng = random.Random(seed)
        for _ in range(6):
            mutated = []
            for phase in template_phases:
                params = []
                for param in phase.parameters:
                    spread = self._mutation_spread(param)
                    target = _mutate_value(param.target_value, spread, rng)
                    minimum = _mutate_value(param.min_value, spread, rng)
                    maximum = _mutate_value(param.max_value, spread, rng)
                    params.append(
                        RecommendedParameter(
                            config_code=param.config_code,
                            config_name=param.config_name,
                            target_value=target,
                            min_value=minimum,
                            max_value=maximum,
                            unit=param.unit,
                        )
                    )
                mutated.append(
                    RecommendedPhase(
                        phase_index=phase.phase_index,
                        phase_name=phase.phase_name,
                        day_start=phase.day_start,
                        day_end=phase.day_end,
                        parameters=params,
                    )
                )
            candidates.append(mutated)
        return candidates

    def _mutation_spread(self, param: RecommendedParameter) -> float:
        token = self._parameter_token(param)
        if self._matches_parameter_kind(token, "temperature"):
            return 0.25
        if self._matches_parameter_kind(token, "humidity"):
            return 5.0
        if self._matches_parameter_kind(token, "turning"):
            return 1.5
        if self._matches_parameter_kind(token, "fan"):
            return 0.5
        return max(abs(param.target_value or 0.0) * 0.05, 0.1)

    def _load_synthetic_candidates(self, request: ParsedRecommendationRequest) -> list[list[RecommendedPhase]]:
        records = self._load_synthetic_records()
        candidates: list[list[RecommendedPhase]] = []
        for record in records:
            if record.get("egg_type") != request.egg_type or validate_synthetic_record(record):
                continue
            phases = self._synthetic_record_to_phases(record)
            if phases and self._is_complete_candidate(phases, request.egg_type):
                candidates.append(phases)
        return candidates

    def _load_synthetic_records(self) -> list[dict]:
        path = Path(self._settings.synthetic_data_path)
        if not path.exists():
            return []

        try:
            modified_time = path.stat().st_mtime_ns
            if self._synthetic_records_cache and self._synthetic_records_cache[0] == modified_time:
                return self._synthetic_records_cache[1]
            records = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return []
        if not isinstance(records, list):
            return []
        valid_records = [record for record in records if isinstance(record, dict)]
        self._synthetic_records_cache = (modified_time, valid_records)
        return valid_records

    def _synthetic_record_to_phases(self, record: dict) -> list[RecommendedPhase]:
        raw_phases = record.get("phases")
        if not isinstance(raw_phases, list):
            return []

        phases: list[RecommendedPhase] = []
        try:
            for raw_phase in raw_phases:
                raw_parameters = raw_phase.get("parameters", [])
                parameters = [
                    RecommendedParameter(
                        config_code=str(parameter["config_code"]),
                        config_name=str(parameter.get("config_name") or parameter["config_code"]),
                        target_value=_as_float(parameter.get("target_value")),
                        min_value=_as_float(parameter.get("min_value")),
                        max_value=_as_float(parameter.get("max_value")),
                        unit=parameter.get("unit"),
                    )
                    for parameter in raw_parameters
                    if isinstance(parameter, dict) and parameter.get("config_code")
                ]
                if not parameters:
                    return []
                phase_index = int(raw_phase["phase_index"])
                phases.append(
                    RecommendedPhase(
                        phase_index=phase_index,
                        phase_name=str(raw_phase.get("phase_name") or f"Phase {phase_index}"),
                        day_start=int(raw_phase["day_start"]),
                        day_end=int(raw_phase["day_end"]),
                        parameters=parameters,
                    )
                )
        except (KeyError, TypeError, ValueError):
            return []
        return sorted(phases, key=lambda phase: phase.phase_index)

    def _is_complete_candidate(self, phases: list[RecommendedPhase], egg_type: str) -> bool:
        profile = EGG_PROFILES.get(egg_type, EGG_PROFILES["chicken"])
        if len(phases) != len(profile["phase_days"]):
            return False
        if phases[-1].day_end != profile["phase_days"][-1][1]:
            return False
        for phase in phases:
            kinds = {
                kind
                for parameter in phase.parameters
                for kind in ("temperature", "humidity")
                if self._matches_parameter_kind(self._parameter_token(parameter), kind)
            }
            if kinds != {"temperature", "humidity"}:
                return False
        return True

    def _score_candidates(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> tuple[list[RecommendedPhase], str, float]:
        scored_candidates, scoring_mode = self._score_all_candidates(candidates, request)
        return scored_candidates[0][1], scoring_mode, scored_candidates[0][0]

    def _score_all_candidates(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> tuple[list[tuple[float, list[RecommendedPhase]]], str]:
        if self._settings.use_prebuilt_model and self._model_path.exists() and self._feature_columns_path.exists():
            scored = self._score_candidates_with_prebuilt_model(candidates, request)
            if scored:
                return scored, "prebuilt_model"

        knn_scored = self._score_candidates_with_knn(candidates, request)
        if knn_scored:
            return knn_scored, "knn_inference"

        scored = self._score_candidates_heuristically(candidates, request)
        return scored, "heuristic"

    def _score_candidates_with_prebuilt_model(
        self,
        candidates: list[list[RecommendedPhase]],
        request: ParsedRecommendationRequest,
    ) -> list[tuple[float, list[RecommendedPhase]]]:
        if not self._prebuilt_model_supports_egg_type(request.egg_type):
            return []
        loaded = self._load_prebuilt_model()
        if loaded is None:
            return []
        model, feature_columns = loaded

        try:
            rows = [self._candidate_to_features(candidate, request) for candidate in candidates]
            frame = pd.DataFrame(rows)
            for column in feature_columns:
                if column not in frame.columns:
                    frame[column] = "" if column == "egg_type" else 0.0
            scores = model.predict(frame[feature_columns])
            if len(scores) != len(candidates):
                return []
            numeric_scores = [float(score) for score in scores]
            if any(not math.isfinite(score) or not 0 <= score <= 1 for score in numeric_scores):
                return []
            return sorted(
                ((score, candidate) for score, candidate in zip(numeric_scores, candidates)),
                key=lambda item: item[0],
                reverse=True,
            )
        except Exception:
            return []

    def _prebuilt_model_supports_egg_type(self, egg_type: str) -> bool:
        manifest_path = Path(self._settings.model_dir) / "recommendation_manifest.json"
        if not manifest_path.exists():
            return True
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            supported = manifest.get("supported_egg_types")
            return not isinstance(supported, list) or egg_type in supported
        except (OSError, json.JSONDecodeError):
            return False

    def _prebuilt_trace(self) -> tuple[int | None, str | None]:
        manifest_path = Path(self._settings.model_dir) / "recommendation_manifest.json"
        if not manifest_path.exists():
            return None, None
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            if not isinstance(manifest, dict):
                return None, None
            version = manifest.get("artifact_version")
            dataset_hash = manifest.get("training_dataset_sha256")
            return (int(version) if version is not None else None, str(dataset_hash) if dataset_hash else None)
        except (OSError, json.JSONDecodeError, TypeError, ValueError):
            return None, None

    def _load_prebuilt_model(self) -> tuple[Any, list[str]] | None:
        try:
            model_mtime = self._model_path.stat().st_mtime_ns
            features_mtime = self._feature_columns_path.stat().st_mtime_ns
            manifest_path = Path(self._settings.model_dir) / "recommendation_manifest.json"
            manifest_mtime = manifest_path.stat().st_mtime_ns if manifest_path.exists() else 0
            metrics_path = Path(self._settings.model_dir) / "recommendation_metrics.json"
            metrics_mtime = metrics_path.stat().st_mtime_ns if metrics_path.exists() else 0
            if (
                self._prebuilt_model_cache
                and self._prebuilt_model_cache[0] == model_mtime
                and self._prebuilt_model_cache[1] == features_mtime
                and self._prebuilt_model_cache[2] == manifest_mtime
                and self._prebuilt_model_cache[3] == metrics_mtime
            ):
                return self._prebuilt_model_cache[4], self._prebuilt_model_cache[5]
            model = joblib.load(self._model_path)
            feature_columns = json.loads(self._feature_columns_path.read_text(encoding="utf-8"))
            if not isinstance(feature_columns, list) or not feature_columns:
                return None
            if manifest_path.exists():
                manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
                schema_json = json.dumps(feature_columns, ensure_ascii=False, separators=(",", ":"))
                schema_hash = hashlib.sha256(schema_json.encode("utf-8")).hexdigest()
                if (
                    not isinstance(manifest, dict)
                    or manifest.get("artifact_version") != 1
                    or manifest.get("feature_schema_sha256") != schema_hash
                ):
                    return None
            if (
                self._settings.prebuilt_require_metrics or self._settings.prebuilt_require_baseline_improvement
            ) and not metrics_path.exists():
                return None
            if metrics_path.exists():
                metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
                mae = _as_float(metrics.get("mae")) if isinstance(metrics, dict) else None
                if mae is None or mae > self._settings.prebuilt_max_mae:
                    return None
                supported_egg_types = set(manifest.get("supported_egg_types") or []) if manifest_path.exists() else set()
                validation_egg_types = set(metrics.get("validation_egg_types") or []) if isinstance(metrics, dict) else set()
                if not supported_egg_types or validation_egg_types != supported_egg_types:
                    return None
                per_egg_type_mae = metrics.get("per_egg_type_mae") if isinstance(metrics, dict) else None
                if (
                    not isinstance(per_egg_type_mae, dict)
                    or set(per_egg_type_mae) != supported_egg_types
                    or any(
                        (value := _as_float(egg_mae)) is None or value > self._settings.prebuilt_max_mae
                        for egg_mae in per_egg_type_mae.values()
                    )
                ):
                    return None
                baseline_mae = _as_float(metrics.get("baseline_mae")) if isinstance(metrics, dict) else None
                if (
                    self._settings.prebuilt_require_baseline_improvement
                    and (baseline_mae is None or mae >= baseline_mae)
                ):
                    return None
            self._prebuilt_model_cache = (
                model_mtime,
                features_mtime,
                manifest_mtime,
                metrics_mtime,
                model,
                feature_columns,
            )
            return model, feature_columns
        except Exception:
            return None

    def _score_candidates_with_knn(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> list[tuple[float, list[RecommendedPhase]]]:
        references = self._prediction_references(request)
        if len(references) < max(1, self._settings.knn_min_samples):
            return []

        heuristic_scores = dict(
            (id(candidate), score) for score, candidate in self._score_candidates_heuristically(candidates, request)
        )
        scored: list[tuple[float, list[RecommendedPhase]]] = []
        for candidate in candidates:
            features = self._candidate_to_distance_features(candidate, request)
            predicted_success, _ = self._predict_success_from_references(features, references)
            score = predicted_success * 100 + heuristic_scores[id(candidate)] * self._settings.knn_heuristic_weight
            scored.append((score, candidate))
        return sorted(scored, key=lambda item: item[0], reverse=True)

    def _predict_success_from_references(
        self,
        features: dict[str, float],
        references: list[tuple[dict[str, float], float, float]],
    ) -> tuple[float, float]:
        neighbors = sorted(
            (
                (_feature_distance(features, reference_features), success_rate, source_weight)
                for reference_features, success_rate, source_weight in references
            ),
            key=lambda item: item[0],
        )[: max(1, self._settings.knn_neighbors)]
        weighted_total = sum(
            success_rate * source_weight / (distance + 0.01)
            for distance, success_rate, source_weight in neighbors
        )
        weight_total = sum(source_weight / (distance + 0.01) for distance, _, source_weight in neighbors)
        prediction = weighted_total / weight_total
        average_distance = sum(distance for distance, _, _ in neighbors) / len(neighbors)
        sample_factor = min(1.0, len(neighbors) / max(1, self._settings.knn_neighbors))
        distance_factor = 1.0 / (1.0 + average_distance * 5)
        return prediction, sample_factor * distance_factor

    def _estimated_success(
        self,
        candidate: list[RecommendedPhase],
        request: ParsedRecommendationRequest,
        scoring_mode: str,
        ranking_score: float,
    ) -> tuple[float | None, float | None]:
        if scoring_mode == "prebuilt_model":
            rescored = self._score_candidates_with_prebuilt_model([candidate], request)
            score = rescored[0][0] if rescored else ranking_score
            return round(min(1.0, max(0.0, score)), 4), None
        if scoring_mode != "knn_inference":
            return None, None
        references = self._prediction_references(request)
        if not references:
            return None, None
        features = self._candidate_to_distance_features(candidate, request)
        prediction, confidence = self._predict_success_from_references(features, references)
        return round(prediction, 4), round(confidence, 4)

    def _prediction_references(
        self, request: ParsedRecommendationRequest
    ) -> list[tuple[dict[str, float], float, float]]:
        db_references = self._deduplicate_references(self._load_db_labeled_references(request))
        if len(db_references) >= max(1, self._settings.knn_min_samples):
            return db_references
        synthetic_references = self._load_synthetic_labeled_references(request)
        return self._deduplicate_references(db_references + synthetic_references)

    def _deduplicate_references(
        self,
        references: list[tuple[dict[str, float], float, float]],
    ) -> list[tuple[dict[str, float], float, float]]:
        grouped: dict[tuple[tuple[str, float], ...], tuple[dict[str, float], list[float], float]] = {}
        for features, success_rate, source_weight in references:
            fingerprint = tuple(sorted((key, round(value, 4)) for key, value in features.items()))
            existing = grouped.get(fingerprint)
            if existing is None or source_weight > existing[2]:
                grouped[fingerprint] = (features, [success_rate], source_weight)
            elif source_weight == existing[2]:
                existing[1].append(success_rate)
        return [
            (features, sum(success_rates) / len(success_rates), source_weight)
            for features, success_rates, source_weight in grouped.values()
        ]

    def _load_synthetic_labeled_references(
        self, request: ParsedRecommendationRequest
    ) -> list[tuple[dict[str, float], float, float]]:
        references: list[tuple[dict[str, float], float, float]] = []
        records = reversed(self._load_synthetic_records())
        for record in records:
            if record.get("egg_type") != request.egg_type or validate_synthetic_record(record):
                continue
            expected_success_rate = _as_float(record.get("expected_success_rate"))
            phases = self._synthetic_record_to_phases(record)
            if expected_success_rate is None or not 0 <= expected_success_rate <= 1 or not phases:
                continue
            reference_request = ParsedRecommendationRequest(
                egg_type=request.egg_type,
                total_eggs=int(_as_float(record.get("total_eggs")) or 100),
                ambient_temperature=_as_float(record.get("ambient_temperature")),
                ambient_humidity=_as_float(record.get("ambient_humidity")),
                notes=None,
            )
            calibrated_success_rate = self._calibrate_synthetic_label(
                expected_success_rate,
                phases,
                reference_request,
            )
            references.append(
                (
                    self._candidate_to_distance_features(phases, reference_request),
                    calibrated_success_rate,
                    max(0.01, self._settings.knn_synthetic_reference_weight),
                )
            )
            if len(references) >= max(1, self._settings.knn_max_synthetic_references):
                break
        return references

    def _calibrate_synthetic_label(
        self,
        gemini_success_rate: float,
        phases: list[RecommendedPhase],
        request: ParsedRecommendationRequest,
    ) -> float:
        heuristic_score = self._score_candidates_heuristically([phases], request)[0][0]
        heuristic_proxy = min(0.95, max(0.40, 0.75 + heuristic_score / 400.0))
        gemini_weight = self._settings.synthetic_label_gemini_weight
        return min(
            1.0,
            max(0.0, gemini_success_rate * gemini_weight + heuristic_proxy * (1.0 - gemini_weight)),
        )

    def _load_db_labeled_references(
        self, request: ParsedRecommendationRequest, force_refresh: bool = False
    ) -> list[tuple[dict[str, float], float, float]]:
        cached = self._db_reference_cache.get(request.egg_type)
        now = time.monotonic()
        if not force_refresh and cached and now - cached[0] < max(0, self._settings.knn_db_cache_seconds):
            return cached[1]
        try:
            frame = self._repository.fetch_training_dataset()
        except Exception:
            return []
        if frame.empty or "season_id" not in frame.columns:
            return []

        references: list[tuple[dict[str, float], float, float]] = []
        for _, group in frame.groupby("season_id"):
            base = group.iloc[0]
            if str(base.get("egg_type", "")).lower() != request.egg_type:
                continue
            total_eggs = _as_float(base.get("total_eggs")) or 0.0
            success_count = _as_float(base.get("success_count")) or 0.0
            if total_eggs <= 0:
                continue
            phases = self._training_group_to_phases(group)
            if not phases:
                continue
            reference_request = ParsedRecommendationRequest(
                egg_type=request.egg_type,
                total_eggs=int(total_eggs),
                ambient_temperature=_as_float(base.get("ambient_temperature")),
                ambient_humidity=_as_float(base.get("ambient_humidity")),
                notes=None,
            )
            success_rate = min(1.0, max(0.0, success_count / total_eggs))
            references.append(
                (
                    self._candidate_to_distance_features(phases, reference_request),
                    success_rate,
                    max(0.01, self._settings.knn_db_reference_weight),
                )
            )
        self._db_reference_cache[request.egg_type] = (now, references)
        return references

    def _training_group_to_phases(self, group: pd.DataFrame) -> list[RecommendedPhase]:
        phases: list[RecommendedPhase] = []
        usable = group.dropna(subset=["batch_index", "config_code"])
        for batch_index, phase_group in usable.groupby("batch_index"):
            first = phase_group.iloc[0]
            parameters = [
                RecommendedParameter(
                    config_code=str(row["config_code"]),
                    config_name=(
                        str(row["config_name"]) if pd.notna(row.get("config_name")) else str(row["config_code"])
                    ),
                    target_value=_as_float(row.get("target_value")),
                    min_value=_as_float(row.get("min_value")),
                    max_value=_as_float(row.get("max_value")),
                    unit=str(row["config_unit"]) if pd.notna(row.get("config_unit")) else None,
                )
                for _, row in phase_group.iterrows()
            ]
            phases.append(
                RecommendedPhase(
                    phase_index=int(batch_index),
                    phase_name=f"Phase {int(batch_index)}",
                    day_start=int(_as_float(first.get("day_start")) or 1),
                    day_end=int(_as_float(first.get("day_end")) or 1),
                    parameters=parameters,
                )
            )
        return sorted(phases, key=lambda phase: phase.phase_index)

    def _candidate_to_distance_features(
        self, candidate: list[RecommendedPhase], request: ParsedRecommendationRequest
    ) -> dict[str, float]:
        features: dict[str, float] = {
            "total_eggs": min(request.total_eggs, 5000) / 5000.0,
            "phase_count": len(candidate) / 5.0,
        }
        if request.ambient_temperature is not None:
            features["ambient_temperature"] = request.ambient_temperature / 50.0
        if request.ambient_humidity is not None:
            features["ambient_humidity"] = request.ambient_humidity / 100.0
        scales = {"temperature": 40.0, "humidity": 100.0, "turning": 12.0, "fan": 5.0}
        for phase in candidate:
            features[f"phase_{phase.phase_index}_day_span"] = (phase.day_end - phase.day_start + 1) / 30.0
            for parameter in phase.parameters:
                token = self._parameter_token(parameter)
                for kind, scale in scales.items():
                    if self._matches_parameter_kind(token, kind) and parameter.target_value is not None:
                        features[f"phase_{phase.phase_index}_{kind}"] = parameter.target_value / scale
                        break
        return features

    def _score_candidates_heuristically(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> list[tuple[float, list[RecommendedPhase]]]:
        def score(candidate: list[RecommendedPhase]) -> float:
            total = 0.0
            last_phase_index = max((phase.phase_index for phase in candidate), default=0)
            for phase in candidate:
                last_phase = phase.phase_index == last_phase_index
                for param in phase.parameters:
                    token = self._parameter_token(param)
                    target = param.target_value or 0.0
                    if self._matches_parameter_kind(token, "temperature"):
                        ideal = self._profile_target(request.egg_type, "temperature", phase.phase_index, last_phase)
                        if request.ambient_temperature and request.ambient_temperature > 32:
                            ideal -= 0.1
                        total -= abs(target - ideal) * 25
                    elif self._matches_parameter_kind(token, "humidity"):
                        ideal = self._profile_target(request.egg_type, "humidity", phase.phase_index, last_phase)
                        if request.ambient_humidity and request.ambient_humidity < 50:
                            ideal += 3.0
                        total -= abs(target - ideal) * 8
                    elif self._matches_parameter_kind(token, "turning"):
                        ideal = 0.0 if last_phase else 6.0
                        total -= abs(target - ideal) * 5
                    elif self._matches_parameter_kind(token, "fan"):
                        ideal = 2.0
                        total -= abs(target - ideal) * 3

                    if (
                        param.min_value is not None
                        and param.max_value is not None
                        and param.min_value <= target <= param.max_value
                    ):
                        total += 2.0
            total += min(request.total_eggs / 100.0, 8.0)
            return total

        return sorted(((score(candidate), candidate) for candidate in candidates), key=lambda item: item[0], reverse=True)

    def _profile_target(self, egg_type: str, kind: str, phase_index: int, last_phase: bool) -> float:
        profile = EGG_PROFILES.get(egg_type, EGG_PROFILES["chicken"])
        values = profile[kind]
        index = min(max(phase_index - 1, 0), len(values) - 1)
        if last_phase:
            index = len(values) - 1
        return float(values[index])

    def _candidate_to_features(
        self, candidate: list[RecommendedPhase], request: ParsedRecommendationRequest
    ) -> dict[str, float | str]:
        features: dict[str, float | str] = {
            "egg_type": request.egg_type,
            "total_eggs": float(request.total_eggs),
            "phase_count": float(len(candidate)),
            "ambient_temperature": float(request.ambient_temperature or 0.0),
            "ambient_humidity": float(request.ambient_humidity or 0.0),
        }
        for phase in candidate:
            prefix = f"phase_{phase.phase_index}"
            features[f"{prefix}_day_span"] = float(phase.day_end - phase.day_start + 1)
            for param in phase.parameters:
                key = _slugify(param.config_code or param.config_name)
                if param.target_value is not None:
                    features[f"{prefix}_{key}_target"] = float(param.target_value)
                if param.min_value is not None:
                    features[f"{prefix}_{key}_min"] = float(param.min_value)
                if param.max_value is not None:
                    features[f"{prefix}_{key}_max"] = float(param.max_value)
        return features

    def _apply_rules(
        self,
        phases: list[RecommendedPhase],
        hardware_limits: dict[str, tuple[float | None, float | None]] | None = None,
        egg_type: str | None = None,
    ) -> list[RecommendedPhase]:
        hardware_limits = hardware_limits or {}
        resolved_egg_type = egg_type or self._settings.default_egg_type
        profile = EGG_PROFILES.get(resolved_egg_type, EGG_PROFILES["chicken"])
        final_humidity_target = float(profile["humidity"][-1])
        last_phase_index = max((phase.phase_index for phase in phases), default=0)
        adjusted: list[RecommendedPhase] = []
        for phase in phases:
            params = []
            for param in phase.parameters:
                target = param.target_value
                minimum = param.min_value
                maximum = param.max_value
                token = self._parameter_token(param)

                if self._matches_parameter_kind(token, "temperature"):
                    minimum, target, maximum = _clamp_triplet(minimum, target, maximum, 36.5, 38.3)
                elif self._matches_parameter_kind(token, "humidity"):
                    minimum, target, maximum = _clamp_triplet(minimum, target, maximum, 45, 80)
                elif self._matches_parameter_kind(token, "turning"):
                    minimum, target, maximum = _clamp_triplet(minimum, target, maximum, 0, 12)
                elif self._matches_parameter_kind(token, "fan"):
                    minimum, target, maximum = _clamp_triplet(minimum, target, maximum, 0, 5)

                if phase.phase_index == last_phase_index and self._matches_parameter_kind(token, "humidity"):
                    target = max(target or 0, final_humidity_target)
                    minimum = max(minimum or 0, final_humidity_target - 5)
                if phase.phase_index == last_phase_index and self._matches_parameter_kind(token, "turning"):
                    target = min(target or 0, 1.0)
                    maximum = min(maximum or 1.0, 1.0)

                hardware_min, hardware_max = hardware_limits.get(param.config_code.lower(), (None, None))
                if hardware_min is not None:
                    minimum = max(minimum if minimum is not None else hardware_min, hardware_min)
                    target = max(target if target is not None else hardware_min, hardware_min)
                    maximum = max(maximum if maximum is not None else hardware_min, hardware_min)
                if hardware_max is not None:
                    minimum = min(minimum if minimum is not None else hardware_max, hardware_max)
                    maximum = min(maximum if maximum is not None else hardware_max, hardware_max)
                    target = min(target if target is not None else hardware_max, hardware_max)

                if minimum is not None and maximum is not None and minimum > maximum:
                    minimum, maximum = maximum, minimum
                if target is not None and minimum is not None:
                    target = max(target, minimum)
                if target is not None and maximum is not None:
                    target = min(target, maximum)

                params.append(
                    RecommendedParameter(
                        config_code=param.config_code,
                        config_name=param.config_name,
                        target_value=_rounded(target),
                        min_value=_rounded(minimum),
                        max_value=_rounded(maximum),
                        unit=param.unit,
                    )
                )
            adjusted.append(
                RecommendedPhase(
                    phase_index=phase.phase_index,
                    phase_name=phase.phase_name,
                    day_start=phase.day_start,
                    day_end=phase.day_end,
                    parameters=params,
                )
            )
        return adjusted

    def _validate_recommended_config(self, phases: list[RecommendedPhase]) -> list[str]:
        issues: list[str] = []
        if len(phases) != 3:
            issues.append("invalid_phase_count")
        phase_indexes = [phase.phase_index for phase in phases]
        if sorted(phase_indexes) != list(range(1, len(phases) + 1)):
            issues.append("invalid_phase_indexes")
        previous_day_end = 0
        for phase in sorted(phases, key=lambda item: item.phase_index):
            if (
                not _is_finite_number(phase.day_start)
                or not _is_finite_number(phase.day_end)
                or phase.day_start != previous_day_end + 1
                or phase.day_end < phase.day_start
            ):
                issues.append(f"phase_{phase.phase_index}_invalid_days")
            previous_day_end = phase.day_end
            core_kinds = set()
            parameter_codes: set[str] = set()
            for parameter in phase.parameters:
                token = self._parameter_token(parameter)
                code = (parameter.config_code or parameter.config_name or "").strip().lower()
                if not code:
                    issues.append(f"phase_{phase.phase_index}_missing_parameter_code")
                elif code in parameter_codes:
                    issues.append(f"phase_{phase.phase_index}_{code}_duplicate_parameter")
                parameter_codes.add(code)
                for kind in ("temperature", "humidity"):
                    if self._matches_parameter_kind(token, kind):
                        core_kinds.add(kind)
                values = (parameter.min_value, parameter.target_value, parameter.max_value)
                if any(value is None for value in values):
                    issues.append(f"phase_{phase.phase_index}_{code or 'unknown'}_missing_value")
                    continue
                if any(value is not None and not _is_finite_number(value) for value in values):
                    issues.append(f"phase_{phase.phase_index}_{code or 'unknown'}_non_finite_value")
                    continue
                if (
                    parameter.min_value is not None
                    and parameter.max_value is not None
                    and parameter.min_value > parameter.max_value
                ):
                    issues.append(f"phase_{phase.phase_index}_{code or 'unknown'}_min_above_max")
                if (
                    parameter.min_value is not None
                    and parameter.target_value is not None
                    and parameter.target_value < parameter.min_value
                ):
                    issues.append(f"phase_{phase.phase_index}_{parameter.config_code}_target_below_min")
                if (
                    parameter.max_value is not None
                    and parameter.target_value is not None
                    and parameter.target_value > parameter.max_value
                ):
                    issues.append(f"phase_{phase.phase_index}_{parameter.config_code}_target_above_max")
            if core_kinds != {"temperature", "humidity"}:
                issues.append(f"phase_{phase.phase_index}_missing_core_parameters")
        return sorted(set(issues))

    def _build_answer(
        self,
        request: ParsedRecommendationRequest,
        phases: list[RecommendedPhase],
        template_source: str,
        scoring_mode: str,
        estimated_success_rate: float | None,
        estimated_success_confidence: float | None,
    ) -> str:
        scoring_explanations = {
            "knn_inference": "Cấu hình được xếp hạng bằng KNN inference trên kết quả DB thật và dữ liệu synthetic có label.",
            "prebuilt_model": "Cấu hình được xếp hạng bằng prebuilt model artifact.",
            "heuristic": "Chưa đủ labeled references cho ML, nên cấu hình được xếp hạng bằng heuristic kỹ thuật.",
        }
        lines = [
            f"Đề xuất cấu hình cho {request.total_eggs} trứng {request.egg_type} với {len(phases)} phase.",
            scoring_explanations.get(scoring_mode, scoring_explanations["heuristic"]),
            "Ở phase cuối, độ ẩm được đẩy cao hơn và thông số đảo trứng được siết lại để ưu tiên giai đoạn nở.",
        ]
        if "db_fallback" in template_source:
            lines.append("DB template hiện chưa đọc được, nên hệ thống đang dùng cấu hình mặc định đã hậu kiểm.")
        if request.ambient_temperature is not None or request.ambient_humidity is not None:
            lines.append("Tôi đã tính đến điều kiện môi trường đầu vào khi chấm điểm các cấu hình ứng viên.")
        if estimated_success_rate is not None:
            lines.append(
                f"Bộ chấm điểm {scoring_mode} ước tính tỷ lệ thành công khoảng {estimated_success_rate * 100:.1f}%."
            )
        if estimated_success_confidence is not None:
            lines.append(f"Độ tin cậy tương đối của ước tính là {estimated_success_confidence * 100:.1f}%.")
        return " ".join(lines)

    def _parameter_token(self, param: RecommendedParameter) -> str:
        return f"{(param.config_code or '').lower()} {(param.config_name or '').lower()}"

    def _matches_parameter_kind(self, token: str, kind: str) -> bool:
        groups = {
            "temperature": ["temp", "temperature", "nhiệt", "nhiet", "tempera", "°c"],
            "humidity": ["humid", "humidity", "độ ẩm", "do am", "ẩm", "am", "%"],
            "turning": ["turn", "turning", "đảo", "dao", "egg_turn"],
            "fan": ["fan", "quạt", "quat", "airflow"],
        }
        return any(keyword in token for keyword in groups[kind])

    def _default_phases(self, egg_type: str) -> list[RecommendedPhase]:
        profile = EGG_PROFILES.get(egg_type, EGG_PROFILES["chicken"])
        phase_days = profile["phase_days"]
        configs = [
            (
                "TEMP",
                "Temperature",
                "C",
                [(value, value - 0.2, value + 0.2) for value in profile["temperature"]],
            ),
            (
                "HUMID",
                "Humidity",
                "%",
                [(value, value - 5, value + 5) for value in profile["humidity"]],
            ),
            ("TURN", "Turning", "times/day", [(6, 4, 8), (6, 4, 8), (0, 0, 1)]),
            ("FAN", "Fan Speed", "level", [(2, 1, 3), (2, 1, 3), (2, 1, 3)]),
        ]
        phases: list[RecommendedPhase] = []
        for index, (day_start, day_end) in enumerate(phase_days, start=1):
            parameters = []
            for code, name, unit, values in configs:
                target, minimum, maximum = values[index - 1]
                parameters.append(
                    RecommendedParameter(
                        config_code=code,
                        config_name=name,
                        target_value=target,
                        min_value=minimum,
                        max_value=maximum,
                        unit=unit,
                    )
                )
            phases.append(
                RecommendedPhase(
                    phase_index=index,
                    phase_name=f"Phase {index}",
                    day_start=day_start,
                    day_end=day_end,
                    parameters=parameters,
                )
            )
        return phases


def _slugify(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", value.lower()).strip("_")


def _mutate_value(value: float | None, spread: float, rng: random.Random) -> float | None:
    if value is None:
        return None
    return value + rng.uniform(-spread, spread)


def _as_float(value: object) -> float | None:
    if value is None:
        return None
    try:
        parsed = float(value)
    except (TypeError, ValueError):
        return None
    return parsed if math.isfinite(parsed) else None


def _rounded(value: float | None) -> float | None:
    if value is None:
        return None
    return round(value, 2)


def _is_finite_number(value: object) -> bool:
    try:
        return math.isfinite(float(value))
    except (TypeError, ValueError):
        return False


def _clamp_triplet(
    minimum: float | None, target: float | None, maximum: float | None, floor: float, ceiling: float
) -> tuple[float | None, float | None, float | None]:
    minimum = None if minimum is None else min(max(minimum, floor), ceiling)
    target = None if target is None else min(max(target, floor), ceiling)
    maximum = None if maximum is None else min(max(maximum, floor), ceiling)
    return minimum, target, maximum


def _feature_distance(left: dict[str, float], right: dict[str, float]) -> float:
    union_keys = set(left) | set(right)
    common_keys = set(left) & set(right)
    if not union_keys or not common_keys:
        return 1.0
    squared_distance = sum((left[key] - right[key]) ** 2 for key in common_keys)
    value_distance = (squared_distance / len(common_keys)) ** 0.5
    missing_ratio = 1.0 - len(common_keys) / len(union_keys)
    return value_distance + missing_ratio * 0.5
