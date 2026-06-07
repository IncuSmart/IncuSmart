from __future__ import annotations

import json
import math
import hashlib
from pathlib import Path

import joblib
import pandas as pd
import sklearn

from app.config import get_settings


def main() -> None:
    settings = get_settings()
    model_path = Path(settings.model_dir) / "recommendation_model.joblib"
    features_path = Path(settings.model_dir) / "recommendation_features.json"
    if not model_path.exists() or not features_path.exists():
        raise SystemExit(f"Missing model artifacts in {settings.model_dir}")

    feature_columns = json.loads(features_path.read_text(encoding="utf-8"))
    if not isinstance(feature_columns, list) or not feature_columns:
        raise SystemExit("recommendation_features.json must contain a non-empty JSON array.")

    sample = {
        column: "chicken" if column == "egg_type" else 0.0
        for column in feature_columns
    }
    model = joblib.load(model_path)
    prediction = float(model.predict(pd.DataFrame([sample]))[0])
    if not math.isfinite(prediction) or not 0 <= prediction <= 1:
        raise SystemExit("Model returned a sample prediction outside the valid success-rate range [0, 1].")

    metrics_path = Path(settings.model_dir) / "recommendation_metrics.json"
    manifest_path = Path(settings.model_dir) / "recommendation_manifest.json"
    print(f"Artifact validation passed. Sample prediction: {prediction:.4f}")
    print(f"Feature columns: {len(feature_columns)}")
    if not manifest_path.exists():
        raise SystemExit("Missing recommendation_manifest.json.")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    schema_json = json.dumps(feature_columns, ensure_ascii=False, separators=(",", ":"))
    schema_hash = hashlib.sha256(schema_json.encode("utf-8")).hexdigest()
    if not isinstance(manifest, dict) or manifest.get("artifact_version") != 1:
        raise SystemExit("Unsupported or invalid recommendation_manifest.json.")
    if manifest.get("feature_schema_sha256") != schema_hash:
        raise SystemExit("Feature schema hash does not match artifact manifest.")
    print("Artifact manifest validation passed.")
    if settings.prebuilt_require_metrics and not metrics_path.exists():
        raise SystemExit("Missing recommendation_metrics.json.")
    if metrics_path.exists():
        print("Training metrics:")
        metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
        if not isinstance(metrics, dict):
            raise SystemExit("Artifact metrics must contain a JSON object.")
        supported_egg_types = set(manifest.get("supported_egg_types") or [])
        validation_egg_types = set(metrics.get("validation_egg_types") or [])
        if not supported_egg_types or validation_egg_types != supported_egg_types:
            raise SystemExit(
                "Artifact validation coverage does not match its supported egg types. "
                f"supported={sorted(supported_egg_types)}, validation={sorted(validation_egg_types)}"
            )
        per_egg_type_mae = metrics.get("per_egg_type_mae")
        if not isinstance(per_egg_type_mae, dict) or set(per_egg_type_mae) != supported_egg_types:
            raise SystemExit("Artifact per_egg_type_mae does not cover every supported egg type.")
        invalid_per_egg_type_mae = {
            egg_type: value
            for egg_type, value in per_egg_type_mae.items()
            if not isinstance(value, (int, float))
            or not math.isfinite(float(value))
            or float(value) > settings.prebuilt_max_mae
        }
        if invalid_per_egg_type_mae:
            raise SystemExit(f"Artifact per-egg-type MAE exceeds allowed maximum: {invalid_per_egg_type_mae}")
        print(json.dumps(metrics, ensure_ascii=False, indent=2))
        try:
            mae = float(metrics.get("mae"))
        except (AttributeError, TypeError, ValueError):
            raise SystemExit("Artifact metrics do not contain a valid MAE.") from None
        if not math.isfinite(mae) or mae > settings.prebuilt_max_mae:
            raise SystemExit(
                f"Artifact MAE {mae:.4f} exceeds allowed maximum {settings.prebuilt_max_mae:.4f}."
            )
        if settings.prebuilt_require_baseline_improvement:
            try:
                baseline_mae = float(metrics.get("baseline_mae"))
            except (AttributeError, TypeError, ValueError):
                raise SystemExit("Artifact metrics do not contain a valid baseline MAE.") from None
            if not math.isfinite(baseline_mae) or mae >= baseline_mae:
                raise SystemExit(
                    f"Artifact MAE {mae:.4f} does not improve baseline MAE {baseline_mae:.4f}."
                )
        trained_sklearn = metrics.get("scikit_learn_version") if isinstance(metrics, dict) else None
        if trained_sklearn and trained_sklearn != sklearn.__version__:
            print(
                "WARNING: scikit-learn version mismatch: "
                f"artifact={trained_sklearn}, runtime={sklearn.__version__}"
            )


if __name__ == "__main__":
    main()
