from __future__ import annotations

import argparse
import json

import requests

from _http_common import resolve_base_url

def call(base_url: str, label: str, method: str, path: str, payload: dict | None = None) -> None:
    print(f"\n== {label} ==")
    url = base_url + path
    if method == "GET":
        response = requests.get(url, timeout=120)
    else:
        response = requests.post(url, json=payload, timeout=120)
    response.raise_for_status()
    print(json.dumps(response.json(), ensure_ascii=False, indent=2))


def main() -> None:
    parser = argparse.ArgumentParser(description="Run local API smoke suite against a running ai-chatbox server.")
    parser.add_argument("--base-url", help="Override base URL. Defaults to AI_CHATBOX_BASE_URL or http://127.0.0.1:8001")
    parser.add_argument("--skip-recommend", action="store_true", help="Skip recommend endpoint checks.")
    parser.add_argument("--skip-knowledge", action="store_true", help="Skip knowledge endpoint checks.")
    args = parser.parse_args()
    base_url = resolve_base_url(args.base_url)

    call(base_url, "health", "GET", "/health")

    if not args.skip_recommend:
        recommend_payload = {
            "message": "de xuat thong so ap trung ga cho 300 eggs",
            "session_id": "suite-recommend",
            "user_context": {
                "ambient_temperature": 31,
                "ambient_humidity": 67,
                "notes": "smoke suite recommend",
            },
        }
        call(base_url, "chat recommend", "POST", "/chat", recommend_payload)

        debug_recommend_payload = {
            "message": "de xuat cau hinh ap trung ga cho 300 eggs",
            "session_id": "suite-debug-recommend",
            "user_context": {
                "ambient_temperature": 31,
                "ambient_humidity": 67,
                "notes": "smoke suite debug recommend",
            },
        }
        call(base_url, "debug recommend", "POST", "/debug/recommend", debug_recommend_payload)

    if not args.skip_knowledge:
        knowledge_payload = {
            "message": "nhiet do va do am cho cac giai doan ap trung ga la gi",
            "session_id": "suite-knowledge",
            "user_context": {},
        }
        call(base_url, "chat knowledge", "POST", "/chat", knowledge_payload)

        debug_knowledge_payload = {
            "message": "nhiet do va do am cho cac giai doan ap trung ga la gi",
            "session_id": "suite-debug-knowledge",
            "user_context": {},
        }
        call(base_url, "debug knowledge", "POST", "/debug/knowledge", debug_knowledge_payload)


if __name__ == "__main__":
    main()
