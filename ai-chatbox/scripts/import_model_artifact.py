from __future__ import annotations

import argparse
from datetime import datetime
import hashlib
import json
import math
from pathlib import Path
import shutil
import tempfile
import zipfile

import joblib
import pandas as pd

from app.config import get_settings


REQUIRED_FILES = {
    "recommendation_model.joblib",
    "recommendation_features.json",
    "recommendation_manifest.json",
    "recommendation_metrics.json",
}
OPTIONAL_FILES: set[str] = set()


def _find_artifact_dir(root: Path) -> Path:
    candidates = {
        path.parent
        for path in root.rglob("recommendation_model.joblib")
        if path.is_file()
    }
    for candidate in sorted(candidates):
        if all((candidate / filename).is_file() for filename in REQUIRED_FILES):
            return candidate
    raise SystemExit(f"Could not find all required artifact files under {root}")


def _safe_extract(archive_path: Path, destination: Path) -> None:
    destination_resolved = destination.resolve()
    with zipfile.ZipFile(archive_path) as archive:
        for member in archive.infolist():
            target = (destination / member.filename).resolve()
            if destination_resolved not in target.parents and target != destination_resolved:
                raise SystemExit(f"Unsafe ZIP path rejected: {member.filename}")
        archive.extractall(destination)


def _validate_artifact(
    artifact_dir: Path,
    max_mae: float = 0.20,
    require_baseline_improvement: bool = True,
) -> tuple[dict, list[str], float]:
    features_path = artifact_dir / "recommendation_features.json"
    manifest_path = artifact_dir / "recommendation_manifest.json"
    model_path = artifact_dir / "recommendation_model.joblib"

    feature_columns = json.loads(features_path.read_text(encoding="utf-8"))
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if not isinstance(feature_columns, list) or not feature_columns:
        raise SystemExit("Feature file must contain a non-empty JSON array.")
    if not isinstance(manifest, dict) or manifest.get("artifact_version") != 1:
        raise SystemExit("Unsupported or invalid recommendation_manifest.json.")

    schema_json = json.dumps(feature_columns, ensure_ascii=False, separators=(",", ":"))
    schema_hash = hashlib.sha256(schema_json.encode("utf-8")).hexdigest()
    if schema_hash != manifest.get("feature_schema_sha256"):
        raise SystemExit("Feature schema hash does not match artifact manifest.")
    if manifest.get("feature_count") != len(feature_columns):
        raise SystemExit("Feature count does not match artifact manifest.")
    metrics = json.loads((artifact_dir / "recommendation_metrics.json").read_text(encoding="utf-8"))
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
        if not isinstance(value, (int, float)) or not math.isfinite(float(value)) or float(value) > max_mae
    }
    if invalid_per_egg_type_mae:
        raise SystemExit(f"Artifact per-egg-type MAE exceeds allowed maximum: {invalid_per_egg_type_mae}")
    try:
        mae = float(metrics.get("mae"))
    except (TypeError, ValueError):
        raise SystemExit("Artifact metrics do not contain a valid MAE.") from None
    if not math.isfinite(mae) or mae > max_mae:
        raise SystemExit(f"Artifact MAE {mae:.4f} exceeds allowed maximum {max_mae:.4f}.")
    if require_baseline_improvement:
        try:
            baseline_mae = float(metrics.get("baseline_mae"))
        except (AttributeError, TypeError, ValueError):
            raise SystemExit("Artifact metrics do not contain a valid baseline MAE.") from None
        if not math.isfinite(baseline_mae) or mae >= baseline_mae:
            raise SystemExit(f"Artifact MAE {mae:.4f} does not improve baseline MAE {baseline_mae:.4f}.")

    sample = {column: "chicken" if column == "egg_type" else 0.0 for column in feature_columns}
    model = joblib.load(model_path)
    prediction = float(model.predict(pd.DataFrame([sample]))[0])
    if not math.isfinite(prediction) or not 0 <= prediction <= 1:
        raise SystemExit("Artifact returned a sample prediction outside the valid success-rate range [0, 1].")
    return manifest, feature_columns, prediction


def _enable_prebuilt_model(env_path: Path) -> None:
    line = "AI_CHATBOX_USE_PREBUILT_MODEL=true"
    if not env_path.exists():
        raise SystemExit(f"Cannot enable prebuilt model because .env does not exist: {env_path}")
    lines = env_path.read_text(encoding="utf-8").splitlines()
    replaced = False
    for index, current in enumerate(lines):
        if current.startswith("AI_CHATBOX_USE_PREBUILT_MODEL="):
            lines[index] = line
            replaced = True
            break
    if not replaced:
        lines.append(line)
    env_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def _install_artifact(artifact_dir: Path, model_dir: Path) -> Path | None:
    model_dir.mkdir(parents=True, exist_ok=True)
    existing = [path for path in model_dir.iterdir() if path.name in REQUIRED_FILES | OPTIONAL_FILES]
    backup_dir = None
    if existing:
        backup_dir = model_dir / "backups" / datetime.now().strftime("%Y%m%d-%H%M%S-%f")
        backup_dir.mkdir(parents=True, exist_ok=True)
        for path in existing:
            shutil.copy2(path, backup_dir / path.name)

    try:
        for filename in REQUIRED_FILES | OPTIONAL_FILES:
            source = artifact_dir / filename
            destination = model_dir / filename
            if source.exists():
                shutil.copy2(source, destination)
            elif filename in OPTIONAL_FILES and destination.exists():
                destination.unlink()
    except OSError:
        if backup_dir:
            for backup in backup_dir.iterdir():
                shutil.copy2(backup, model_dir / backup.name)
        raise
    return backup_dir


def main() -> None:
    parser = argparse.ArgumentParser(description="Validate and import a RandomForest artifact downloaded from Colab.")
    parser.add_argument("artifact", help="Path to a Colab artifact ZIP or extracted directory.")
    parser.add_argument("--enable", action="store_true", help="Set AI_CHATBOX_USE_PREBUILT_MODEL=true in .env after import.")
    args = parser.parse_args()

    source = Path(args.artifact).resolve()
    if not source.exists():
        raise SystemExit(f"Artifact path does not exist: {source}")
    settings = get_settings()

    with tempfile.TemporaryDirectory(prefix="incusmart-model-import-") as temp_dir:
        root = Path(temp_dir)
        if source.is_file():
            if not zipfile.is_zipfile(source):
                raise SystemExit("Artifact file must be a ZIP archive.")
            _safe_extract(source, root)
            artifact_dir = _find_artifact_dir(root)
        else:
            artifact_dir = _find_artifact_dir(source)

        manifest, feature_columns, prediction = _validate_artifact(
            artifact_dir,
            settings.prebuilt_max_mae,
            settings.prebuilt_require_baseline_improvement,
        )
        backup_dir = _install_artifact(artifact_dir, Path(settings.model_dir))

    if args.enable:
        _enable_prebuilt_model(Path(".env"))

    print(f"Imported artifact version {manifest['artifact_version']} into {settings.model_dir}")
    print(f"Feature columns: {len(feature_columns)}")
    print(f"Sample prediction: {prediction:.4f}")
    if backup_dir:
        print(f"Previous artifact backup: {backup_dir}")
    if args.enable:
        print("Enabled AI_CHATBOX_USE_PREBUILT_MODEL=true in .env")
        print("Restart the API so the updated .env setting is loaded.")


if __name__ == "__main__":
    main()
