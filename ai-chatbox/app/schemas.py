from typing import Any, Literal

from pydantic import BaseModel, Field


class ChatRequest(BaseModel):
    message: str = Field(min_length=1)
    session_id: str = Field(min_length=1)
    user_context: dict[str, Any] | None = None


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


class RecommendDebugResponse(BaseModel):
    message: str
    egg_type: str
    total_eggs: int
    template_source: str
    scoring_mode: Literal["heuristic", "prebuilt_model"]
    answer_preview: str
    top_candidates: list[RecommendDebugCandidate]


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
