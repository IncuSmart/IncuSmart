from app.config import Settings
from app.services.llm_service import LlmService


class _ResponseStub:
    ok = True
    status_code = 200

    def json(self):
        return {"embeddings": [{"values": [0.1, 0.2, 0.3]}]}


def test_gemini_embedding_uses_ai_studio_batch_embed_endpoint(monkeypatch) -> None:
    captured = {}

    def fake_post(url, **kwargs):
        captured["url"] = url
        captured["params"] = kwargs["params"]
        captured["json"] = kwargs["json"]
        return _ResponseStub()

    monkeypatch.setattr("app.services.llm_service.requests.post", fake_post)
    service = LlmService(
        Settings(
            llm_provider="gemini",
            llm_api_key="test-key",
            llm_embedding_model="gemini-embedding-001",
        )
    )

    result = service.embed_texts(["hello"], "RETRIEVAL_DOCUMENT")

    assert result[0].values == [0.1, 0.2, 0.3]
    assert captured["url"].endswith("/gemini-embedding-001:batchEmbedContents")
    assert captured["params"] == {"key": "test-key"}
    assert captured["json"]["requests"][0]["model"] == "models/gemini-embedding-001"
    assert captured["json"]["requests"][0]["taskType"] == "RETRIEVAL_DOCUMENT"
