from __future__ import annotations

from threading import Lock

from app.config import Settings
from app.config import get_settings
from app.repositories.postgres_repository import PostgresRepository
from app.schemas import (
    ChatRequest,
    ChatResponse,
    KnowledgeDebugResponse,
    MlCacheResponse,
    MlBenchmarkResponse,
    MlEvaluationResponse,
    MlStatusResponse,
    ModelArtifactStatusResponse,
    BigQueryStatusResponse,
    PredictSuccessRequest,
    PredictSuccessResponse,
    RecommendDebugResponse,
)
from app.services.intent_router import IntentRouter
from app.services.llm_service import LlmService
from app.services.rag_service import RagService
from app.services.recommend_service import RecommendService
from app.services.bigquery_ml_service import BigQueryMlService
from app.services.bigquery_rag_service import BigQueryRagService
from app.services.bigquery_status_service import BigQueryStatusService


class ChatOrchestrator:
    def __init__(
        self,
        settings: Settings,
        router: IntentRouter,
        rag_service: RagService,
        recommend_service: RecommendService,
    ):
        self._settings = settings
        self._router = router
        self._rag_service = rag_service
        self._recommend_service = recommend_service

    def handle_chat(self, payload: ChatRequest) -> ChatResponse:
        intent = self._router.detect(payload.message)
        if intent == "knowledge":
            if not self._settings.knowledge_enabled:
                return ChatResponse(
                    intent="knowledge",
                    answer="Tính năng hỏi đáp tài liệu đang tạm tắt để tránh gọi Gemini/RAG. Hiện hệ thống chỉ xử lý recommend và dự đoán BigQuery ML.",
                    sources=[],
                )
            result = self._rag_service.answer(payload.message)
            return ChatResponse(intent="knowledge", answer=result.answer, sources=result.sources)

        recommendation = self._recommend_service.recommend(payload.message, payload.user_context)
        return ChatResponse(
            intent="recommend",
            answer=recommendation.answer,
            sources=[],
            recommended_config=recommendation.phases,
            scoring_mode=recommendation.scoring_mode,
            estimated_success_rate=recommendation.estimated_success_rate,
            estimated_success_confidence=recommendation.estimated_success_confidence,
            model_artifact_version=recommendation.model_artifact_version,
            model_training_dataset_sha256=recommendation.model_training_dataset_sha256,
            validation_warnings=recommendation.validation_warnings,
        )

    def debug_recommend(self, payload: ChatRequest) -> RecommendDebugResponse:
        return self._recommend_service.debug_recommend(payload.message, payload.user_context)

    def predict_success(self, payload: PredictSuccessRequest) -> PredictSuccessResponse:
        return self._recommend_service.predict_success(payload)

    def debug_knowledge(self, payload: ChatRequest) -> KnowledgeDebugResponse:
        if not self._settings.knowledge_enabled:
            answer = "Tính năng hỏi đáp tài liệu đang tạm tắt để tránh gọi Gemini/RAG."
            return KnowledgeDebugResponse(
                message=payload.message,
                answer_preview=answer,
                source_count=0,
                sources=[],
            )
        result = self._rag_service.answer(payload.message)
        return KnowledgeDebugResponse(
            message=payload.message,
            answer_preview=result.answer,
            source_count=len(result.sources),
            sources=result.sources,
        )

    def ml_status(self, egg_type: str | None = None) -> MlStatusResponse:
        return self._recommend_service.ml_status(egg_type)

    def ml_evaluation(self, egg_type: str | None = None) -> MlEvaluationResponse:
        return self._recommend_service.evaluate_knn(egg_type)

    def clear_ml_cache(self) -> MlCacheResponse:
        return self._recommend_service.clear_ml_cache()

    def model_artifact_status(self) -> ModelArtifactStatusResponse:
        return self._recommend_service.model_artifact_status()

    def bigquery_status(self) -> BigQueryStatusResponse:
        return BigQueryStatusService(get_settings()).status()

    def ml_benchmark(self, payload: ChatRequest) -> MlBenchmarkResponse:
        return self._recommend_service.benchmark_recommend(payload.message, payload.user_context)


_orchestrator: ChatOrchestrator | None = None
_orchestrator_lock = Lock()


def get_chat_orchestrator() -> ChatOrchestrator:
    global _orchestrator
    if _orchestrator is not None:
        return _orchestrator
    with _orchestrator_lock:
        if _orchestrator is None:
            settings = get_settings()
            repository = PostgresRepository(settings)
            llm_service = LlmService(settings)
            bigquery_ml_service = BigQueryMlService(settings)
            bigquery_rag_service = BigQueryRagService(settings, llm_service)
            _orchestrator = ChatOrchestrator(
                settings=settings,
                router=IntentRouter(),
                rag_service=RagService(settings, llm_service, bigquery_rag_service),
                recommend_service=RecommendService(settings, repository, bigquery_ml_service),
            )
    return _orchestrator
