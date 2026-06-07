from __future__ import annotations

from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path

from app.config import get_settings
from app.repositories.postgres_repository import PostgresRepository
from app.services.recommend_service import RecommendService


def main() -> None:
    settings = get_settings()
    service = RecommendService(settings, PostgresRepository(settings))
    rows = service.export_training_rows()
    if not rows:
        raise SystemExit("No valid DB or synthetic labeled rows are available for export.")

    output_path = Path(settings.ml_export_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("w", encoding="utf-8") as handle:
        for row in rows:
            handle.write(json.dumps(row, ensure_ascii=False, allow_nan=False) + "\n")

    sources = Counter(str(row["data_source"]) for row in rows)
    egg_types = Counter(str(row["egg_type"]) for row in rows)
    dataset_hash = hashlib.sha256(output_path.read_bytes()).hexdigest()
    manifest_path = output_path.with_suffix(".manifest.json")
    manifest_path.write_text(
        json.dumps(
            {
                "dataset_version": 1,
                "generated_at_utc": datetime.now(timezone.utc).isoformat(),
                "dataset_file": output_path.name,
                "dataset_sha256": dataset_hash,
                "row_count": len(rows),
                "synthetic_label_gemini_weight": settings.synthetic_label_gemini_weight,
                "source_weights": {
                    "db": settings.knn_db_reference_weight,
                    "synthetic": settings.knn_synthetic_reference_weight,
                },
                "source_counts": dict(sorted(sources.items())),
                "egg_type_counts": dict(sorted(egg_types.items())),
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    print(f"Exported {len(rows)} labeled feature rows to {output_path}")
    print(f"Dataset manifest: {manifest_path}")
    print(f"Sources: {dict(sorted(sources.items()))}")
    print(f"Egg types: {dict(sorted(egg_types.items()))}")


if __name__ == "__main__":
    main()
