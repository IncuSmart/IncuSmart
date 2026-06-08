from __future__ import annotations

from pathlib import Path

from fastapi import APIRouter, Depends, File, HTTPException, UploadFile

from app.schemas import AdminRagStatusResponse, AdminRagUploadResponse, AdminTrainingAddRequest, AdminTrainingAddResponse
from app.services.chat_orchestrator import ChatOrchestrator, get_chat_orchestrator

admin_router = APIRouter(prefix="/admin")

_ALLOWED_SUFFIXES = {".txt", ".md", ".pdf"}


@admin_router.post("/training/data", response_model=AdminTrainingAddResponse)
def add_training_data(
    payload: AdminTrainingAddRequest,
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> AdminTrainingAddResponse:
    return orchestrator.add_training_record(payload.model_dump())


@admin_router.post("/rag/upload", response_model=AdminRagUploadResponse)
async def upload_rag_document(
    file: UploadFile = File(...),
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> AdminRagUploadResponse:
    suffix = Path(file.filename or "").suffix.lower()
    if suffix not in _ALLOWED_SUFFIXES:
        raise HTTPException(
            status_code=400,
            detail=f"Loại file không hỗ trợ: '{suffix}'. Chỉ nhận .txt, .md, .pdf",
        )

    raw = await file.read()

    if suffix == ".pdf":
        try:
            import io
            import pypdf

            reader = pypdf.PdfReader(io.BytesIO(raw))
            text = "\n".join(page.extract_text() or "" for page in reader.pages)
        except ImportError as exc:
            raise HTTPException(status_code=500, detail="pypdf chưa được cài đặt trên server") from exc
    else:
        text = raw.decode("utf-8", errors="ignore")

    filename = file.filename or "unknown"
    topic = Path(filename).stem

    return orchestrator.upload_rag_document(text, filename, topic)


@admin_router.get("/rag/status", response_model=AdminRagStatusResponse)
def rag_status(
    orchestrator: ChatOrchestrator = Depends(get_chat_orchestrator),
) -> AdminRagStatusResponse:
    return orchestrator.get_rag_status()
