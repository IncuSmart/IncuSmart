from __future__ import annotations

from pathlib import Path


def mask_secret(value: str | None, keep_start: int = 4, keep_end: int = 3) -> str | None:
    if not value:
        return value
    if len(value) <= keep_start + keep_end:
        return "*" * len(value)
    return value[:keep_start] + "*" * (len(value) - keep_start - keep_end) + value[-keep_end:]


def mask_dsn(value: str | None) -> str | None:
    if not value:
        return value
    if "://" not in value:
        return mask_secret(value)
    scheme, rest = value.split("://", 1)
    return f"{scheme}://{mask_secret(rest, keep_start=2, keep_end=8)}"


def load_masked_config(env_path: Path) -> dict[str, object]:
    if not env_path.exists():
        return {"env_file_exists": False}

    raw: dict[str, str] = {}
    for line in env_path.read_text(encoding="utf-8", errors="ignore").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        raw[key.strip()] = value.strip()

    return {
        "env_file_exists": True,
        "app_name": raw.get("AI_CHATBOX_APP_NAME"),
        "env": raw.get("AI_CHATBOX_ENV"),
        "api_host": raw.get("AI_CHATBOX_API_HOST"),
        "api_port": raw.get("AI_CHATBOX_API_PORT"),
        "docs_dir": raw.get("AI_CHATBOX_DOCS_DIR"),
        "synthetic_data_path": raw.get("AI_CHATBOX_SYNTHETIC_DATA_PATH"),
        "ml_export_path": raw.get("AI_CHATBOX_ML_EXPORT_PATH"),
        "llm_provider": raw.get("AI_CHATBOX_LLM_PROVIDER"),
        "llm_model": raw.get("AI_CHATBOX_LLM_MODEL"),
        "llm_base_url": raw.get("AI_CHATBOX_LLM_BASE_URL"),
        "llm_timeout_seconds": raw.get("AI_CHATBOX_LLM_TIMEOUT_SECONDS"),
        "llm_max_retries": raw.get("AI_CHATBOX_LLM_MAX_RETRIES"),
        "llm_retry_backoff_seconds": raw.get("AI_CHATBOX_LLM_RETRY_BACKOFF_SECONDS"),
        "use_prebuilt_model": raw.get("AI_CHATBOX_USE_PREBUILT_MODEL"),
        "prebuilt_require_metrics": raw.get("AI_CHATBOX_PREBUILT_REQUIRE_METRICS"),
        "prebuilt_max_mae": raw.get("AI_CHATBOX_PREBUILT_MAX_MAE"),
        "prebuilt_require_baseline_improvement": raw.get("AI_CHATBOX_PREBUILT_REQUIRE_BASELINE_IMPROVEMENT"),
        "enable_debug_endpoints": raw.get("AI_CHATBOX_ENABLE_DEBUG_ENDPOINTS"),
        "knn_neighbors": raw.get("AI_CHATBOX_KNN_NEIGHBORS"),
        "knn_min_samples": raw.get("AI_CHATBOX_KNN_MIN_SAMPLES"),
        "knn_heuristic_weight": raw.get("AI_CHATBOX_KNN_HEURISTIC_WEIGHT"),
        "knn_db_cache_seconds": raw.get("AI_CHATBOX_KNN_DB_CACHE_SECONDS"),
        "knn_db_reference_weight": raw.get("AI_CHATBOX_KNN_DB_REFERENCE_WEIGHT"),
        "knn_synthetic_reference_weight": raw.get("AI_CHATBOX_KNN_SYNTHETIC_REFERENCE_WEIGHT"),
        "knn_max_synthetic_references": raw.get("AI_CHATBOX_KNN_MAX_SYNTHETIC_REFERENCES"),
        "synthetic_label_gemini_weight": raw.get("AI_CHATBOX_SYNTHETIC_LABEL_GEMINI_WEIGHT"),
        "postgres_dsn_masked": mask_dsn(raw.get("AI_CHATBOX_POSTGRES_DSN")),
        "llm_api_key_masked": mask_secret(raw.get("AI_CHATBOX_LLM_API_KEY")),
    }
