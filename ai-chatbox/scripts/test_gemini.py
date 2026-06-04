from app.config import get_settings
from app.services.llm_service import LlmService, LlmServiceError


if __name__ == "__main__":
    settings = get_settings()
    service = LlmService(settings)
    if not service.is_enabled():
        raise SystemExit("Gemini is not configured. Check AI_CHATBOX_LLM_* in .env")

    print(f"Provider: {settings.llm_provider}")
    print(f"Model: {settings.llm_model}")
    print("Testing Gemini connectivity...")

    try:
        result = service.complete(
            "Tra loi dung mot dong: ket noi gemini thanh cong.",
            temperature=0.0,
        )
    except LlmServiceError as exc:
        raise SystemExit(f"Gemini test failed: {exc}") from exc

    print("Response:")
    print(result.text)
