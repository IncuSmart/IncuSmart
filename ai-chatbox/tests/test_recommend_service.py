import json
import math

import pytest

from app.config import Settings
from app.repositories.postgres_repository import TemplateConfigRow
from app.schemas import PredictSuccessRequest, RecommendedParameter, RecommendedPhase
from app.services.recommend_service import RecommendService, _feature_distance
from app.services.bigquery_ml_service import BigQueryPrediction


@pytest.fixture(autouse=True)
def _isolate_default_synthetic_path(monkeypatch, tmp_path) -> None:
    monkeypatch.setenv("AI_CHATBOX_SYNTHETIC_DATA_PATH", str(tmp_path / "default-missing.json"))
    monkeypatch.setenv("AI_CHATBOX_MODEL_DIR", str(tmp_path / "default-models"))
    monkeypatch.setenv("AI_CHATBOX_USE_PREBUILT_MODEL", "false")


def _complete_synthetic_phases(egg_type="chicken"):
    phase_days = [(1, 7), (8, 18), (19, 21)] if egg_type == "chicken" else [(1, 10), (11, 25), (26, 28)]
    phases = []
    for index, (day_start, day_end) in enumerate(phase_days, start=1):
        phases.append(
            {
                "phase_index": index,
                "day_start": day_start,
                "day_end": day_end,
                "parameters": [
                    {
                        "config_code": "TEMP",
                        "target_value": 37.2 if index == 3 else 37.6,
                        "min_value": 37.0,
                        "max_value": 38.0,
                        "unit": "C",
                    },
                    {
                        "config_code": "HUMID",
                        "target_value": 70 if index == 3 else 60,
                        "min_value": 55,
                        "max_value": 75,
                        "unit": "%",
                    },
                    {
                        "config_code": "TURN",
                        "target_value": 0 if index == 3 else 6,
                        "min_value": 0 if index == 3 else 4,
                        "max_value": 1 if index == 3 else 8,
                        "unit": "times/day",
                    },
                    {
                        "config_code": "FAN",
                        "target_value": 2,
                        "min_value": 1,
                        "max_value": 3,
                        "unit": "level",
                    },
                ],
            }
        )
    return phases


class _RepoStub:
    def fetch_template_configs(self):
        return []

    def fetch_training_dataset(self):
        import pandas as pd

        return pd.DataFrame()


class _RepoFailingStub:
    def fetch_template_configs(self):
        raise RuntimeError("db unavailable")


class _RepoHardwareStub(_RepoStub):
    def fetch_incubator_config_limits(self, incubator_id):
        assert incubator_id == "incubator-1"
        return {
            "temp": (36.8, 37.0),
            "humid": (50.0, 60.0),
        }


class _RepoDbLabeledStub(_RepoStub):
    def fetch_training_dataset(self):
        import pandas as pd

        rows = []
        for season_index, target in enumerate([37.2, 37.5, 37.8], start=1):
            rows.append(
                {
                    "season_id": f"season-{season_index}",
                    "egg_type": "chicken",
                    "total_eggs": 100 + season_index * 50,
                    "success_count": 70 + season_index * 5,
                    "ambient_temperature": 30 + season_index,
                    "ambient_humidity": 60 + season_index,
                    "batch_index": 1,
                    "day_start": 1,
                    "day_end": 21,
                    "config_code": "TEMP",
                    "config_name": "Temperature",
                    "config_unit": "C",
                    "target_value": target,
                    "min_value": 37.0,
                    "max_value": 38.0,
                }
            )
        return pd.DataFrame(rows)


class _RepoInvalidTemplateStub(_RepoStub):
    def fetch_template_configs(self):
        return [
            TemplateConfigRow(
                template_id="invalid-template",
                template_name="Missing humidity",
                egg_type="chicken",
                batch_index=index,
                phase_name=f"Phase {index}",
                number_of_days=7,
                config_id=f"temp-{index}",
                config_code="TEMP",
                config_name="Temperature",
                config_type=None,
                config_unit="C",
                target_value=37.5,
                min_value=37.0,
                max_value=38.0,
            )
            for index in range(1, 4)
        ]


