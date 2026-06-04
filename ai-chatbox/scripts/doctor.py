from __future__ import annotations

import json
import os
from pathlib import Path
from urllib.error import URLError
from urllib.request import urlopen

from _masked_config import load_masked_config

ROOT = Path(__file__).resolve().parents[1]


def yes_no(value: bool) -> str:
    return "yes" if value else "no"


def main() -> None:
    env_path = ROOT / ".env"
    venv_python = ROOT / ".venv" / "Scripts" / "python.exe"
    docs_dir = ROOT / "docs"
    log_file = ROOT / "storage" / "logs" / "uvicorn.log"
    pid_file = ROOT / "storage" / "run" / "api.pid"
    base_url = (os.getenv("AI_CHATBOX_BASE_URL") or "http://127.0.0.1:8001").rstrip("/")

    report: dict[str, object] = {
        "project_root": str(ROOT),
        "env_file_exists": env_path.exists(),
        "venv_python_exists": venv_python.exists(),
        "docs_dir_exists": docs_dir.exists(),
        "docs_file_count": len(list(docs_dir.glob("*"))) if docs_dir.exists() else 0,
        "log_file_exists": log_file.exists(),
        "pid_file_exists": pid_file.exists(),
        "base_url": base_url,
        "masked_config": load_masked_config(env_path),
    }

    if pid_file.exists():
        try:
            report["pid_value"] = pid_file.read_text(encoding="utf-8").strip()
        except OSError:
            report["pid_value"] = None

    try:
        with urlopen(f"{base_url}/health", timeout=5) as response:
            body = response.read().decode("utf-8", errors="ignore")
            report["health_reachable"] = True
            report["health_status_code"] = response.status
            report["health_body"] = body
    except (URLError, TimeoutError, OSError) as exc:
        report["health_reachable"] = False
        report["health_error"] = str(exc)

    print("== ai-chatbox doctor ==")
    print(f"project_root       : {report['project_root']}")
    print(f"env_file_exists    : {yes_no(bool(report['env_file_exists']))}")
    print(f"venv_python_exists : {yes_no(bool(report['venv_python_exists']))}")
    print(f"docs_dir_exists    : {yes_no(bool(report['docs_dir_exists']))}")
    print(f"docs_file_count    : {report['docs_file_count']}")
    print(f"log_file_exists    : {yes_no(bool(report['log_file_exists']))}")
    print(f"pid_file_exists    : {yes_no(bool(report['pid_file_exists']))}")
    if "pid_value" in report:
        print(f"pid_value          : {report['pid_value']}")
    print(f"base_url           : {report['base_url']}")
    print(f"llm_provider       : {report['masked_config'].get('llm_provider')}")
    print(f"llm_model          : {report['masked_config'].get('llm_model')}")
    print(f"docs_dir_config    : {report['masked_config'].get('docs_dir')}")
    print(f"postgres_dsn_masked: {report['masked_config'].get('postgres_dsn_masked')}")
    print(f"llm_key_masked     : {report['masked_config'].get('llm_api_key_masked')}")
    print(f"health_reachable   : {yes_no(bool(report['health_reachable']))}")
    if report["health_reachable"]:
        print(f"health_status_code : {report['health_status_code']}")
        print(f"health_body        : {report['health_body']}")
    else:
        print(f"health_error       : {report.get('health_error', '')}")

    print("\nJSON:")
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
