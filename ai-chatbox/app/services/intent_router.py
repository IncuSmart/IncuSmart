from typing import Literal


Intent = Literal["knowledge", "recommend"]

RECOMMEND_KEYWORDS = [
    "gợi ý",
    "đề xuất",
    "recommend",
    "thông số",
    "cấu hình",
    "set",
    "setting",
]


class IntentRouter:
    def detect(self, message: str) -> Intent:
        normalized = message.lower().strip()
        if any(keyword in normalized for keyword in RECOMMEND_KEYWORDS):
            return "recommend"
        return "knowledge"
