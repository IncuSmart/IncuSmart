from app.config import Settings
from app.main import create_app
from app.schemas import ChatRequest
from app.services.chat_orchestrator import ChatOrchestrator
from app.services.intent_router import IntentRouter
from fastapi.testclient import TestClient


def _route_paths(enable_debug_endpoints: bool) -> set[str]:
    app = create_app(Settings(enable_debug_endpoints=enable_debug_endpoints))
    return {route.path for route in app.routes}


def test_debug_endpoints_are_disabled_by_default() -> None:
    paths = _route_paths(enable_debug_endpoints=False)

    assert "/chat" in paths
    assert "/predict-success" in paths
    assert "/debug/recommend" not in paths
    assert "/debug/knowledge" not in paths


def test_debug_endpoints_can_be_enabled_explicitly() -> None:
    paths = _route_paths(enable_debug_endpoints=True)

    assert "/debug/recommend" in paths
    assert "/debug/knowledge" in paths
    assert "/debug/ml-status" in paths
    assert "/debug/ml-evaluation" in paths
    assert "/debug/ml-cache/clear" in paths
    assert "/debug/model-artifact-status" in paths
    assert "/debug/bigquery-status" in paths
    assert "/debug/ml-benchmark" in paths


def test_chat_preflight_allows_browser_cors() -> None:
    app = create_app(Settings(cors_origins="*"))
    client = TestClient(app)

    response = client.options(
        "/chat",
        headers={
            "Origin": "https://api-incusmart.io.vn",
            "Access-Control-Request-Method": "POST",
            "Access-Control-Request-Headers": "content-type",
        },
    )

    assert response.status_code == 200
    assert response.headers["access-control-allow-origin"] == "*"


class _RagShouldNotRun:
    def answer(self, question):
        raise AssertionError("RAG should be disabled")


class _RecommendStub:
    pass


def test_knowledge_disabled_blocks_rag_calls() -> None:
    orchestrator = ChatOrchestrator(
        settings=Settings(knowledge_enabled=False),
        router=IntentRouter(),
        rag_service=_RagShouldNotRun(),
        recommend_service=_RecommendStub(),
    )

    response = orchestrator.handle_chat(ChatRequest(message="nhiệt độ ấp là gì?", session_id="s1"))

    assert response.intent == "knowledge"
    assert response.sources == []
    assert "tạm tắt" in response.answer
