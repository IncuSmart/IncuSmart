from __future__ import annotations

import json
import os
from datetime import datetime
from pathlib import Path
from urllib.error import URLError
from urllib.request import urlopen

from _masked_config import load_masked_config

ROOT = Path(__file__).resolve().parents[1]


def read_tail(path: Path, max_lines: int = 80) -> list[str]:
    if not path.exists():
        return []
    lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    return lines[-max_lines:]


def build_report() -> dict[str, object]:
    env_path = ROOT / ".env"
    venv_python = ROOT / ".venv" / "Scripts" / "python.exe"
    docs_dir = ROOT / "docs"
    log_file = ROOT / "storage" / "logs" / "uvicorn.log"
    pid_file = ROOT / "storage" / "run" / "api.pid"
    reports_dir = ROOT / "storage" / "reports"
    base_url = (os.getenv("AI_CHATBOX_BASE_URL") or "http://127.0.0.1:8001").rstrip("/")

    report: dict[str, object] = {
        "generated_at": datetime.now().isoformat(),
        "project_root": str(ROOT),
        "env_file_exists": env_path.exists(),
        "venv_python_exists": venv_python.exists(),
        "docs_dir_exists": docs_dir.exists(),
        "docs_file_count": len(list(docs_dir.glob("*"))) if docs_dir.exists() else 0,
        "log_file_exists": log_file.exists(),
        "pid_file_exists": pid_file.exists(),
        "base_url": base_url,
        "masked_config": load_masked_config(env_path),
        "log_tail": read_tail(log_file),
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

    reports_dir.mkdir(parents=True, exist_ok=True)
    return report


def main() -> None:
    report = build_report()
    reports_dir = ROOT / "storage" / "reports"
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output_path = reports_dir / f"session-report-{stamp}.json"
    output_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote report to {output_path}")


if __name__ == "__main__":
    main()
