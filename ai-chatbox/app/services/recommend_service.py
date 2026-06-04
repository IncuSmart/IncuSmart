from __future__ import annotations

import json
import random
import re
from dataclasses import dataclass
from pathlib import Path

import joblib
import pandas as pd

from app.config import Settings
from app.repositories.postgres_repository import PostgresRepository, TemplateConfigRow
from app.schemas import RecommendDebugCandidate, RecommendDebugResponse, RecommendedParameter, RecommendedPhase


@dataclass
class RecommendationResult:
    answer: str
    phases: list[RecommendedPhase]
    template_source: str
    scoring_mode: str


@dataclass
class ParsedRecommendationRequest:
    egg_type: str
    total_eggs: int
    ambient_temperature: float | None
    ambient_humidity: float | None
    notes: str | None


class RecommendService:
    def __init__(self, settings: Settings, repository: PostgresRepository):
        self._settings = settings
        self._repository = repository
        self._model_path = Path(settings.model_dir) / "recommendation_model.joblib"
        self._feature_columns_path = Path(settings.model_dir) / "recommendation_features.json"

    def recommend(self, message: str, user_context: dict | None) -> RecommendationResult:
        request = self._parse_request(message, user_context)
        templates, template_fetch_error = self._fetch_templates_safe()
        phases, template_source = self._select_template(request.egg_type, templates)
        if template_fetch_error:
            template_source = f"{template_source}:db_fallback"
        candidates = self._generate_candidates(phases, request)
        best_candidate, scoring_mode = self._score_candidates(candidates, request)
        adjusted = self._apply_rules(best_candidate)
        answer = self._build_answer(request, adjusted, template_source)
        return RecommendationResult(
            answer=answer,
            phases=adjusted,
            template_source=template_source,
            scoring_mode=scoring_mode,
        )

    def debug_recommend(self, message: str, user_context: dict | None) -> RecommendDebugResponse:
        request = self._parse_request(message, user_context)
        templates, template_fetch_error = self._fetch_templates_safe()
        phases, template_source = self._select_template(request.egg_type, templates)
        if template_fetch_error:
            template_source = f"{template_source}:db_fallback"
        candidates = self._generate_candidates(phases, request)
        scored_candidates, scoring_mode = self._score_all_candidates(candidates, request)
        top_candidates = [
            RecommendDebugCandidate(
                rank=index + 1,
                score=round(score, 4),
                phases=self._apply_rules(candidate),
            )
            for index, (score, candidate) in enumerate(scored_candidates[:3])
        ]
        best_candidate = top_candidates[0].phases if top_candidates else self._apply_rules(phases)
        answer_preview = self._build_answer(request, best_candidate, template_source)
        return RecommendDebugResponse(
            message=message,
            egg_type=request.egg_type,
            total_eggs=request.total_eggs,
            template_source=template_source,
            scoring_mode=scoring_mode,
            answer_preview=answer_preview,
            top_candidates=top_candidates,
        )

    def _fetch_templates_safe(self) -> tuple[list[TemplateConfigRow], str | None]:
        try:
            return self._repository.fetch_template_configs(), None
        except Exception as exc:
            return [], exc.__class__.__name__

    def _parse_request(self, message: str, user_context: dict | None) -> ParsedRecommendationRequest:
        normalized = message.lower()
        egg_type = self._settings.default_egg_type
        if "vịt" in normalized or "duck" in normalized:
            egg_type = "duck"
        elif "cút" in normalized or "quail" in normalized:
            egg_type = "quail"
        elif "ngỗng" in normalized or "goose" in normalized:
            egg_type = "goose"
        elif "gà" in normalized or "chicken" in normalized:
            egg_type = "chicken"

        total_eggs = 100
        match = re.search(r"(\d{2,5})\s*(trứng|egg)", normalized)
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
                    spread = 0.15 if (param.unit or "").startswith("%") else 0.08
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

    def _score_candidates(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> tuple[list[RecommendedPhase], str]:
        scored_candidates, scoring_mode = self._score_all_candidates(candidates, request)
        return scored_candidates[0][1], scoring_mode

    def _score_all_candidates(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> tuple[list[tuple[float, list[RecommendedPhase]]], str]:
        if (
            not self._settings.use_prebuilt_model
            or not self._model_path.exists()
            or not self._feature_columns_path.exists()
        ):
            scored = self._score_candidates_heuristically(candidates, request)
            return scored, "heuristic"

        model = joblib.load(self._model_path)
        with self._feature_columns_path.open("r", encoding="utf-8") as handle:
            feature_columns: list[str] = json.load(handle)

        rows = [self._candidate_to_features(candidate, request) for candidate in candidates]
        frame = pd.DataFrame(rows)
        for column in feature_columns:
            if column not in frame.columns:
                frame[column] = "" if column == "egg_type" else 0.0
        frame = frame[feature_columns]

        scores = model.predict(frame)
        scored = sorted(
            ((float(score), candidate) for score, candidate in zip(scores, candidates)),
            key=lambda item: item[0],
            reverse=True,
        )
        return scored, "prebuilt_model"

    def _score_candidates_heuristically(
        self, candidates: list[list[RecommendedPhase]], request: ParsedRecommendationRequest
    ) -> list[tuple[float, list[RecommendedPhase]]]:
        def score(candidate: list[RecommendedPhase]) -> float:
            total = 0.0
            for phase in candidate:
                last_phase = phase.phase_index == len(candidate)
                for param in phase.parameters:
                    token = self._parameter_token(param)
                    target = param.target_value or 0.0
                    if self._matches_parameter_kind(token, "temperature"):
                        ideal = 37.2 if last_phase else 37.6
                        if request.ambient_temperature and request.ambient_temperature > 32:
                            ideal -= 0.1
                        total -= abs(target - ideal) * 25
                    elif self._matches_parameter_kind(token, "humidity"):
                        ideal = 70.0 if last_phase else 60.0
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

    def _apply_rules(self, phases: list[RecommendedPhase]) -> list[RecommendedPhase]:
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

                if phase.phase_index >= len(phases) and self._matches_parameter_kind(token, "humidity"):
                    target = max(target or 0, 68.0)
                    minimum = max(minimum or 0, 65.0)
                if phase.phase_index >= len(phases) and self._matches_parameter_kind(token, "turning"):
                    target = min(target or 0, 1.0)
                    maximum = min(maximum or 1.0, 1.0)

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

    def _build_answer(
        self,
        request: ParsedRecommendationRequest,
        phases: list[RecommendedPhase],
        template_source: str,
    ) -> str:
        lines = [
            f"Đề xuất cấu hình cho {request.total_eggs} trứng {request.egg_type} với {len(phases)} phase.",
            "Cấu hình này được chọn từ template hiện có, dữ liệu synthetic, rule kỹ thuật và bộ chấm điểm inference tại chỗ.",
            "Ở phase cuối, độ ẩm được đẩy cao hơn và thông số đảo trứng được siết lại để ưu tiên giai đoạn nở.",
        ]
        if "db_fallback" in template_source:
            lines.append("DB template hiện chưa đọc được, nên hệ thống đang dùng cấu hình mặc định đã hậu kiểm.")
        if request.ambient_temperature is not None or request.ambient_humidity is not None:
            lines.append("Tôi đã tính đến điều kiện môi trường đầu vào khi chấm điểm các cấu hình ứng viên.")
        if self._settings.use_prebuilt_model and self._model_path.exists():
            lines.append("Nếu có prebuilt model artifact, hệ thống sẽ ưu tiên model đó thay cho heuristic scorer.")
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
        phase_days = [(1, 7), (8, 18), (19, 21)] if egg_type != "duck" else [(1, 10), (11, 25), (26, 28)]
        configs = [
            ("TEMP", "Temperature", "C", [(37.7, 37.5, 37.8), (37.5, 37.4, 37.6), (37.2, 37.1, 37.3)]),
            ("HUMID", "Humidity", "%", [(60, 55, 65), (60, 55, 65), (70, 65, 75)]),
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
    delta = abs(value) * spread
    return value + rng.uniform(-delta, delta)


def _as_float(value: object) -> float | None:
    if value is None:
        return None
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def _rounded(value: float | None) -> float | None:
    if value is None:
        return None
    return round(value, 2)


def _clamp_triplet(
    minimum: float | None, target: float | None, maximum: float | None, floor: float, ceiling: float
) -> tuple[float | None, float | None, float | None]:
    minimum = None if minimum is None else min(max(minimum, floor), ceiling)
    target = None if target is None else min(max(target, floor), ceiling)
    maximum = None if maximum is None else min(max(maximum, floor), ceiling)
    return minimum, target, maximum
