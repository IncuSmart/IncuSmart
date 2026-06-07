from app.services.intent_router import IntentRouter


def test_detect_recommend() -> None:
    router = IntentRouter()
    assert router.detect("Đề xuất thông số cho 300 trứng gà") == "recommend"


def test_detect_recommend_without_vietnamese_accents() -> None:
    router = IntentRouter()
    assert router.detect("de xuat cau hinh ap trung ga") == "recommend"


def test_detect_knowledge() -> None:
    router = IntentRouter()
    assert router.detect("Khi nào nên ngừng đảo trứng?") == "knowledge"
