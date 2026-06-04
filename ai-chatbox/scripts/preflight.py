from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def run_step(label: str, command: list[str]) -> None:
    print(f"\n== {label} ==")
    print("Command:", " ".join(command))
    completed = subprocess.run(command, cwd=ROOT, check=False)
    if completed.returncode != 0:
        raise SystemExit(completed.returncode)


def main() -> None:
    parser = argparse.ArgumentParser(description="Run local AI chatbox preflight checks.")
    parser.add_argument("--skip-ingest", action="store_true", help="Skip seeding docs and RAG ingest.")
    parser.add_argument("--skip-gemini", action="store_true", help="Skip Gemini connectivity test.")
    args = parser.parse_args()

    python_exe = sys.executable

    run_step("Validate local setup", [python_exe, "scripts/validate_local_setup.py"])

    if not args.skip_gemini:
        run_step("Test Gemini connectivity", [python_exe, "scripts/test_gemini.py"])

    run_step("Seed local docs", [python_exe, "scripts/seed_docs.py"])

    if not args.skip_ingest:
        run_step("Ingest RAG documents", [python_exe, "scripts/ingest_rag.py"])

    print("\nPreflight completed.")
    print("Next steps:")
    print("1. Run .\\scripts\\run_api.ps1")
    print("2. Run .\\scripts\\test_health.ps1")
    print("3. Run smoke tests as needed:")
    print("   - .\\scripts\\smoke_chat.ps1")
    print("   - .\\scripts\\smoke_debug_recommend.ps1")
    print("   - .\\scripts\\smoke_knowledge.ps1")
    print("   - .\\scripts\\smoke_debug_knowledge.ps1")


if __name__ == "__main__":
    main()
