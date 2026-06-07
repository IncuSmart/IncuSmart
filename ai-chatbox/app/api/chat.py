from fastapi import APIRouter, Depends

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
from app.services.chat_orchestrator import ChatOrchestrator, get_chat_orchestrator

router = APIRouter()
debug_router = APIRouter(prefix="/debug")


@router.post("/chat", response_model=ChatResponse)
def chat(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> ChatResponse:
    return orchestrator.handle_chat(payload)


@router.post("/predict-success", response_model=PredictSuccessResponse)
def predict_success(
    payload: PredictSuccessRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> PredictSuccessResponse:
    return orchestrator.predict_success(payload)


@debug_router.post("/recommend", response_model=RecommendDebugResponse)
def debug_recommend(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> RecommendDebugResponse:
    return orchestrator.debug_recommend(payload)


@debug_router.post("/knowledge", response_model=KnowledgeDebugResponse)
def debug_knowledge(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> KnowledgeDebugResponse:
    return orchestrator.debug_knowledge(payload)


@debug_router.get("/ml-status", response_model=MlStatusResponse)
def ml_status(
    egg_type: str | None = None,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> MlStatusResponse:
    return orchestrator.ml_status(egg_type)


@debug_router.get("/ml-evaluation", response_model=MlEvaluationResponse)
def ml_evaluation(
    egg_type: str | None = None,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> MlEvaluationResponse:
    return orchestrator.ml_evaluation(egg_type)


@debug_router.post("/ml-cache/clear", response_model=MlCacheResponse)
def clear_ml_cache(
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> MlCacheResponse:
    return orchestrator.clear_ml_cache()


@debug_router.get("/model-artifact-status", response_model=ModelArtifactStatusResponse)
def model_artifact_status(
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> ModelArtifactStatusResponse:
    return orchestrator.model_artifact_status()


@debug_router.get("/bigquery-status", response_model=BigQueryStatusResponse)
def bigquery_status(
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> BigQueryStatusResponse:
    return orchestrator.bigquery_status()


@debug_router.post("/ml-benchmark", response_model=MlBenchmarkResponse)
def ml_benchmark(
    payload: ChatRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> MlBenchmarkResponse:
    return orchestrator.ml_benchmark(payload)
