from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import uuid

from app.config import get_settings
from app.services.llm_service import LlmService, LlmServiceError
from app.services.synthetic_validation import validate_synthetic_record


PROMPT_TEMPLATE = """
Bạn đang tạo dữ liệu synthetic cho hệ thống máy ấp trứng.
Hãy sinh đúng JSON array gồm __RECORD_COUNT__ records.
__EGG_TYPE_REQUIREMENT__
Mỗi record có cấu trúc:
{
  "egg_type": "chicken|duck|quail|goose",
  "total_eggs": 100,
  "ambient_temperature": 30.0,
  "ambient_humidity": 65.0,
  "phases": [
    {
      "phase_index": 1,
      "day_start": 1,
      "day_end": 7,
      "parameters": [
        {"config_code": "TEMP", "target_value": 37.6, "min_value": 37.5, "max_value": 37.8, "unit": "C"},
        {"config_code": "HUMID", "target_value": 60, "min_value": 55, "max_value": 65, "unit": "%"},
        {"config_code": "TURN", "target_value": 6, "min_value": 4, "max_value": 8, "unit": "times/day"},
        {"config_code": "FAN", "target_value": 2, "min_value": 1, "max_value": 3, "unit": "level"}
      ]
    },
    {
      "phase_index": 2,
      "day_start": 8,
      "day_end": 18,
      "parameters": [
        {"config_code": "TEMP", "target_value": 37.5, "min_value": 37.3, "max_value": 37.7, "unit": "C"},
        {"config_code": "HUMID", "target_value": 60, "min_value": 55, "max_value": 65, "unit": "%"},
        {"config_code": "TURN", "target_value": 6, "min_value": 4, "max_value": 8, "unit": "times/day"},
        {"config_code": "FAN", "target_value": 2, "min_value": 1, "max_value": 3, "unit": "level"}
      ]
    },
    {
      "phase_index": 3,
      "day_start": 19,
      "day_end": 21,
      "parameters": [
        {"config_code": "TEMP", "target_value": 37.2, "min_value": 37.0, "max_value": 37.4, "unit": "C"},
        {"config_code": "HUMID", "target_value": 70, "min_value": 65, "max_value": 75, "unit": "%"},
        {"config_code": "TURN", "target_value": 0, "min_value": 0, "max_value": 1, "unit": "times/day"},
        {"config_code": "FAN", "target_value": 2, "min_value": 1, "max_value": 3, "unit": "level"}
      ]
    }
  ],
  "expected_success_rate": 0.84,
  "notes": "short note"
}

Yêu cầu:
- giá trị hợp lý cho kỹ thuật ấp
- mỗi record phải có đúng 3 phase liên tục, đủ TEMP và HUMID trong từng phase
- ngày cuối theo loại trứng: chicken=21, duck=28, quail=18, goose=30
- phase cuối tăng ẩm và giảm đảo trứng
- tạo hỗn hợp config tốt và config chưa tối ưu nhưng vẫn nằm trong vùng an toàn
- expected_success_rate phải tương quan với chất lượng config và trải rộng khoảng 0.55 đến 0.95
- không tạo giá trị nguy hiểm chỉ để làm giảm expected_success_rate
- không thêm markdown
- không thêm giải thích
""".strip()


def _extract_json_block(text: str) -> str:
    text = text.strip()
    if text.startswith("```"):
        lines = text.splitlines()
        if lines and lines[0].startswith("```"):
            lines = lines[1:]
        if lines and lines[-1].startswith("```"):
            lines = lines[:-1]
        text = "\n".join(lines).strip()
        if text.lower().startswith("json"):
            text = text[4:].strip()
    return text


def _load_existing(path: Path) -> list[dict]:
    if not path.exists():
        return []
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return []
    return data if isinstance(data, list) else []


def _save_dataset(path: Path, data: list[dict]) -> None:
    temporary_path = path.with_suffix(path.suffix + ".tmp")
    temporary_path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    temporary_path.replace(path)


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate synthetic incubation records with Gemini.")
    parser.add_argument("--records", type=int, default=5, help="Requested records per Gemini call.")
    parser.add_argument("--batches", type=int, default=1, help="Number of Gemini calls.")
    parser.add_argument("--append", action="store_true", help="Append to the existing dataset instead of replacing it.")
    parser.add_argument(
        "--egg-type",
        choices=["chicken", "duck", "quail", "goose"],
        help="Generate records only for one egg type.",
    )
    args = parser.parse_args()
    if args.records < 1 or args.batches < 1:
        raise SystemExit("--records and --batches must be positive integers.")

    settings = get_settings()
    service = LlmService(settings)
    if not service.is_enabled():
        raise SystemExit("Gemini is not configured. Check AI_CHATBOX_LLM_* in .env")

    output_path = Path(settings.synthetic_data_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    data: list[dict] = _load_existing(output_path) if args.append else []

    for batch_index in range(1, args.batches + 1):
        egg_type_requirement = (
            f"Tất cả records phải có egg_type='{args.egg_type}'."
            if args.egg_type
            else "Phân bố records tương đối đều giữa chicken, duck, quail và goose."
        )
        prompt = (
            PROMPT_TEMPLATE.replace("__RECORD_COUNT__", str(args.records))
            .replace("__EGG_TYPE_REQUIREMENT__", egg_type_requirement)
        )
        try:
            result = service.complete(prompt, temperature=0.7)
            generated = json.loads(_extract_json_block(result.text))
        except (LlmServiceError, json.JSONDecodeError) as exc:
            raise SystemExit(f"Gemini synthetic generation failed at batch {batch_index}: {exc}") from exc
        if not isinstance(generated, list):
            raise SystemExit(f"Gemini batch {batch_index} did not return a JSON array.")
        valid_records = [
            record
            for record in generated
            if not validate_synthetic_record(record)
            and (not args.egg_type or record.get("egg_type") == args.egg_type)
        ]
        batch_id = str(uuid.uuid4())
        generated_at = datetime.now(timezone.utc).isoformat()
        for record in valid_records:
            record["_synthetic_batch_id"] = batch_id
            record["_generated_at_utc"] = generated_at
        data.extend(valid_records)
        _save_dataset(output_path, data)
        rejected = len(generated) - len(valid_records)
        print(
            f"Generated batch {batch_index}/{args.batches}: "
            f"{len(valid_records)} valid records, {rejected} rejected records"
        )

    print(f"Saved {len(data)} synthetic records to {output_path}")


if __name__ == "__main__":
    main()
