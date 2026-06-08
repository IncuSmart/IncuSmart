from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api.admin import admin_router
from app.api.chat import debug_router, router as chat_router
from app.config import Settings, get_settings


def _parse_cors_origins(raw_origins: str) -> list[str]:
    origins = [origin.strip() for origin in raw_origins.split(",") if origin.strip()]
    return origins or ["*"]


def create_app(settings: Settings | None = None) -> FastAPI:
    resolved_settings = settings or get_settings()
    application = FastAPI(title=resolved_settings.app_name)
    cors_origins = _parse_cors_origins(resolved_settings.cors_origins)
    application.add_middleware(
        CORSMiddleware,
        allow_origins=cors_origins,
        allow_credentials=False if "*" in cors_origins else True,
        allow_methods=["*"],
        allow_headers=["*"],
    )
    application.include_router(chat_router)
    application.include_router(admin_router)
    if resolved_settings.enable_debug_endpoints:
        application.include_router(debug_router)
    return application


app = create_app()


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}
