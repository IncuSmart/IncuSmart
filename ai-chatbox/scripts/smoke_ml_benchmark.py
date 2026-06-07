from __future__ import annotations

import argparse
from datetime import datetime
import json
from pathlib import Path

import requests

from _http_common import resolve_base_url


ROOT = Path(__file__).resolve().parents[1]


def main() -> None:
    parser = argparse.ArgumentParser(description="Compare heuristic, KNN, and prebuilt-model recommend scorers.")
    parser.add_argument("--base-url", help="Override base URL.")
    parser.add_argument("--egg-type", default="chicken", choices=["chicken", "duck", "quail", "goose"])
    parser.add_argument("--total-eggs", type=int, default=300)
    parser.add_argument("--ambient-temperature", type=float, default=31)
    parser.add_argument("--ambient-humidity", type=float, default=67)
    parser.add_argument("--save-report", action="store_true")
    args = parser.parse_args()

    payload = {
        "message": f"recommend config for {args.total_eggs} {args.egg_type} eggs",
        "session_id": f"benchmark-{args.egg_type}",
        "user_context": {
            "ambient_temperature": args.ambient_temperature,
            "ambient_humidity": args.ambient_humidity,
        },
    }
    response = requests.post(
        f"{resolve_base_url(args.base_url)}/debug/ml-benchmark",
        json=payload,
        timeout=120,
    )
    response.raise_for_status()
    body = response.json()
    print(json.dumps(body, ensure_ascii=False, indent=2))

    if args.save_report:
        reports_dir = ROOT / "storage" / "reports"
        reports_dir.mkdir(parents=True, exist_ok=True)
        stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
        output_path = reports_dir / f"ml-benchmark-{args.egg_type}-{stamp}.json"
        output_path.write_text(json.dumps(body, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"Saved benchmark report to {output_path}")


if __name__ == "__main__":
    main()
