from app.config import Settings
from app.services.bigquery_rag_service import BigQueryRagChunk
from app.services.llm_service import LlmResult
from app.services.rag_service import RagService


class _LlmStub:
    def complete(self, prompt):
        assert "Context:" in prompt
        return LlmResult(text="Câu trả lời từ tài liệu cloud.")


class _BigQueryRagStub:
    def is_enabled(self):
        return True

    def retrieve(self, question, top_k):
        assert question == "Nhiệt độ ấp?"
        assert top_k == 4
        return [
            BigQueryRagChunk(
                source="guide.pdf",
                section="chunk-1",
                topic="temperature",
                content="Nhiệt độ cần được kiểm soát ổn định.",
            )
        ]

    def count_chunks(self):
        return 12

    def upload_and_embed(self, chunks, replace=True):
        assert replace is False
        assert chunks[0]["content"]
        return len(chunks)


def test_rag_uses_bigquery_without_loading_local_chroma() -> None:
    service = RagService(
        Settings(rag_provider="bigquery", bigquery_project_id="incusmart-test"),
        _LlmStub(),
        _BigQueryRagStub(),
    )

    result = service.answer("Nhiệt độ ấp?")

    assert result.answer == "Câu trả lời từ tài liệu cloud."
    assert result.sources[0].source == "guide.pdf"
    assert not hasattr(service, "_embedding_model")


def test_bigquery_rag_admin_methods_do_not_load_chromadb() -> None:
    service = RagService(
        Settings(rag_provider="bigquery", bigquery_project_id="incusmart-test"),
        _LlmStub(),
        _BigQueryRagStub(),
    )

    assert service.get_chunk_count() == 12
    assert service.ingest_text("Một đoạn hướng dẫn ấp trứng.", "guide.md", "guide") == 1
