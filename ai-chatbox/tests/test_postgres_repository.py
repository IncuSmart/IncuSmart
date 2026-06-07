import pandas as pd

from app.repositories.postgres_repository import PostgresRepository


def test_fetch_cloud_training_rows_deduplicates_season_batches() -> None:
    repository = object.__new__(PostgresRepository)
    repository.fetch_training_dataset = lambda: pd.DataFrame(
        [
            {
                "season_id": "season-1",
                "egg_type": "chicken",
                "total_eggs": 100,
                "success_count": 85,
                "ambient_temperature": 30.0,
                "ambient_humidity": 65.0,
                "batch_index": 1,
            },
            {
                "season_id": "season-1",
                "egg_type": "chicken",
                "total_eggs": 100,
                "success_count": 85,
                "ambient_temperature": 30.0,
                "ambient_humidity": 65.0,
                "batch_index": 2,
            },
        ]
    )

    rows = repository.fetch_cloud_training_rows()

    assert len(rows) == 1
    assert rows[0]["success_rate"] == 0.85
