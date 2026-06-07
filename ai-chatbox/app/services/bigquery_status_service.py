from __future__ import annotations

from app.config import Settings
from app.schemas import BigQueryStatusResponse


class BigQueryStatusService:
    def __init__(self, settings: Settings):
        self._settings = settings

    def status(self) -> BigQueryStatusResponse:
        response = BigQueryStatusResponse(
            enabled=self._settings.bigquery_ml_enabled or self._settings.rag_provider.lower() == "bigquery",
            project_id_configured=bool(self._settings.bigquery_project_id),
            dataset=self._settings.bigquery_dataset,
            location=self._settings.bigquery_location,
            training_table=self._settings.bigquery_training_table,
            model=self._settings.bigquery_model,
            rag_provider=self._settings.rag_provider,
            rag_embeddings_table=self._settings.bigquery_rag_embeddings_table,
            rag_embedding_model=self._settings.llm_embedding_model,
            credentials_available=False,
        )
        if not self._settings.bigquery_project_id:
            response.errors.append("AI_CHATBOX_BIGQUERY_PROJECT_ID is missing.")
            return response

        try:
            from google.auth.exceptions import DefaultCredentialsError
            from google.cloud import bigquery
        except ImportError:
            response.errors.append("google-cloud-bigquery is not installed.")
            return response

        try:
            credentials = self._load_credentials()
            client = bigquery.Client(
                project=self._settings.bigquery_project_id,
                location=self._settings.bigquery_location,
                credentials=credentials,
            )
            response.credentials_available = True
        except DefaultCredentialsError:
            response.errors.append("Google Application Default Credentials are missing.")
            return response

        dataset_id = f"{self._settings.bigquery_project_id}.{self._settings.bigquery_dataset}"
        timeout = self._settings.bigquery_query_timeout_seconds
        response.dataset_exists = self._exists(lambda: client.get_dataset(dataset_id, timeout=timeout))
        response.training_table_exists = self._exists(
            lambda: client.get_table(f"{dataset_id}.{self._settings.bigquery_training_table}", timeout=timeout)
        )
        response.model_exists = self._exists(
            lambda: client.get_model(f"{dataset_id}.{self._settings.bigquery_model}", timeout=timeout)
        )
        response.rag_embeddings_table_exists = self._exists(
            lambda: client.get_table(f"{dataset_id}.{self._settings.bigquery_rag_embeddings_table}", timeout=timeout)
        )
        return response

    @staticmethod
    def _exists(fetch) -> bool:
        try:
            fetch()
            return True
        except Exception:
            return False

    def _load_credentials(self):
        if not self._settings.bigquery_credentials_path:
            return None
        from google.oauth2 import service_account

        return service_account.Credentials.from_service_account_file(
            str(self._settings.bigquery_credentials_path)
        )
