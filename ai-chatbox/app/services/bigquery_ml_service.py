from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from app.config import Settings
from app.schemas import PredictSuccessRequest


@dataclass
class BigQueryPrediction:
    success_rate: float


class BigQueryMlService:
    """Cloud-only training and prediction through BigQuery ML."""

    def __init__(self, settings: Settings):
        self._settings = settings
        self._client: Any | None = None

    def is_enabled(self) -> bool:
        return bool(self._settings.bigquery_ml_enabled and self._settings.bigquery_project_id)

    def predict_success(self, payload: PredictSuccessRequest) -> BigQueryPrediction | None:
        if not self.is_enabled():
            return None

        bigquery = self._bigquery_module()
        query = f"""
            SELECT predicted_success_rate
            FROM ML.PREDICT(
              MODEL `{self._model_id}`,
              (
                SELECT
                  @egg_type AS egg_type,
                  @total_eggs AS total_eggs,
                  @ambient_temperature AS ambient_temperature,
                  @ambient_humidity AS ambient_humidity
              )
            )
            LIMIT 1
        """
        job_config = bigquery.QueryJobConfig(
            query_parameters=[
                bigquery.ScalarQueryParameter("egg_type", "STRING", payload.egg_type.lower()),
                bigquery.ScalarQueryParameter("total_eggs", "INT64", payload.total_eggs),
                bigquery.ScalarQueryParameter(
                    "ambient_temperature", "FLOAT64", payload.ambient_temperature or 0.0
                ),
                bigquery.ScalarQueryParameter(
                    "ambient_humidity", "FLOAT64", payload.ambient_humidity or 0.0
                ),
            ],
            maximum_bytes_billed=self._settings.bigquery_prediction_maximum_bytes_billed,
            job_timeout_ms=self._settings.bigquery_query_timeout_seconds * 1000,
        )
        job = self._get_client().query(
            query,
            job_config=job_config,
            location=self._settings.bigquery_location,
            timeout=self._settings.bigquery_query_timeout_seconds,
        )
        rows = list(job.result(timeout=self._settings.bigquery_query_timeout_seconds))
        if not rows:
            return None
        rate = min(1.0, max(0.0, float(rows[0]["predicted_success_rate"])))
        return BigQueryPrediction(success_rate=rate)

    def sync_training_data(self, rows: list[dict[str, Any]]) -> int:
        if not self.is_enabled():
            raise RuntimeError("BigQuery ML is not configured.")

        bigquery = self._bigquery_module()
        client = self._get_client()
        dataset = bigquery.Dataset(self._dataset_id)
        dataset.location = self._settings.bigquery_location
        client.create_dataset(dataset, exists_ok=True)

        normalized = [
            {
                "season_id": str(row["season_id"]),
                "egg_type": str(row["egg_type"]).lower(),
                "total_eggs": int(row["total_eggs"]),
                "ambient_temperature": float(row.get("ambient_temperature") or 0.0),
                "ambient_humidity": float(row.get("ambient_humidity") or 0.0),
                "success_rate": float(row["success_rate"]),
            }
            for row in rows
        ]
        job_config = bigquery.LoadJobConfig(
            schema=[
                bigquery.SchemaField("season_id", "STRING"),
                bigquery.SchemaField("egg_type", "STRING"),
                bigquery.SchemaField("total_eggs", "INT64"),
                bigquery.SchemaField("ambient_temperature", "FLOAT64"),
                bigquery.SchemaField("ambient_humidity", "FLOAT64"),
                bigquery.SchemaField("success_rate", "FLOAT64"),
            ],
            write_disposition=bigquery.WriteDisposition.WRITE_TRUNCATE,
        )
        client.load_table_from_json(
            normalized,
            self._table_id,
            job_config=job_config,
            location=self._settings.bigquery_location,
        ).result()
        return len(normalized)

    def train(self) -> None:
        if not self.is_enabled():
            raise RuntimeError("BigQuery ML is not configured.")

        query = f"""
            CREATE OR REPLACE MODEL `{self._model_id}`
            OPTIONS(
              model_type = 'LINEAR_REG',
              input_label_cols = ['success_rate'],
              data_split_method = 'AUTO_SPLIT'
            ) AS
            SELECT
              egg_type,
              total_eggs,
              ambient_temperature,
              ambient_humidity,
              success_rate
            FROM `{self._table_id}`
        """
        self._get_client().query(query, location=self._settings.bigquery_location).result()

    @property
    def _dataset_id(self) -> str:
        return f"{self._settings.bigquery_project_id}.{self._settings.bigquery_dataset}"

    @property
    def _table_id(self) -> str:
        return f"{self._dataset_id}.{self._settings.bigquery_training_table}"

    @property
    def _model_id(self) -> str:
        return f"{self._dataset_id}.{self._settings.bigquery_model}"

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
        try:
            from google.cloud import bigquery
        except ImportError as exc:
            raise RuntimeError("Install google-cloud-bigquery to use BigQuery ML.") from exc
        return bigquery

    def _load_credentials(self):
        if not self._settings.bigquery_credentials_path:
            return None
        from google.oauth2 import service_account

        return service_account.Credentials.from_service_account_file(
            str(self._settings.bigquery_credentials_path)
        )
