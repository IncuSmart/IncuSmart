from __future__ import annotations

import json
import os
from datetime import datetime
from pathlib import Path

import requests


ROOT = Path(__file__).resolve().parents[1]
BASE_URL = (os.getenv("AI_CHATBOX_BASE_URL") or "http://127.0.0.1:8001").rstrip("/")


def call(name: str, method: str, path: str, payload: dict | None = None) -> dict[str, object]:
    url = BASE_URL + path
    try:
        if method == "GET":
            response = requests.get(url, timeout=120)
        else:
            response = requests.post(url, json=payload, timeout=120)

        body: object
        try:
            body = response.json()
        except ValueError:
            body = response.text

        return {
            "name": name,
            "method": method,
            "path": path,
            "status_code": response.status_code,
            "ok": response.ok,
            "response": body,
        }
    except Exception as exc:  # pragma: no cover - defensive runtime capture
        return {
            "name": name,
            "method": method,
            "path": path,
            "ok": False,
            "error": str(exc),
        }


def build_report() -> dict[str, object]:
    recommend_payload = {
        "message": "de xuat thong so ap trung ga cho 300 eggs",
        "session_id": "report-recommend",
        "user_context": {
            "ambient_temperature": 31,
            "ambient_humidity": 67,
            "notes": "report recommend",
        },
    }
    debug_recommend_payload = {
        "message": "de xuat cau hinh ap trung ga cho 300 eggs",
        "session_id": "report-debug-recommend",
        "user_context": {
            "ambient_temperature": 31,
            "ambient_humidity": 67,
            "notes": "report debug recommend",
        },
    }
    knowledge_payload = {
        "message": "nhiet do va do am cho cac giai doan ap trung ga la gi",
        "session_id": "report-knowledge",
        "user_context": {},
    }
    debug_knowledge_payload = {
        "message": "nhiet do va do am cho cac giai doan ap trung ga la gi",
        "session_id": "report-debug-knowledge",
        "user_context": {},
    }

    checks = [
        call("health", "GET", "/health"),
        call("chat_recommend", "POST", "/chat", recommend_payload),
        call("debug_recommend", "POST", "/debug/recommend", debug_recommend_payload),
        call("chat_knowledge", "POST", "/chat", knowledge_payload),
        call("debug_knowledge", "POST", "/debug/knowledge", debug_knowledge_payload),
    ]

    passed = sum(1 for item in checks if item.get("ok"))
    failed = len(checks) - passed

    return {
        "generated_at": datetime.now().isoformat(),
        "base_url": BASE_URL,
        "summary": {
            "total": len(checks),
            "passed": passed,
            "failed": failed,
        },
        "checks": checks,
    }


def main() -> None:
    reports_dir = ROOT / "storage" / "reports"
    reports_dir.mkdir(parents=True, exist_ok=True)
    report = build_report()
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output_path = reports_dir / f"smoke-suite-report-{stamp}.json"
    output_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote report to {output_path}")
    print(json.dumps(report["summary"], ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
