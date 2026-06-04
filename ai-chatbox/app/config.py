from functools import lru_cache
from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_prefix="AI_CHATBOX_",
        extra="ignore",
    )

    app_name: str = "IncuSmart AI Chatbox"
    env: str = "development"
    api_host: str = "0.0.0.0"
    api_port: int = 8001
    streamlit_port: int = 8501

    postgres_dsn: str = Field(
        default="postgresql+psycopg://postgres:postgres@localhost:5432/incu_smart_test"
    )
    chroma_dir: Path = Path("./storage/chroma")
    model_dir: Path = Path("./storage/models")
    docs_dir: Path = Path("./docs")

    embedding_model: str = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
    default_egg_type: str = "chicken"
    default_template_name: str = "default"
    max_rag_chunks: int = 4

    llm_provider: str = "none"
    llm_api_key: str | None = None
    llm_model: str | None = None
    llm_base_url: str | None = None
    llm_timeout_seconds: int = 60
    llm_max_retries: int = 3
    llm_retry_backoff_seconds: float = 1.0
    use_prebuilt_model: bool = False


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    settings = Settings()
    settings.chroma_dir.mkdir(parents=True, exist_ok=True)
    settings.model_dir.mkdir(parents=True, exist_ok=True)
    settings.docs_dir.mkdir(parents=True, exist_ok=True)
    return settings
