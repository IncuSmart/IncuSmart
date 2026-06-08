from __future__ import annotations

import hashlib
from dataclasses import dataclass
from threading import Lock
from typing import Any

import chromadb

from app.config import Settings
from app.schemas import SourceItem
from app.services.bigquery_rag_service import BigQueryRagService
from app.services.llm_service import LlmService


@dataclass
class RAGAnswer:
    answer: str
    sources: list[SourceItem]


class RagService:
    COLLECTION_NAME = "incusmart_knowledge"

    def __init__(
        self,
        settings: Settings,
        llm_service: LlmService,
        bigquery_rag_service: BigQueryRagService | None = None,
    ):
        self._settings = settings
        self._llm_service = llm_service
        self._bigquery_rag_service = bigquery_rag_service
        self._client: Any | None = None
        self._collection: Any | None = None
        self._collection_lock = Lock()

    def answer(self, question: str) -> RAGAnswer:
        if self._bigquery_rag_service is not None and self._bigquery_rag_service.is_enabled():
            return self._answer_with_bigquery(question)
        return self._answer_with_chroma(question)

    def _answer_with_chroma(self, question: str) -> RAGAnswer:
        collection = self._get_collection()
        if collection is None:
            return RAGAnswer(
                answer="Hệ thống dữ liệu chưa sẵn sàng. Vui lòng thử lại sau.",
                sources=[],
            )
        if collection.count() == 0:
            return self._empty_answer()

        try:
            query_embedding = self._embed_query(question)
            result = collection.query(
                query_embeddings=[query_embedding],
                n_results=min(collection.count(), self._settings.max_rag_chunks * 5),
            )
        except Exception:
            return RAGAnswer(
                answer="Hệ thống dữ liệu chưa sẵn sàng hoặc dữ liệu tìm kiếm không tương thích.",
                sources=[],
            )

        documents = result.get("documents", [[]])[0]
        metadatas = result.get("metadatas", [[]])[0]
        selected: list[tuple[str, dict]] = []
        selected_sources: set[str] = set()
        for document, metadata in zip(documents, metadatas):
            metadata = metadata or {}
            source = str(metadata.get("source", "unknown"))
            if source in selected_sources:
                continue
            selected.append((document, metadata))
            selected_sources.add(source)
            if len(selected) >= self._settings.max_rag_chunks:
                break
        if len(selected) < self._settings.max_rag_chunks:
            for document, metadata in zip(documents, metadatas):
                metadata = metadata or {}
                if (document, metadata) not in selected:
                    selected.append((document, metadata))
                if len(selected) >= self._settings.max_rag_chunks:
                    break

        sources: list[SourceItem] = []
        snippets: list[str] = []
        for document, metadata in selected:
            snippets.append(document)
            sources.append(
                SourceItem(
                    source=str(metadata.get("source", "unknown")),
                    section=metadata.get("section"),
                    topic=metadata.get("topic"),
                    excerpt=document[:220],
                )
            )

        if not snippets:
            return self._empty_answer()

        if self._llm_service.is_enabled():
            llm_text = self._llm_service.complete(self._build_answer_prompt(question, snippets)).text
            if llm_text:
                return RAGAnswer(answer=llm_text, sources=sources)

        answer = "Tóm tắt từ dữ liệu hiện có:\n" + "\n".join(
            f"- {snippet[:220]}" for snippet in snippets[:3]
        )
        return RAGAnswer(answer=answer, sources=sources)

    def _answer_with_bigquery(self, question: str) -> RAGAnswer:
        try:
            chunks = self._bigquery_rag_service.retrieve(question, self._settings.max_rag_chunks)
        except Exception:
            return RAGAnswer(answer="Hiện không truy cập được vào hệ thống dữ liệu.", sources=[])
        if not chunks:
            return self._empty_answer()

        sources = [
            SourceItem(
                source=chunk.source,
                section=chunk.section,
                topic=chunk.topic,
                excerpt=chunk.content[:220],
            )
            for chunk in chunks
        ]
        result = self._llm_service.complete(
            self._build_answer_prompt(question, [chunk.content for chunk in chunks])
        )
        return RAGAnswer(
            answer=result.text or "Chưa thể tạo câu trả lời từ dữ liệu hiện có.",
            sources=sources,
        )

    def _build_answer_prompt(self, question: str, snippets: list[str]) -> str:
        language_instruction = {
            "vi": "Chỉ trả lời bằng tiếng Việt.",
            "en": "Answer only in English.",
        }.get(self._settings.answer_language.lower(), f"Answer only in {self._settings.answer_language}.")
        return (
            "Bạn là trợ lý kỹ thuật máy ấp trứng.\n"
            f"{language_instruction}\n"
            "Chỉ dùng thông tin trong Context; không dùng kiến thức ngoài Context.\n"
            "Nếu Context không đủ để trả lời, không nhắc tới 'tài liệu' hay 'context'. "
            "Hãy từ chối tự nhiên bằng tiếng Việt, có thể diễn đạt đa dạng, nhưng phải nói rõ câu hỏi nằm ngoài phạm vi chủ đề ấp trứng.\n"
            "Nếu Context có thuật ngữ tiếng Anh, hãy dịch hoặc giải thích ngắn trong ngoặc.\n"
            "Với độ ẩm, ưu tiên biểu diễn bằng %RH; nếu có wet bulb, giải thích đó là nhiệt độ bầu ướt, không phải độ ẩm.\n"
            "Với nhiệt độ, ưu tiên trả về độ Celsius (°C). Nếu Context chỉ có Fahrenheit, hãy quy đổi sang °C và đặt Fahrenheit trong ngoặc.\n"
            "Trả lời tối đa 3 câu, ngắn gọn, thực dụng, tránh liệt kê dài.\n\n"
            f"Câu hỏi: {question}\n\n"
            "Context:\n"
            + "\n\n".join(snippets)
        )

    def _embed_documents(self, documents: list[str]) -> list[list[float]]:
        return [
            embedding.values
            for embedding in self._llm_service.embed_texts(documents, "RETRIEVAL_DOCUMENT")
        ]

    def _embed_query(self, query: str) -> list[float]:
        return self._llm_service.embed_texts([query], "RETRIEVAL_QUERY")[0].values

    def _get_collection(self):
        if self._collection is not None:
            return self._collection
        with self._collection_lock:
            if self._collection is not None:
                return self._collection
            try:
                self._client = chromadb.PersistentClient(path=str(self._settings.chroma_dir))
                self._collection = self._client.get_or_create_collection(
                    name=self.COLLECTION_NAME,
                    embedding_function=None,
                )
            except Exception:
                self._client = None
                self._collection = None
        return self._collection

    def get_chunk_count(self) -> int:
        collection = self._get_collection()
        return collection.count() if collection is not None else 0

    def ingest_text(self, text: str, source: str, topic: str) -> int:
        from app.pipelines.ingest_rag_documents import chunk_text

        collection = self._get_collection()
        if collection is None:
            raise RuntimeError("Hệ thống dữ liệu không khả dụng.")

        chunks = chunk_text(text)
        if not chunks:
            return 0

        embeddings = self._embed_documents(chunks)
        source_id = hashlib.sha256(source.encode("utf-8")).hexdigest()[:16]
        ids = [f"{source_id}-{i}" for i in range(len(chunks))]
        metadatas = [
            {"source": source, "section": f"chunk-{i}", "topic": topic}
            for i in range(len(chunks))
        ]
        collection.upsert(ids=ids, documents=chunks, metadatas=metadatas, embeddings=embeddings)
        return len(chunks)

    def _empty_answer(self) -> RAGAnswer:
        return RAGAnswer(
            answer="Tôi chưa có dữ liệu phù hợp để trả lời câu này.",
            sources=[],
        )
