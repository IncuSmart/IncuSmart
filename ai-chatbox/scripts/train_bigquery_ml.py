import json

from app.config import get_settings
from app.repositories.postgres_repository import PostgresRepository
from app.services.bigquery_ml_service import BigQueryMlService


def _synthetic_seed_rows(settings) -> list[dict]:
    path = settings.synthetic_data_path
    if not path.exists():
        return []
    try:
        records = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return []
    rows: list[dict] = []
    for index, record in enumerate(records):
        try:
            egg_type = str(record["egg_type"]).lower()
            total_eggs = int(record.get("total_eggs") or 100)
            success_rate = float(record["expected_success_rate"])
        except (KeyError, TypeError, ValueError):
            continue
        if total_eggs <= 0 or not 0 <= success_rate <= 1:
            continue
        rows.append(
            {
                "season_id": f"synthetic-{index + 1}",
                "egg_type": egg_type,
                "total_eggs": total_eggs,
                "ambient_temperature": float(record.get("ambient_temperature") or 0.0),
                "ambient_humidity": float(record.get("ambient_humidity") or 0.0),
                "success_rate": success_rate,
            }
        )
    return rows


def main() -> None:
    settings = get_settings()
    service = BigQueryMlService(settings)
    if not service.is_enabled():
        raise RuntimeError("Set AI_CHATBOX_BIGQUERY_ML_ENABLED=true and AI_CHATBOX_BIGQUERY_PROJECT_ID.")

    repository = PostgresRepository(settings)
    rows = repository.fetch_cloud_training_rows()
    source = "completed DB seasons"
    if len(rows) < 2:
        rows = _synthetic_seed_rows(settings)
        source = "synthetic seed rows"
    if len(rows) < 2:
        raise RuntimeError("Need at least two DB or synthetic rows before cloud training.")

    uploaded = service.sync_training_data(rows)
    service.train()
    print(f"Uploaded {uploaded} {source} and trained BigQuery ML model.")


if __name__ == "__main__":
    main()