class _BigQueryMlStub:
    def is_enabled(self):
        return True

    def predict_success(self, payload):
        assert payload.egg_type == "chicken"
        return BigQueryPrediction(success_rate=0.91)


def test_recommend_uses_default_phases_without_db_templates() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoStub())

    result = service.recommend(
        "recommend config for 300 eggs",
        {"ambient_temperature": 31, "ambient_humidity": 67},
    )

    assert result.scoring_mode == "heuristic"
    assert result.estimated_success_rate is None
    assert result.estimated_success_confidence is None
    assert result.model_artifact_version is None
    assert result.validation_warnings == []
    assert result.template_source == "built_in_default"
    assert len(result.phases) == 3


def test_validate_recommended_config_detects_structural_and_numeric_errors() -> None:
    service = RecommendService(Settings(llm_provider="none"), _RepoStub())
    phases = [
        RecommendedPhase(
            phase_index=2,
            phase_name="Broken",
            day_start=2,
            day_end=1,
            parameters=[
                RecommendedParameter(
                    config_code="TEMP",
                    config_name="Temperature",
                    target_value=math.nan,
                    min_value=38,
                    max_value=37,
                    unit="C",
                ),
                RecommendedParameter(
                    config_code="TEMP",
                    config_name="Temperature duplicate",
                    target_value=37.5,
                    min_value=37,
                    max_value=38,
                    unit="C",
                ),
            ],
        )
    ]

    issues = service._validate_recommended_config(phases)

    assert "invalid_phase_count" in issues
    assert "invalid_phase_indexes" in issues
    assert "phase_2_invalid_days" in issues
    assert "phase_2_missing_core_parameters" in issues
    assert "phase_2_temp_duplicate_parameter" in issues
    assert "phase_2_temp_non_finite_value" in issues


def test_validate_recommended_config_rejects_missing_parameter_values() -> None:
    service = RecommendService(Settings(llm_provider="none"), _RepoStub())
    phases = service._default_phases("chicken")
    phases[0].parameters[0].min_value = None

    issues = service._validate_recommended_config(phases)

    assert "phase_1_temp_missing_value" in issues


def test_recommend_falls_back_when_selected_template_is_invalid() -> None:
    service = RecommendService(
        Settings(llm_provider="none", use_prebuilt_model=False),
        _RepoInvalidTemplateStub(),
    )

    result = service.recommend("de xuat cau hinh cho 100 trung ga", None)

    assert result.scoring_mode == "heuristic"
    assert result.estimated_success_rate is None
    assert result.model_artifact_version is None
    assert "selected_candidate_invalid_fallback_applied" in result.validation_warnings
    assert any("missing_core_parameters" in warning for warning in result.validation_warnings)
    assert service._validate_recommended_config(result.phases) == []


def test_recommend_parses_vietnamese_without_accents() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoStub())

    result = service.recommend("de xuat cau hinh cho 12 trung vit", None)

    assert "12 trứng duck" in result.answer
    assert result.phases[-1].day_end == 28


def test_recommend_uses_species_specific_default_schedule() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoStub())

    quail = service.recommend("de xuat cau hinh cho 100 trung cut", None)
    goose = service.recommend("de xuat cau hinh cho 100 trung ngong", None)

    assert quail.phases[-1].day_end == 18
    assert goose.phases[-1].day_end == 30


