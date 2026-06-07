from __future__ import annotations

from pathlib import Path

from app.config import get_settings


def main() -> None:
    settings = get_settings()
    issues: list[str] = []

    if settings.llm_provider.lower() == "gemini" and not settings.llm_api_key:
        issues.append("Missing AI_CHATBOX_LLM_API_KEY for Gemini.")

    if not settings.docs_dir.exists():
        issues.append(f"Docs dir does not exist: {settings.docs_dir}")

    env_path = Path(".env")
    if not env_path.exists():
        issues.append("Missing .env file.")

    if issues:
        print("Local setup validation failed:")
        for issue in issues:
            print(f"- {issue}")
        raise SystemExit(1)

    print("Local setup looks valid.")


if __name__ == "__main__":
    main()
