from __future__ import annotations

import argparse
from pathlib import Path

from app.config import get_settings
from import_model_artifact import _find_artifact_dir, _install_artifact, _validate_artifact


def main() -> None:
    parser = argparse.ArgumentParser(description="Restore a previously backed-up recommendation model artifact.")
    parser.add_argument("--backup", help="Backup directory. Defaults to the latest storage/models/backups directory.")
    args = parser.parse_args()

    settings = get_settings()
    model_dir = Path(settings.model_dir)
    if args.backup:
        backup_dir = Path(args.backup).resolve()
    else:
        backups_root = model_dir / "backups"
        backups = sorted((path for path in backups_root.iterdir() if path.is_dir()), reverse=True) if backups_root.exists() else []
        if not backups:
            raise SystemExit(f"No model backups found under {backups_root}")
        backup_dir = backups[0]

    artifact_dir = _find_artifact_dir(backup_dir)
    manifest, feature_columns, prediction = _validate_artifact(
        artifact_dir,
        settings.prebuilt_max_mae,
        settings.prebuilt_require_baseline_improvement,
    )
    current_backup = _install_artifact(artifact_dir, model_dir)
    print(f"Restored artifact version {manifest['artifact_version']} from {artifact_dir}")
    print(f"Feature columns: {len(feature_columns)}")
    print(f"Sample prediction: {prediction:.4f}")
    if current_backup:
        print(f"Replaced artifact backup: {current_backup}")
    print("Restart the API or clear the model cache before checking the restored artifact.")


if __name__ == "__main__":
    main()
