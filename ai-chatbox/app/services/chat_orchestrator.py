from __future__ import annotations

from functools import lru_cache

from app.config import get_settings
from app.repositories.postgres_repository import PostgresRepository
from app.schemas import ChatRequest, ChatResponse, KnowledgeDebugResponse, RecommendDebugResponse
from app.services.intent_router import IntentRouter
from app.services.llm_service import LlmService
from app.services.rag_service import RagService
from app.services.recommend_service import RecommendService


class ChatOrchestrator:
    def __init__(
        self,
        router: IntentRouter,
        rag_service: RagService,
        recommend_service: RecommendService,
    ):
        self._router = router
        self._rag_service = rag_service
        self._recommend_service = recommend_service

    def handle_chat(self, payload: ChatRequest) -> ChatResponse:
        intent = self._router.detect(payload.message)
        if intent == "knowledge":
            result = self._rag_service.answer(payload.message)
            return ChatResponse(intent="knowledge", answer=result.answer, sources=result.sources)

        recommendation = self._recommend_service.recommend(payload.message, payload.user_context)
        return ChatResponse(
            intent="recommend",
            answer=recommendation.answer,
            sources=[],
            recommended_config=recommendation.phases,
        )

    def debug_recommend(self, payload: ChatRequest) -> RecommendDebugResponse:
        return self._recommend_service.debug_recommend(payload.message, payload.user_context)

    def debug_knowledge(self, payload: ChatRequest) -> KnowledgeDebugResponse:
        result = self._rag_service.answer(payload.message)
        return KnowledgeDebugResponse(
            message=payload.message,
            answer_preview=result.answer,
            source_count=len(result.sources),
            sources=result.sources,
        )


@lru_cache(maxsize=1)
def get_chat_orchestrator() -> ChatOrchestrator:
    settings = get_settings()
    repository = PostgresRepository(settings)
    llm_service = LlmService(settings)
    return ChatOrchestrator(
        router=IntentRouter(),
        rag_service=RagService(settings, llm_service),
        recommend_service=RecommendService(settings, repository),
    )