def test_recommend_loads_matching_synthetic_candidates(tmp_path) -> None:
    synthetic_path = tmp_path / "synthetic.json"
    synthetic_path.write_text(
        json.dumps(
            [
                    {
                        "egg_type": "duck",
                        "total_eggs": 100,
                        "expected_success_rate": 0.82,
                        "phases": _complete_synthetic_phases("duck"),
                    },
                {"egg_type": "chicken", "phases": []},
            ]
        ),
        encoding="utf-8",
    )
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
        synthetic_data_path=synthetic_path,
    )
    service = RecommendService(settings, _RepoStub())
    request = service._parse_request("de xuat cho trung vit", None)

    candidates = service._load_synthetic_candidates(request)

    assert len(candidates) == 1
    assert candidates[0][0].parameters[0].config_code == "TEMP"


def test_recommend_rejects_incomplete_synthetic_output_candidates(tmp_path) -> None:
    synthetic_path = tmp_path / "synthetic.json"
    synthetic_path.write_text(
        json.dumps(
            [
                {
                    "egg_type": "chicken",
                    "phases": [
                        {
                            "phase_index": 1,
                            "day_start": 1,
                            "day_end": 21,
                            "parameters": [{"config_code": "TEMP", "target_value": 37.5}],
                        }
                    ],
                }
            ]
        ),
        encoding="utf-8",
    )
    service = RecommendService(
        Settings(llm_provider="none", synthetic_data_path=synthetic_path),
        _RepoStub(),
    )
    request = service._parse_request("de xuat cho trung ga", None)

    assert service._load_synthetic_candidates(request) == []


def test_recommend_uses_knn_when_labeled_samples_are_available(tmp_path) -> None:
    synthetic_path = tmp_path / "synthetic.json"
    records = []
    for index, temperature in enumerate([37.2, 37.5, 37.8], start=1):
        records.append(
            {
                "egg_type": "chicken",
                "total_eggs": 100 * index,
                "ambient_temperature": 30,
                "ambient_humidity": 65,
                "expected_success_rate": 0.7 + index * 0.05,
                "phases": _complete_synthetic_phases(),
            }
        )
        records[-1]["phases"][0]["parameters"][0]["target_value"] = temperature
    synthetic_path.write_text(json.dumps(records), encoding="utf-8")
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
        synthetic_data_path=synthetic_path,
        knn_min_samples=3,
    )
    service = RecommendService(settings, _RepoStub())

    result = service.recommend("de xuat cau hinh cho 200 trung ga", None)

    assert result.scoring_mode == "knn_inference"
    assert result.estimated_success_rate is not None
    assert result.estimated_success_confidence is not None
    assert 0 <= result.estimated_success_rate <= 1
    assert 0 <= result.estimated_success_confidence <= 1


def test_recommend_uses_db_outcomes_as_knn_labels(tmp_path) -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
        synthetic_data_path=tmp_path / "missing.json",
        knn_min_samples=3,
    )
    service = RecommendService(settings, _RepoDbLabeledStub())

    result = service.recommend("de xuat cau hinh cho 100 trung ga", None)

    assert result.scoring_mode == "knn_inference"
    assert result.estimated_success_rate is not None


def test_predict_success_prefers_completed_db_seasons(tmp_path) -> None:
    service = RecommendService(
        Settings(
            llm_provider="none",
            use_prebuilt_model=False,
            synthetic_data_path=tmp_path / "missing.json",
            knn_min_samples=3,
        ),
        _RepoDbLabeledStub(),
    )

    result = service.predict_success(PredictSuccessRequest(egg_type="chicken", total_eggs=200))

    assert result.prediction_mode == "db_knn"
    assert result.db_completed_seasons == 3
    assert result.synthetic_references == 0
    assert result.predicted_success_percent is not None
    assert 0 <= result.predicted_success_percent <= 100


def test_predict_success_prefers_cloud_trained_bigquery_model(tmp_path) -> None:
    service = RecommendService(
        Settings(
            llm_provider="none",
            bigquery_ml_enabled=True,
            bigquery_project_id="incusmart-test",
            synthetic_data_path=tmp_path / "missing.json",
        ),
        _RepoStub(),
        _BigQueryMlStub(),
    )

    result = service.predict_success(PredictSuccessRequest(egg_type="chicken", total_eggs=200))

    assert result.prediction_mode == "bigquery_ml"
    assert result.predicted_success_percent == 91.0
    assert result.synthetic_references == 0


