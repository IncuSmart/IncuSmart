from typing import Any, Literal

from pydantic import BaseModel, Field


class ChatRequest(BaseModel):
    message: str = Field(min_length=1)
    session_id: str = Field(min_length=1)
    user_context: dict[str, Any] | None = None


class PredictSuccessRequest(BaseModel):
    egg_type: str = Field(min_length=1)
    total_eggs: int = Field(ge=1, le=100000)
    incubator_id: str | None = None
    ambient_temperature: float | None = None
    ambient_humidity: float | None = None


class PredictSuccessResponse(BaseModel):
    egg_type: str
    total_eggs: int
    predicted_success_rate: float | None = None
    predicted_success_percent: float | None = None
    confidence: float | None = None
    prediction_mode: Literal["bigquery_ml", "db_knn", "blended_knn", "synthetic_knn", "insufficient_data"]
    db_completed_seasons: int
    synthetic_references: int
    message: str


class SourceItem(BaseModel):
    source: str
    section: str | None = None
    topic: str | None = None
    excerpt: str | None = None


class RecommendedParameter(BaseModel):
    config_code: str
    config_name: str
    target_value: float | None = None
    min_value: float | None = None
    max_value: float | None = None
    unit: str | None = None


class RecommendedPhase(BaseModel):
    phase_index: int
    phase_name: str
    day_start: int
    day_end: int
    parameters: list[RecommendedParameter]


class RecommendDebugCandidate(BaseModel):
    rank: int
    score: float
    phases: list[RecommendedPhase]
    passed_output_validation: bool = True
    validation_warnings: list[str] = []


class RecommendDebugResponse(BaseModel):
    message: str
    egg_type: str
    total_eggs: int
    template_source: str
    scoring_mode: Literal["heuristic", "knn_inference", "prebuilt_model"]
    estimated_success_rate: float | None = None
    estimated_success_confidence: float | None = None
    model_artifact_version: int | None = None
    model_training_dataset_sha256: str | None = None
    db_labeled_references: int = 0
    synthetic_labeled_references: int = 0
    effective_unique_references: int = 0
    answer_preview: str
    top_candidates: list[RecommendDebugCandidate]


class MlStatusResponse(BaseModel):
    egg_type: str
    db_labeled_references: int
    synthetic_labeled_references: int
    total_labeled_references: int
    effective_unique_references: int
    minimum_references: int
    ready_for_knn: bool
    db_reference_weight: float
    synthetic_reference_weight: float
    synthetic_label_gemini_weight: float
    max_synthetic_references: int
    scoring_precedence: list[str]


class MlEvaluationResponse(BaseModel):
    egg_type: str
    sample_count: int
    mae: float | None
    baseline_mae: float | None
    mean_confidence: float | None
    evaluation_mode: Literal["leave_one_out"]


class MlCacheResponse(BaseModel):
    cleared_db_reference_groups: int
    cleared_synthetic_cache: bool
    cleared_prebuilt_model_cache: bool


class ModelArtifactStatusResponse(BaseModel):
    enabled: bool
    model_exists: bool
    features_exists: bool
    metrics_exists: bool
    loadable: bool
    feature_count: int
    metrics: dict[str, Any] | None = None
    manifest: dict[str, Any] | None = None
    schema_hash_matches: bool | None = None
    validation_coverage_matches: bool = False
    artifact_mae: float | None = None
    baseline_mae: float | None = None
    max_allowed_mae: float
    quality_gate_passed: bool
    rejection_reasons: list[str]
    training_dataset_sha256: str | None = None
    current_export_sha256: str | None = None
    matches_current_export: bool | None = None


class BigQueryStatusResponse(BaseModel):
    enabled: bool
    project_id_configured: bool
    dataset: str
    location: str
    training_table: str
    model: str
    rag_provider: str
    rag_embeddings_table: str
    rag_embedding_model: str
    credentials_available: bool
    dataset_exists: bool | None = None
    training_table_exists: bool | None = None
    model_exists: bool | None = None
    rag_embeddings_table_exists: bool | None = None
    errors: list[str] = []


class MlBenchmarkModeResult(BaseModel):
    scoring_mode: Literal["heuristic", "knn_inference", "prebuilt_model"]
    available: bool
    ranking_score: float | None = None
    estimated_success_rate: float | None = None
    estimated_success_confidence: float | None = None
    recommended_config: list[RecommendedPhase] | None = None
    passed_output_validation: bool | None = None
    validation_warnings: list[str] = []


class MlBenchmarkResponse(BaseModel):
    egg_type: str
    total_eggs: int
    candidate_count: int
    results: list[MlBenchmarkModeResult]


class KnowledgeDebugResponse(BaseModel):
    message: str
    answer_preview: str
    source_count: int
    sources: list[SourceItem]


class ChatResponse(BaseModel):
    intent: Literal["knowledge", "recommend"]
    answer: str
    sources: list[SourceItem] = []
    recommended_config: list[RecommendedPhase] | None = None
    scoring_mode: Literal["heuristic", "knn_inference", "prebuilt_model"] | None = None
    estimated_success_rate: float | None = None
    estimated_success_confidence: float | None = None
    model_artifact_version: int | None = None
    model_training_dataset_sha256: str | None = None
    validation_warnings: list[str] = []


class AdminTrainingSyncResponse(BaseModel):
    cleared_cache_groups: int
    cleared_synthetic_cache: bool
    cleared_prebuilt_model_cache: bool
    message: str


class AdminTrainingAddRequest(BaseModel):
    egg_type: str
    total_eggs: int
    expected_success_rate: float
    ambient_temperature: float | None = None
    ambient_humidity: float | None = None
    phases: list[dict[str, Any]]


class AdminTrainingAddResponse(BaseModel):
    records_added: int
    total_records: int
    validation_issues: list[str]
    message: str


class AdminRagUploadResponse(BaseModel):
    filename: str
    chunks_added: int
    total_chunks_in_collection: int
    message: str


class AdminRagStatusResponse(BaseModel):
    collection_name: str
    total_chunks: int
    provider: str
