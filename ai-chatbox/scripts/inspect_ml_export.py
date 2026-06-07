from __future__ import annotations

import argparse
from collections import Counter
import json
from pathlib import Path

from app.config import get_settings


def main() -> None:
    parser = argparse.ArgumentParser(description="Inspect exported labeled feature rows before Colab training.")
    parser.add_argument("--strict", action="store_true", help="Exit non-zero unless RandomForest training minimums pass.")
    parser.add_argument("--minimum-rows", type=int, default=10, help="Minimum unique feature rows.")
    parser.add_argument(
        "--require-all-egg-types",
        action="store_true",
        help="Require chicken, duck, quail, and goose rows.",
    )
    parser.add_argument(
        "--minimum-groups-per-egg-type",
        type=int,
        default=2,
        help="Minimum independent training groups per egg type when all egg types are required.",
    )
    args = parser.parse_args()

    path = Path(get_settings().ml_export_path)
    if not path.exists():
        raise SystemExit(f"ML export does not exist: {path}")

    raw_rows = [
        json.loads(line)
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip()
    ]
    valid_rows = []
    fingerprints = set()
    for row in raw_rows:
        if not isinstance(row, dict):
            continue
        try:
            success_rate = float(row.get("success_rate"))
        except (TypeError, ValueError):
            continue
        if not 0 <= success_rate <= 1 or not row.get("egg_type"):
            continue
        valid_rows.append(row)
        features = {
            key: value
            for key, value in row.items()
            if key not in {"success_rate", "data_source", "training_group"}
        }
        fingerprints.add(json.dumps(features, ensure_ascii=False, sort_keys=True))

    labels = [float(row["success_rate"]) for row in valid_rows]
    sources = Counter(str(row.get("data_source") or "unknown") for row in valid_rows)
    egg_types = Counter(str(row.get("egg_type") or "unknown") for row in valid_rows)
    groups_by_egg_type = {
        egg_type: len(
            {
                str(row.get("training_group"))
                for row in valid_rows
                if str(row.get("egg_type") or "unknown") == egg_type
            }
        )
        for egg_type in sorted(egg_types)
    }
    report = {
        "path": str(path),
        "raw_rows": len(raw_rows),
        "valid_rows": len(valid_rows),
        "unique_feature_rows": len(fingerprints),
        "training_groups": len({str(row.get("training_group")) for row in valid_rows}),
        "source_counts": dict(sorted(sources.items())),
        "egg_type_counts": dict(sorted(egg_types.items())),
        "training_groups_by_egg_type": groups_by_egg_type,
        "label_min": min(labels) if labels else None,
        "label_max": max(labels) if labels else None,
        "label_range": max(labels) - min(labels) if labels else None,
        "unique_labels": len({round(label, 4) for label in labels}),
    }
    print(json.dumps(report, ensure_ascii=False, indent=2))

    issues = []
    if len(fingerprints) < args.minimum_rows:
        issues.append(f"need at least {args.minimum_rows} unique feature rows")
    if len({round(label, 4) for label in labels}) < 3:
        issues.append("need at least 3 unique labels")
    if labels and max(labels) - min(labels) < 0.1:
        issues.append("label range must be at least 0.1")
    if args.require_all_egg_types:
        missing_egg_types = {"chicken", "duck", "quail", "goose"} - set(egg_types)
        if missing_egg_types:
            issues.append(f"missing egg types: {sorted(missing_egg_types)}")
        under_grouped = {
            egg_type: groups_by_egg_type.get(egg_type, 0)
            for egg_type in {"chicken", "duck", "quail", "goose"}
            if groups_by_egg_type.get(egg_type, 0) < args.minimum_groups_per_egg_type
        }
        if under_grouped:
            issues.append(
                f"need at least {args.minimum_groups_per_egg_type} training groups per egg type: "
                f"{dict(sorted(under_grouped.items()))}"
            )
    if args.strict and issues:
        raise SystemExit("ML export is not ready: " + "; ".join(issues))


if __name__ == "__main__":
    main()
