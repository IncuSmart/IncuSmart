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
    answer_language: str = "vi"
    api_host: str = "0.0.0.0"
    api_port: int = 8001
    streamlit_port: int = 8501
    cors_origins: str = "*"

    postgres_dsn: str = Field(
        default="postgresql+psycopg://postgres:postgres@localhost:5432/incu_smart_test"
    )
    chroma_dir: Path = Path("./storage/chroma")
    model_dir: Path = Path("./storage/models")
    docs_dir: Path = Path("./docs")
    synthetic_data_path: Path = Path("./storage/synthetic/gemini_synthetic_dataset.json")
    ml_export_path: Path = Path("./storage/ml-export/recommendation_training_rows.jsonl")

    default_egg_type: str = "chicken"
    default_template_name: str = "default"
    knowledge_enabled: bool = True
    max_rag_chunks: int = 4
    rag_provider: str = "chroma"

    llm_provider: str = "none"
    llm_api_key: str | None = None
    llm_model: str | None = None
    llm_embedding_model: str = "gemini-embedding-001"
    llm_base_url: str | None = None
    llm_timeout_seconds: int = 60
    llm_max_retries: int = 3
    llm_retry_backoff_seconds: float = 1.0
    use_prebuilt_model: bool = False
    prebuilt_require_metrics: bool = True
    prebuilt_max_mae: float = Field(default=0.20, gt=0, le=1)
    prebuilt_require_baseline_improvement: bool = True
    enable_debug_endpoints: bool = False
    knn_neighbors: int = Field(default=5, ge=1, le=50)
    knn_min_samples: int = Field(default=3, ge=1)
    knn_heuristic_weight: float = Field(default=0.05, ge=0, le=1)
    knn_db_cache_seconds: int = Field(default=300, ge=0)
    knn_db_reference_weight: float = Field(default=1.0, gt=0)
    knn_synthetic_reference_weight: float = Field(default=0.6, gt=0)
    knn_max_synthetic_references: int = Field(default=200, ge=1)
    synthetic_label_gemini_weight: float = Field(default=0.7, ge=0, le=1)

    bigquery_ml_enabled: bool = False
    bigquery_project_id: str | None = None
    bigquery_credentials_path: Path | None = None
    bigquery_dataset: str = "incusmart_ml"
    bigquery_location: str = "asia-southeast1"
    bigquery_training_table: str = "hatching_success_training"
    bigquery_model: str = "hatching_success_model"
    bigquery_prediction_maximum_bytes_billed: int = Field(default=20_971_520, ge=0)
    bigquery_query_timeout_seconds: int = Field(default=30, ge=1)
    bigquery_rag_embeddings_table: str = "rag_embeddings"
    bigquery_rag_maximum_bytes_billed: int = Field(default=50_000_000, ge=0)


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    settings = Settings()
    settings.chroma_dir.mkdir(parents=True, exist_ok=True)
    settings.model_dir.mkdir(parents=True, exist_ok=True)
    settings.docs_dir.mkdir(parents=True, exist_ok=True)
    return settings
