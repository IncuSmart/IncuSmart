from __future__ import annotations

import argparse
import json

import requests

from _http_common import resolve_base_url


def main() -> None:
    parser = argparse.ArgumentParser(description="Smoke test the /chat recommend path.")
    parser.add_argument("--base-url", help="Override base URL. Defaults to AI_CHATBOX_BASE_URL or http://127.0.0.1:8001")
    args = parser.parse_args()

    payload = {
        "message": "de xuat thong so ap trung ga cho 300 eggs",
        "session_id": "local-smoke",
        "user_context": {
            "ambient_temperature": 31,
            "ambient_humidity": 67,
            "notes": "local smoke test",
        },
    }

    base_url = resolve_base_url(args.base_url)
    response = requests.post(f"{base_url}/chat", json=payload, timeout=120)
    response.raise_for_status()
    print(json.dumps(response.json(), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
