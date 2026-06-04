from __future__ import annotations

import os


DEFAULT_BASE_URL = "http://127.0.0.1:8001"


def resolve_base_url(base_url: str | None = None) -> str:
    value = base_url or os.getenv("AI_CHATBOX_BASE_URL") or DEFAULT_BASE_URL
    return value.rstrip("/")
