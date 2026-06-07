from __future__ import annotations

import argparse
from datetime import datetime
import json
from pathlib import Path

import requests

from _http_common import resolve_base_url


ROOT = Path(__file__).resolve().parents[1]
EGG_TYPES = ("chicken", "duck", "quail", "goose")


def _get(base_url: str, path: str, **params) -> dict:
    response = requests.get(base_url + path, params=params, timeout=120)
    response.raise_for_status()
    return response.json()


def _post(base_url: str, path: str, payload: dict) -> dict:
    response = requests.post(base_url + path, json=payload, timeout=120)
    response.raise_for_status()
    return response.json()


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a post-activation acceptance report for the prebuilt ML model.")
    parser.add_argument("--base-url", help="Override base URL.")
    parser.add_argument("--strict", action="store_true", help="Exit non-zero if acceptance checks fail.")
    args = parser.parse_args()
    base_url = resolve_base_url(args.base_url)

    artifact_status = _get(base_url, "/debug/model-artifact-status")
    checks = {
        "artifact_enabled": artifact_status.get("enabled") is True,
        "artifact_loadable": artifact_status.get("loadable") is True,
        "quality_gate_passed": artifact_status.get("quality_gate_passed") is True,
        "validation_coverage_matches": artifact_status.get("validation_coverage_matches") is True,
        "artifact_matches_current_export": artifact_status.get("matches_current_export") is not False,
    }
    egg_reports = {}
    for egg_type in EGG_TYPES:
        benchmark = _post(
            base_url,
            "/debug/ml-benchmark",
            {
                "message": f"recommend config for 300 {egg_type} eggs",
                "session_id": f"acceptance-{egg_type}",
                "user_context": {"ambient_temperature": 31, "ambient_humidity": 67},
            },
        )
        evaluation = _get(base_url, "/debug/ml-evaluation", egg_type=egg_type)
        prebuilt = next(
            (result for result in benchmark.get("results", []) if result.get("scoring_mode") == "prebuilt_model"),
            {},
        )
        config = prebuilt.get("recommended_config") or []
        egg_checks = {
            "prebuilt_available": prebuilt.get("available") is True,
            "estimated_success_available": prebuilt.get("estimated_success_rate") is not None,
            "recommended_config_has_three_phases": len(config) == 3,
            "recommended_config_passes_output_validation": prebuilt.get("passed_output_validation") is True,
        }
        checks[f"{egg_type}_accepted"] = all(egg_checks.values())
        egg_reports[egg_type] = {
            "checks": egg_checks,
            "benchmark": benchmark,
            "knn_evaluation": evaluation,
        }

    passed = all(checks.values())
    report = {
        "generated_at": datetime.now().isoformat(),
        "base_url": base_url,
        "passed": passed,
        "checks": checks,
        "artifact_status": artifact_status,
        "egg_types": egg_reports,
    }
    reports_dir = ROOT / "storage" / "reports"
    reports_dir.mkdir(parents=True, exist_ok=True)
    output_path = reports_dir / f"model-acceptance-{datetime.now().strftime('%Y%m%d-%H%M%S')}.json"
    output_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote model acceptance report to {output_path}")
    print(json.dumps({"passed": passed, "checks": checks}, ensure_ascii=False, indent=2))

    if args.strict and not passed:
        raise SystemExit("Model acceptance checks failed.")


if __name__ == "__main__":
    main()
