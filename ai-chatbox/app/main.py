from fastapi import FastAPI

from app.api.chat import debug_router, router as chat_router
from app.config import Settings, get_settings


def create_app(settings: Settings | None = None) -> FastAPI:
    resolved_settings = settings or get_settings()
    application = FastAPI(title=resolved_settings.app_name)
    application.include_router(chat_router)
    if resolved_settings.enable_debug_endpoints:
        application.include_router(debug_router)
    return application


app = create_app()


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}
