from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from app.config import Settings
from app.services.llm_service import LlmService


@dataclass
class BigQueryRagChunk:
    source: str
    section: str | None
    topic: str | None
    content: str


class BigQueryRagService:
    def __init__(self, settings: Settings, llm_service: LlmService):
        self._settings = settings
        self._llm_service = llm_service
        self._client: Any | None = None

    def is_enabled(self) -> bool:
        return self._settings.rag_provider.lower() == "bigquery" and bool(
            self._settings.bigquery_project_id
        )

    def retrieve(self, question: str, top_k: int) -> list[BigQueryRagChunk]:
        bigquery = self._bigquery_module()
        query_embedding = self._llm_service.embed_texts([question], "RETRIEVAL_QUERY")[0].values
        query = f"""
            WITH query_embedding AS (
              SELECT @query_embedding AS embedding
            )
            SELECT base.source, base.section, base.topic, base.content
            FROM VECTOR_SEARCH(
              TABLE `{self._embeddings_table_id}`,
              'embedding',
              (SELECT embedding FROM query_embedding),
              top_k => @top_k,
              distance_type => 'COSINE'
            )
            ORDER BY distance
        """
        job_config = bigquery.QueryJobConfig(
            query_parameters=[
                bigquery.ArrayQueryParameter("query_embedding", "FLOAT64", query_embedding),
                bigquery.ScalarQueryParameter("top_k", "INT64", top_k),
            ],
            maximum_bytes_billed=self._settings.bigquery_rag_maximum_bytes_billed,
            job_timeout_ms=self._settings.bigquery_query_timeout_seconds * 1000,
        )
        job = self._get_client().query(
            query,
            job_config=job_config,
            location=self._settings.bigquery_location,
            timeout=self._settings.bigquery_query_timeout_seconds,
        )
        rows = job.result(timeout=self._settings.bigquery_query_timeout_seconds)
        return [
            BigQueryRagChunk(
                source=str(row["source"]),
                section=row["section"],
                topic=row["topic"],
                content=str(row["content"]),
            )
            for row in rows
        ]

    def upload_and_embed(self, chunks: list[dict[str, str]], batch_size: int = 16) -> int:
        bigquery = self._bigquery_module()
        client = self._get_client()
        dataset = bigquery.Dataset(self._dataset_id)
        dataset.location = self._settings.bigquery_location
        client.create_dataset(dataset, exists_ok=True)
        embedded_chunks: list[dict[str, Any]] = []
        for index in range(0, len(chunks), batch_size):
            batch = chunks[index : index + batch_size]
            embeddings = self._llm_service.embed_texts(
                [chunk["content"] for chunk in batch],
                "RETRIEVAL_DOCUMENT",
            )
            for chunk, embedding in zip(batch, embeddings):
                embedded_chunks.append({**chunk, "embedding": embedding.values})
        job_config = bigquery.LoadJobConfig(
            schema=[
                bigquery.SchemaField("chunk_id", "STRING"),
                bigquery.SchemaField("source", "STRING"),
                bigquery.SchemaField("section", "STRING"),
                bigquery.SchemaField("topic", "STRING"),
                bigquery.SchemaField("content", "STRING"),
                bigquery.SchemaField("embedding", "FLOAT64", mode="REPEATED"),
            ],
            write_disposition=bigquery.WriteDisposition.WRITE_TRUNCATE,
        )
        client.load_table_from_json(
            embedded_chunks,
            self._embeddings_table_id,
            job_config=job_config,
            location=self._settings.bigquery_location,
        ).result()
        return len(embedded_chunks)

    @property
    def _dataset_id(self) -> str:
        return f"{self._settings.bigquery_project_id}.{self._settings.bigquery_dataset}"

    @property
    def _embeddings_table_id(self) -> str:
        return f"{self._dataset_id}.{self._settings.bigquery_rag_embeddings_table}"

    def _get_client(self):
        if self._client is None:
            bigquery = self._bigquery_module()
            credentials = self._load_credentials()
            self._client = bigquery.Client(
                project=self._settings.bigquery_project_id,
                location=self._settings.bigquery_location,
                credentials=credentials,
            )
        return self._client

    @staticmethod
    def _bigquery_module():
        from google.cloud import bigquery

        return bigquery

    def _load_credentials(self):
        if not self._settings.bigquery_credentials_path:
            return None
        from google.oauth2 import service_account

        return service_account.Credentials.from_service_account_file(
            str(self._settings.bigquery_credentials_path)
        )
