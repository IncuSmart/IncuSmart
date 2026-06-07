import hashlib

from app.config import get_settings
from app.pipelines.ingest_rag_documents import chunk_text, load_text
from app.services.bigquery_rag_service import BigQueryRagService
from app.services.llm_service import LlmService


def main() -> None:
    settings = get_settings()
    if not settings.bigquery_project_id:
        raise RuntimeError("Set AI_CHATBOX_BIGQUERY_PROJECT_ID before BigQuery RAG ingestion.")

    rows: list[dict[str, str]] = []
    paths = [
        path
        for path in settings.docs_dir.rglob("*")
        if path.is_file() and path.suffix.lower() in {".md", ".txt", ".pdf"}
    ]
    for path in paths:
        source = str(path.relative_to(settings.docs_dir)).replace("\\", "/")
        source_id = hashlib.sha256(source.encode("utf-8")).hexdigest()[:16]
        for index, content in enumerate(chunk_text(load_text(path))):
            rows.append(
                {
                    "chunk_id": f"{source_id}-{index}",
                    "source": source,
                    "section": f"chunk-{index}",
                    "topic": path.stem,
                    "content": content,
                }
            )

    uploaded = BigQueryRagService(settings, LlmService(settings)).upload_and_embed(rows)
    print(f"Uploaded and embedded {uploaded} RAG chunks in BigQuery.")


if __name__ == "__main__":
    main()
