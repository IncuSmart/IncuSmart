from __future__ import annotations

import hashlib
from pathlib import Path

import chromadb
from sentence_transformers import SentenceTransformer

from app.config import get_settings


def chunk_text(text: str, chunk_size: int = 800, overlap: int = 120) -> list[str]:
    cleaned = " ".join(text.split())
    chunks: list[str] = []
    cursor = 0
    while cursor < len(cleaned):
        end = min(len(cleaned), cursor + chunk_size)
        chunks.append(cleaned[cursor:end])
        if end == len(cleaned):
            break
        cursor = max(0, end - overlap)
    return chunks


def load_text(path: Path) -> str:
    if path.suffix.lower() in {".md", ".txt"}:
        return path.read_text(encoding="utf-8", errors="ignore")
    if path.suffix.lower() == ".pdf":
        try:
            import pypdf
        except ImportError as exc:
            raise RuntimeError("Install pypdf to ingest PDF documents.") from exc
        reader = pypdf.PdfReader(str(path))
        return "\n".join(page.extract_text() or "" for page in reader.pages)
    raise ValueError(f"Unsupported document type: {path.suffix}")


def ingest_documents() -> int:
    settings = get_settings()
    client = chromadb.PersistentClient(path=str(settings.chroma_dir))
    collection = client.get_or_create_collection(name="incusmart_knowledge")
    embedding_model = SentenceTransformer(settings.embedding_model)

    doc_paths = [
        path
        for path in settings.docs_dir.rglob("*")
        if path.is_file() and path.suffix.lower() in {".md", ".txt", ".pdf"}
    ]

    total_chunks = 0
    for doc_path in doc_paths:
        text = load_text(doc_path)
        chunks = chunk_text(text)
        if not chunks:
            continue
        embeddings = embedding_model.encode(chunks).tolist()
        relative_source = str(doc_path.relative_to(settings.docs_dir)).replace("\\", "/")
        source_id = hashlib.sha256(relative_source.encode("utf-8")).hexdigest()[:16]
        ids = [f"{source_id}-{index}" for index, _ in enumerate(chunks)]
        metadatas = [
            {"source": relative_source, "section": f"chunk-{index}", "topic": doc_path.stem}
            for index, _ in enumerate(chunks)
        ]
        collection.upsert(ids=ids, documents=chunks, metadatas=metadatas, embeddings=embeddings)
        total_chunks += len(chunks)
    return total_chunks


if __name__ == "__main__":
    count = ingest_documents()
    print(f"Ingested {count} chunks.")
