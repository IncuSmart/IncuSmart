from app.pipelines.ingest_rag_documents import ingest_documents


if __name__ == "__main__":
    count = ingest_documents()
    print(f"Ingested {count} chunks.")
