from fastapi import APIRouter, Depends

from app.schemas import ChatRequest, ChatResponse, KnowledgeDebugResponse, RecommendDebugResponse
from app.services.chat_orchestrator import ChatOrchestrator, get_chat_orchestrator

router = APIRouter()


@router.post("/chat", response_model=ChatResponse)
def chat(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> ChatResponse:
    return orchestrator.handle_chat(payload)


@router.post("/debug/recommend", response_model=RecommendDebugResponse)
def debug_recommend(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> RecommendDebugResponse:
    return orchestrator.debug_recommend(payload)


@router.post("/debug/knowledge", response_model=KnowledgeDebugResponse)
def debug_knowledge(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> KnowledgeDebugResponse:
    return orchestrator.debug_knowledge(payload)
