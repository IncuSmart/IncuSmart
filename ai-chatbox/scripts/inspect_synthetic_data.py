from __future__ import annotations

import argparse
from collections import Counter
import json
from pathlib import Path

from app.config import get_settings
from app.services.synthetic_validation import validate_synthetic_record


def _as_rate(value: object) -> float | None:
    try:
        rate = float(value)
    except (TypeError, ValueError):
        return None
    return rate if 0 <= rate <= 1 else None


def main() -> None:
    parser = argparse.ArgumentParser(description="Inspect Gemini synthetic records used by KNN inference.")
    parser.add_argument("--strict", action="store_true", help="Exit non-zero if no usable labeled records exist.")
    parser.add_argument(
        "--minimum-per-egg-type",
        type=int,
        default=0,
        help="Exit non-zero unless each supported egg type has at least this many usable records.",
    )
    args = parser.parse_args()

    path = Path(get_settings().synthetic_data_path)
    if not path.exists():
        raise SystemExit(f"Synthetic dataset does not exist: {path}")

    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, list):
        raise SystemExit("Synthetic dataset root must be a JSON array.")

    egg_types: Counter[str] = Counter()
    usable_by_egg_type: Counter[str] = Counter()
    rates: list[float] = []
    invalid_records = 0
    issue_counts: Counter[str] = Counter()

    for record in data:
        if isinstance(record, dict):
            egg_types[str(record.get("egg_type") or "unknown").lower()] += 1
        issues = validate_synthetic_record(record)
        if issues:
            invalid_records += 1
            issue_counts.update(issues)
            continue
        assert isinstance(record, dict)
        egg_type = str(record.get("egg_type") or "unknown").lower()
        rate = _as_rate(record.get("expected_success_rate"))
        assert rate is not None
        rates.append(rate)
        usable_by_egg_type[egg_type] += 1

    report = {
        "path": str(path),
        "total_records": len(data),
        "usable_labeled_records": len(rates),
        "invalid_or_unlabeled_records": invalid_records,
        "validation_issue_counts": dict(sorted(issue_counts.items())),
        "records_by_egg_type": dict(sorted(egg_types.items())),
        "usable_by_egg_type": dict(sorted(usable_by_egg_type.items())),
        "success_rate_min": min(rates) if rates else None,
        "success_rate_average": sum(rates) / len(rates) if rates else None,
        "success_rate_max": max(rates) if rates else None,
        "success_rate_range": max(rates) - min(rates) if rates else None,
        "unique_success_rates": len({round(rate, 4) for rate in rates}),
    }
    print(json.dumps(report, ensure_ascii=False, indent=2))

    if args.strict and not rates:
        raise SystemExit("No usable labeled synthetic records were found.")
    if args.minimum_per_egg_type > 0:
        missing = {
            egg_type: args.minimum_per_egg_type - usable_by_egg_type.get(egg_type, 0)
            for egg_type in ("chicken", "duck", "quail", "goose")
            if usable_by_egg_type.get(egg_type, 0) < args.minimum_per_egg_type
        }
        if missing:
            raise SystemExit(f"Insufficient usable records by egg type: {missing}")


if __name__ == "__main__":
    main()
