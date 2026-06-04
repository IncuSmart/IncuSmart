from __future__ import annotations

import json
import random

import joblib
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestRegressor
from sklearn.impute import SimpleImputer
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder

from app.config import get_settings
from app.repositories.postgres_repository import PostgresRepository


def _slugify(value: str) -> str:
    return "".join(ch.lower() if ch.isalnum() else "_" for ch in value).strip("_")


def build_training_frame(repository: PostgresRepository) -> pd.DataFrame:
    raw = repository.fetch_training_dataset()
    if raw.empty:
        return pd.DataFrame()

    rows: list[dict[str, float | str]] = []
    for season_id, group in raw.groupby("season_id"):
        row: dict[str, float | str] = {}
        base = group.iloc[0]
        total_eggs = float(base["total_eggs"] or 0.0)
        success_count = float(base["success_count"] or 0.0)
        row["egg_type"] = str(base["egg_type"])
        row["total_eggs"] = total_eggs
        row["phase_count"] = float(group["batch_index"].dropna().nunique())
        row["ambient_temperature"] = 0.0
        row["ambient_humidity"] = 0.0
        row["success_rate"] = success_count / total_eggs if total_eggs > 0 else 0.0

        for _, record in group.iterrows():
            if pd.isna(record["batch_index"]) or pd.isna(record["config_code"]):
                continue
            prefix = f"phase_{int(record['batch_index'])}_{_slugify(str(record['config_code']))}"
            for source, target in [("target_value", "target"), ("min_value", "min"), ("max_value", "max")]:
                value = record[source]
                if pd.notna(value):
                    row[f"{prefix}_{target}"] = float(value)
        rows.append(row)

    frame = pd.DataFrame(rows)
    return augment_with_synthetic_samples(frame)


def augment_with_synthetic_samples(frame: pd.DataFrame, copies_per_row: int = 3) -> pd.DataFrame:
    if frame.empty:
        return frame

    synthetic_rows: list[dict[str, float | str]] = []
    rng = random.Random(42)
    feature_columns = [column for column in frame.columns if column != "success_rate"]
    for _, record in frame.iterrows():
        for _ in range(copies_per_row):
            sample = record.to_dict()
            for column in feature_columns:
                value = sample[column]
                if isinstance(value, (int, float)) and column not in {"total_eggs", "phase_count"}:
                    jitter = abs(float(value)) * 0.05
                    sample[column] = float(value) + rng.uniform(-jitter, jitter)
            sample["success_rate"] = max(
                0.0,
                min(1.0, float(record["success_rate"]) + rng.uniform(-0.08, 0.08)),
            )
            synthetic_rows.append(sample)
    return pd.concat([frame, pd.DataFrame(synthetic_rows)], ignore_index=True)


def train_model() -> Path:
    settings = get_settings()
    repository = PostgresRepository(settings)
    frame = build_training_frame(repository)
    if frame.empty:
        raise RuntimeError("Training dataset is empty. Populate hatching season data first.")

    target = frame["success_rate"]
    features = frame.drop(columns=["success_rate"])

    categorical_columns = [column for column in features.columns if features[column].dtype == "object"]
    numeric_columns = [column for column in features.columns if column not in categorical_columns]

    preprocessor = ColumnTransformer(
        transformers=[
            (
                "numeric",
                Pipeline(
                    steps=[
                        ("imputer", SimpleImputer(strategy="constant", fill_value=0.0)),
                    ]
                ),
                numeric_columns,
            ),
            (
                "categorical",
                Pipeline(
                    steps=[
                        ("imputer", SimpleImputer(strategy="most_frequent")),
                        ("encoder", OneHotEncoder(handle_unknown="ignore")),
                    ]
                ),
                categorical_columns,
            ),
        ]
    )

    model = Pipeline(
        steps=[
            ("preprocessor", preprocessor),
            ("regressor", RandomForestRegressor(n_estimators=200, random_state=42)),
        ]
    )
    model.fit(features, target)

    model_path = settings.model_dir / "recommendation_model.joblib"
    features_path = settings.model_dir / "recommendation_features.json"
    joblib.dump(model, model_path)

    feature_columns = features.columns.tolist()
    features_path.write_text(json.dumps(feature_columns, ensure_ascii=False, indent=2), encoding="utf-8")
    return model_path


if __name__ == "__main__":
    path = train_model()
    print(f"Saved model to {path}")
