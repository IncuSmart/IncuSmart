from __future__ import annotations

import json
from pathlib import Path

from app.config import get_settings
from app.services.llm_service import LlmService, LlmServiceError


PROMPT = """
Bạn đang tạo dữ liệu synthetic cho hệ thống máy ấp trứng.
Hãy sinh đúng JSON array gồm 5 records.
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
    }
  ],
  "expected_success_rate": 0.84,
  "notes": "short note"
}

Yêu cầu:
- giá trị hợp lý cho kỹ thuật ấp
- phase cuối tăng ẩm và giảm đảo trứng
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


if __name__ == "__main__":
    settings = get_settings()
    service = LlmService(settings)
    if not service.is_enabled():
        raise SystemExit("Gemini is not configured. Check AI_CHATBOX_LLM_* in .env")

    output_dir = Path("storage") / "synthetic"
    output_dir.mkdir(parents=True, exist_ok=True)
    output_path = output_dir / "gemini_synthetic_dataset.json"

    try:
        result = service.complete(PROMPT, temperature=0.7)
    except LlmServiceError as exc:
        raise SystemExit(f"Gemini synthetic generation failed: {exc}") from exc
    text = _extract_json_block(result.text)
    data = json.loads(text)
    output_path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Saved synthetic dataset to {output_path}")