def test_predict_success_uses_synthetic_only_as_cold_start(tmp_path) -> None:
    synthetic_path = tmp_path / "synthetic.json"
    synthetic_path.write_text(
        json.dumps(
            [
                {
                    "egg_type": "chicken",
                    "total_eggs": 100 + index * 50,
                    "expected_success_rate": 0.75 + index * 0.03,
                    "phases": _complete_synthetic_phases(),
                }
                for index in range(3)
            ]
        ),
        encoding="utf-8",
    )
    service = RecommendService(
        Settings(
            llm_provider="none",
            use_prebuilt_model=False,
            synthetic_data_path=synthetic_path,
            knn_min_samples=3,
        ),
        _RepoStub(),
    )

    result = service.predict_success(PredictSuccessRequest(egg_type="chicken", total_eggs=200))

    assert result.prediction_mode == "synthetic_knn"
    assert result.db_completed_seasons == 0
    assert result.synthetic_references == 3
    assert result.predicted_success_rate is not None


def test_predict_success_reports_insufficient_data(tmp_path) -> None:
    service = RecommendService(
        Settings(
            llm_provider="none",
            use_prebuilt_model=False,
            synthetic_data_path=tmp_path / "missing.json",
            knn_min_samples=3,
        ),
        _RepoStub(),
    )

    result = service.predict_success(PredictSuccessRequest(egg_type="duck", total_eggs=200))

    assert result.prediction_mode == "insufficient_data"
    assert result.predicted_success_rate is None
    assert result.db_completed_seasons == 0


def test_ml_status_reports_ready_labeled_references(tmp_path) -> None:
    settings = Settings(
        llm_provider="none",
        synthetic_data_path=tmp_path / "missing.json",
        knn_min_samples=3,
    )
    service = RecommendService(settings, _RepoDbLabeledStub())

    status = service.ml_status("gà")

    assert status.egg_type == "chicken"
    assert status.db_labeled_references == 3
    assert status.synthetic_labeled_references == 0
    assert status.effective_unique_references == 3
    assert status.ready_for_knn is True
    assert status.db_reference_weight > status.synthetic_reference_weight


def test_knn_source_weight_prefers_db_at_equal_distance() -> None:
    settings = Settings(
        llm_provider="none",
        knn_neighbors=2,
    )
    service = RecommendService(settings, _RepoStub())

    prediction, confidence = service._predict_success_from_references(
        {"temperature": 0.5},
        [
            ({"temperature": 0.5}, 0.2, 1.0),
            ({"temperature": 0.5}, 0.9, 0.1),
        ],
    )

    assert prediction < 0.4
    assert 0 <= confidence <= 1


def test_knn_leave_one_out_evaluation_returns_metrics(tmp_path) -> None:
    service = RecommendService(
        Settings(
            llm_provider="none",
            synthetic_data_path=tmp_path / "missing.json",
        ),
        _RepoDbLabeledStub(),
    )

    evaluation = service.evaluate_knn("chicken")

    assert evaluation.sample_count == 3
    assert evaluation.mae is not None
    assert evaluation.baseline_mae is not None
    assert evaluation.mean_confidence is not None


def test_knn_evaluation_returns_null_metrics_when_samples_are_insufficient(tmp_path) -> None:
    service = RecommendService(
        Settings(
            llm_provider="none",
            synthetic_data_path=tmp_path / "missing.json",
        ),
        _RepoStub(),
    )

    evaluation = service.evaluate_knn("chicken")

    assert evaluation.sample_count == 0
    assert evaluation.mae is None


