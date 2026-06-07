from __future__ import annotations

import re
import shutil
from pathlib import Path

from app.config import get_settings


DEFAULT_CONTEXT_MD = Path(r"C:\Users\GIGA\Downloads\egg_incubator_AI_context.md")
DEFAULT_SCHEMA_SQL = Path(r"C:\Users\GIGA\OneDrive\Documents\egg_incubator.sql")


def copy_if_exists(source: Path, destination: Path) -> bool:
    if not source.exists():
        return False
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    return True


def build_schema_summary(sql_path: Path, output_path: Path) -> bool:
    if not sql_path.exists():
        return False

    text = sql_path.read_text(encoding="utf-8", errors="ignore")
    table_names = re.findall(r"CREATE TABLE public\.([A-Za-z0-9_\"]+)\s*\(", text)

    important_tables = [
        "hatching_seasons",
        "hatching_batches",
        "hatching_batch_configs",
        "hatching_season_templates",
        "hatching_season_template_batches",
        "hatching_season_template_batch_configs",
        "sensor_readings",
        "sensors",
        "configs",
        "incubators",
        "incubator_config_instances",
        "alerts",
    ]

    summary_lines = [
        "# IncuSmart Internal Schema Summary",
        "",
        "This file is generated from the PostgreSQL dump for internal AI context.",
        "",
        "## Important tables for AI chatbox",
        "",
    ]

    found = set(name.strip('"') for name in table_names)
    for table_name in important_tables:
        status = "present" if table_name in found else "missing"
        summary_lines.append(f"- `{table_name}`: {status}")

    summary_lines.extend(
        [
            "",
            "## All discovered tables",
            "",
        ]
    )
    for table_name in sorted(found):
        summary_lines.append(f"- `{table_name}`")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(summary_lines) + "\n", encoding="utf-8")
    return True


def main() -> None:
    settings = get_settings()
    copied: list[str] = []
    generated: list[str] = []

    context_target = settings.docs_dir / "egg_incubator_AI_context.md"
    if copy_if_exists(DEFAULT_CONTEXT_MD, context_target):
        copied.append(str(context_target))

    schema_target = settings.docs_dir / "internal_schema_summary.md"
    if build_schema_summary(DEFAULT_SCHEMA_SQL, schema_target):
        generated.append(str(schema_target))

    if not copied and not generated:
        raise SystemExit("No source documents found to seed.")

    if copied:
        print("Copied:")
        for path in copied:
            print(f"- {path}")

    if generated:
        print("Generated:")
        for path in generated:
            print(f"- {path}")


if __name__ == "__main__":
    main()
