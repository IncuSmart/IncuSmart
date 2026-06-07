import re
import unicodedata
from typing import Literal


Intent = Literal["knowledge", "recommend"]

RECOMMEND_KEYWORDS = [
    "goi y",
    "de xuat",
    "recommend",
    "thong so",
    "cau hinh",
    "set",
    "setting",
]


class IntentRouter:
    def detect(self, message: str) -> Intent:
        normalized = normalize_text(message)
        if any(keyword in normalized for keyword in RECOMMEND_KEYWORDS):
            return "recommend"
        return "knowledge"


def normalize_text(value: str) -> str:
    decomposed = unicodedata.normalize("NFD", value.lower().strip())
    without_marks = "".join(character for character in decomposed if unicodedata.category(character) != "Mn")
    return re.sub(r"\s+", " ", without_marks.replace("đ", "d"))