def test_clear_ml_cache_resets_loaded_references(tmp_path) -> None:
    service = RecommendService(
        Settings(llm_provider="none", synthetic_data_path=tmp_path / "missing.json"),
        _RepoDbLabeledStub(),
    )
    service.ml_status("chicken")

    result = service.clear_ml_cache()

    assert result.cleared_db_reference_groups == 1


def test_export_training_rows_combines_db_and_synthetic(tmp_path) -> None:
    synthetic_path = tmp_path / "synthetic.json"
    synthetic_path.write_text(
        json.dumps(
            [
                {
                    "egg_type": "chicken",
                    "total_eggs": 100,
                    "ambient_temperature": 30,
                    "ambient_humidity": 65,
                    "expected_success_rate": 0.84,
                    "phases": _complete_synthetic_phases(),
                }
            ]
        ),
        encoding="utf-8",
    )
    service = RecommendService(
        Settings(llm_provider="none", synthetic_data_path=synthetic_path),
        _RepoDbLabeledStub(),
    )

    rows = service.export_training_rows()

    assert len(rows) == 4
    assert {row["data_source"] for row in rows} == {"db", "synthetic"}
    assert all("success_rate" in row for row in rows)
    assert all("egg_type" in row for row in rows)
    synthetic_row = next(row for row in rows if row["data_source"] == "synthetic")
    assert synthetic_row["success_rate"] != 0.84
    assert "gemini_expected_success_rate" not in synthetic_row


def test_model_artifact_status_reports_missing_artifacts(tmp_path) -> None:
    service = RecommendService(
        Settings(llm_provider="none", model_dir=tmp_path),
        _RepoStub(),
    )

    status = service.model_artifact_status()

    assert status.model_exists is False
    assert status.features_exists is False
    assert status.loadable is False
    assert status.quality_gate_passed is False
    assert status.validation_coverage_matches is False
    assert "model_missing" in status.rejection_reasons


def test_ml_benchmark_compares_available_scorers(tmp_path) -> None:
    service = RecommendService(
        Settings(
            llm_provider="none",
            synthetic_data_path=tmp_path / "missing.json",
            knn_min_samples=3,
        ),
        _RepoDbLabeledStub(),
    )

    benchmark = service.benchmark_recommend("de xuat cau hinh cho 100 trung ga", None)
    by_mode = {result.scoring_mode: result for result in benchmark.results}

    assert benchmark.egg_type == "chicken"
    assert benchmark.candidate_count >= 1
    assert by_mode["heuristic"].available is True
    assert by_mode["heuristic"].passed_output_validation is True
    assert by_mode["knn_inference"].available is True
    assert by_mode["prebuilt_model"].available is False


def test_feature_distance_penalizes_missing_features() -> None:
    exact = _feature_distance({"temperature": 0.5, "humidity": 0.6}, {"temperature": 0.5, "humidity": 0.6})
    missing = _feature_distance({"temperature": 0.5, "humidity": 0.6}, {"temperature": 0.5})

    assert exact == 0
    assert missing > exact


def test_knn_deduplication_prefers_higher_trust_source() -> None:
    service = RecommendService(Settings(llm_provider="none"), _RepoStub())

    references = service._deduplicate_references(
        [
            ({"temperature": 0.5}, 0.9, 0.6),
            ({"temperature": 0.5}, 0.2, 1.0),
        ]
    )

    assert references == [({"temperature": 0.5}, 0.2, 1.0)]


def test_recommend_clamps_output_to_incubator_hardware_limits() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoHardwareStub())

    result = service.recommend(
        "de xuat cau hinh cho 100 trung ga",
        {"incubator_id": "incubator-1"},
    )

    temperatures = [
        parameter
        for phase in result.phases
        for parameter in phase.parameters
        if parameter.config_code == "TEMP"
    ]
    humidities = [
        parameter
        for phase in result.phases
        for parameter in phase.parameters
        if parameter.config_code == "HUMID"
    ]
    assert all(parameter.max_value <= 37.0 for parameter in temperatures)
    assert all(parameter.target_value <= 37.0 for parameter in temperatures)
    assert all(parameter.max_value <= 60.0 for parameter in humidities)
    assert all(parameter.target_value <= 60.0 for parameter in humidities)


