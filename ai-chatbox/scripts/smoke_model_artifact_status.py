from __future__ import annotations

import json

import requests

from _http_common import resolve_base_url


if __name__ == "__main__":
    response = requests.get(f"{resolve_base_url()}/debug/model-artifact-status", timeout=120)
    response.raise_for_status()
    print(json.dumps(response.json(), ensure_ascii=False, indent=2))
