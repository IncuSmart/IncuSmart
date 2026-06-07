import json

from app.config import Settings
from scripts.train_bigquery_ml import _synthetic_seed_rows


def test_synthetic_seed_rows_supports_cloud_training_when_db_is_empty(tmp_path) -> None:
    synthetic_path = tmp_path / "synthetic.json"
    synthetic_path.write_text(
        json.dumps(
            [
                {
                    "egg_type": "chicken",
                    "total_eggs": 100,
                    "ambient_temperature": 28,
                    "ambient_humidity": 62,
                    "expected_success_rate": 0.84,
                }
            ]
        ),
        encoding="utf-8",
    )

    rows = _synthetic_seed_rows(Settings(synthetic_data_path=synthetic_path))

    assert rows == [
        {
            "season_id": "synthetic-1",
            "egg_type": "chicken",
            "total_eggs": 100,
            "ambient_temperature": 28.0,
            "ambient_humidity": 62.0,
            "success_rate": 0.84,
        }
    ]
