from app.config import Settings
from app.services.recommend_service import RecommendService


class _RepoStub:
    def fetch_template_configs(self):
        return []


class _RepoFailingStub:
    def fetch_template_configs(self):
        raise RuntimeError("db unavailable")


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
    assert result.template_source == "built_in_default"
    assert len(result.phases) == 3


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
    assert len(debug.top_candidates) >= 1
    assert debug.top_candidates[0].rank == 1


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
