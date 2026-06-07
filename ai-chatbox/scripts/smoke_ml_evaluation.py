from __future__ import annotations

import argparse
import json

import requests

from _http_common import resolve_base_url


def main() -> None:
    parser = argparse.ArgumentParser(description="Run leave-one-out evaluation for KNN inference.")
    parser.add_argument("--base-url", help="Override base URL. Defaults to AI_CHATBOX_BASE_URL or http://127.0.0.1:8001")
    parser.add_argument("--egg-type", default="chicken", help="Egg type to evaluate.")
    args = parser.parse_args()

    base_url = resolve_base_url(args.base_url)
    response = requests.get(
        f"{base_url}/debug/ml-evaluation",
        params={"egg_type": args.egg_type},
        timeout=120,
    )
    response.raise_for_status()
    print(json.dumps(response.json(), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