def test_recommend_falls_back_when_db_template_fetch_fails() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoFailingStub())

    result = service.recommend(
        "recommend config for 300 eggs",
        {"ambient_temperature": 31, "ambient_humidity": 67},
    )

    assert result.scoring_mode == "heuristic"
    assert result.template_source == "built_in_default:db_fallback"
    assert "DB template" in result.answer
    assert len(result.phases) == 3


def test_debug_recommend_returns_ranked_candidates() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoStub())

    debug = service.debug_recommend(
        "recommend config for 300 eggs",
        {"ambient_temperature": 31, "ambient_humidity": 67},
    )

    assert debug.scoring_mode == "heuristic"
    assert debug.estimated_success_rate is None
    assert debug.effective_unique_references == 0
    assert len(debug.top_candidates) >= 1
    assert debug.top_candidates[0].rank == 1
    assert debug.top_candidates[0].passed_output_validation is True
    assert debug.top_candidates[0].validation_warnings == []


def test_debug_and_benchmark_expose_invalid_selected_candidate() -> None:
    service = RecommendService(
        Settings(llm_provider="none", use_prebuilt_model=False),
        _RepoInvalidTemplateStub(),
    )

    debug = service.debug_recommend("de xuat cau hinh cho 100 trung ga", None)
    benchmark = service.benchmark_recommend("de xuat cau hinh cho 100 trung ga", None)
    heuristic = next(result for result in benchmark.results if result.scoring_mode == "heuristic")

    assert debug.top_candidates[0].passed_output_validation is False
    assert any("missing_core_parameters" in warning for warning in debug.top_candidates[0].validation_warnings)
    assert heuristic.passed_output_validation is False
    assert any("missing_core_parameters" in warning for warning in heuristic.validation_warnings)


def test_debug_recommend_falls_back_when_db_template_fetch_fails() -> None:
    settings = Settings(
        llm_provider="none",
        use_prebuilt_model=False,
    )
    service = RecommendService(settings, _RepoFailingStub())

    debug = service.debug_recommend(
        "recommend config for 300 eggs",
        {"ambient_temperature": 31, "ambient_humidity": 67},
    )

    assert debug.scoring_mode == "heuristic"
    assert debug.template_source == "built_in_default:db_fallback"
    assert "DB template" in debug.answer_preview
    assert len(debug.top_candidates) >= 1


def test_broken_prebuilt_model_falls_back_to_heuristic(tmp_path) -> None:
    model_dir = tmp_path / "models"
    model_dir.mkdir()
    (model_dir / "recommendation_model.joblib").write_text("broken", encoding="utf-8")
    (model_dir / "recommendation_features.json").write_text('["egg_type"]', encoding="utf-8")
    service = RecommendService(
        Settings(
            llm_provider="none",
            use_prebuilt_model=True,
            model_dir=model_dir,
            synthetic_data_path=tmp_path / "missing.json",
        ),
        _RepoStub(),
    )

    result = service.recommend("recommend config for 100 eggs", None)

    assert result.scoring_mode == "heuristic"


def test_prebuilt_model_rejects_predictions_outside_success_rate_range() -> None:
    class _InvalidPredictionModel:
        def predict(self, frame):
            return [1.5] * len(frame)

    service = RecommendService(Settings(llm_provider="none"), _RepoStub())
    service._prebuilt_model_supports_egg_type = lambda egg_type: True
    service._load_prebuilt_model = lambda: (_InvalidPredictionModel(), ["egg_type"])
    request = service._parse_request("recommend config for 100 chicken eggs", None)

    scored = service._score_candidates_with_prebuilt_model([service._default_phases("chicken")], request)

    assert scored == []
