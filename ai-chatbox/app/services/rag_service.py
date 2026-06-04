from __future__ import annotations

from dataclasses import dataclass

import chromadb

from app.config import Settings
from app.schemas import SourceItem
from app.services.llm_service import LlmService


@dataclass
class RAGAnswer:
    answer: str
    sources: list[SourceItem]


class RagService:
    COLLECTION_NAME = "incusmart_knowledge"

    def __init__(self, settings: Settings, llm_service: LlmService):
        self._settings = settings
        self._llm_service = llm_service
        self._client = chromadb.PersistentClient(path=str(settings.chroma_dir))
        self._collection = self._client.get_or_create_collection(name=self.COLLECTION_NAME)

    def answer(self, question: str) -> RAGAnswer:
        result = self._collection.query(
            query_texts=[question],
            n_results=self._settings.max_rag_chunks,
        )

        documents = result.get("documents", [[]])[0]
        metadatas = result.get("metadatas", [[]])[0]

        sources: list[SourceItem] = []
        snippets: list[str] = []
        for doc, metadata in zip(documents, metadatas):
            metadata = metadata or {}
            snippets.append(doc)
            sources.append(
                SourceItem(
                    source=str(metadata.get("source", "unknown")),
                    section=metadata.get("section"),
                    topic=metadata.get("topic"),
                    excerpt=doc[:220],
                )
            )

        if not snippets:
            return RAGAnswer(
                answer="Tôi chưa có tài liệu phù hợp trong kho RAG để trả lời câu này.",
                sources=[],
            )

        if self._llm_service.is_enabled():
            prompt = (
                "Bạn là trợ lý kỹ thuật máy ấp trứng. "
                "Trả lời ngắn gọn bằng tiếng Việt, chỉ dựa trên context sau. "
                "Nếu context chưa đủ, nói rõ giới hạn.\n\n"
                f"Câu hỏi: {question}\n\n"
                "Context:\n"
                + "\n\n".join(snippets)
            )
            llm_text = self._llm_service.complete(prompt).text
            if llm_text:
                return RAGAnswer(answer=llm_text, sources=sources)

        answer = "Tóm tắt từ tài liệu:\n" + "\n".join(f"- {snippet[:220]}" for snippet in snippets[:3])
        return RAGAnswer(answer=answer, sources=sources)
