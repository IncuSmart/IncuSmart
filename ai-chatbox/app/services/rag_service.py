from __future__ import annotations

from dataclasses import dataclass
from threading import Lock
from typing import Any

import chromadb
from sentence_transformers import SentenceTransformer

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
        self._embedding_model: SentenceTransformer | None = None

    def answer(self, question: str) -> RAGAnswer:
        if self._bigquery_rag_service is not None and self._bigquery_rag_service.is_enabled():
            return self._answer_with_bigquery(question)
        return self._answer_with_chroma(question)

    def _answer_with_chroma(self, question: str) -> RAGAnswer:
        collection = self._get_collection()
        if collection is None:
            return RAGAnswer(
                answer="Kho RAG chua san sang. Hay ingest lai tai lieu truoc khi hoi kien thuc.",
                sources=[],
            )
        if collection.count() == 0:
            return self._empty_answer()

        try:
            query_embedding = self._get_embedding_model().encode([question]).tolist()
            result = collection.query(
                query_embeddings=query_embedding,
                n_results=min(collection.count(), self._settings.max_rag_chunks * 5),
            )
        except Exception:
            return RAGAnswer(
                answer="Kho RAG chua san sang hoac embedding khong tuong thich. Hay ingest lai tai lieu.",
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

        answer = "Tom tat tu tai lieu:\n" + "\n".join(f"- {snippet[:220]}" for snippet in snippets[:3])
        return RAGAnswer(answer=answer, sources=sources)

    def _answer_with_bigquery(self, question: str) -> RAGAnswer:
        try:
            chunks = self._bigquery_rag_service.retrieve(question, self._settings.max_rag_chunks)
        except Exception:
            return RAGAnswer(answer="Kho RAG BigQuery hien khong truy cap duoc.", sources=[])
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
        return RAGAnswer(answer=result.text or "Khong the tao cau tra loi tu context.", sources=sources)

    def _build_answer_prompt(self, question: str, snippets: list[str]) -> str:
        language_instruction = {
            "vi": "Chi tra loi bang tieng Viet.",
            "en": "Answer only in English.",
        }.get(self._settings.answer_language.lower(), f"Answer only in {self._settings.answer_language}.")
        return (
            "Ban la tro ly ky thuat may ap trung.\n"
            f"{language_instruction}\n"
            "Chi dung thong tin trong Context; khong dung kien thuc ngoai Context.\n"
            "Neu Context khong du de tra loi, khong nhac toi 'tai lieu' hay 'context'. "
            "Hay tu choi tu nhien bang tieng Viet, co the dien dat da dang, nhung phai noi ro rang cau hoi nam ngoai pham vi chu de ap trung.\n"
            "Neu Context co thuat ngu tieng Anh, hay dich hoac giai thich ngan trong ngoac.\n"
            "Voi do am, uu tien bieu dien bang %RH; neu co wet bulb, giai thich do la nhiet do bau uot, khong phai do am.\n"
            "Voi nhiet do, uu tien tra ve do Celsius (°C). Neu Context chi co Fahrenheit, hay quy doi sang °C va dat Fahrenheit trong ngoac.\n"
            "Tra loi toi da 3 cau, ngan gon, thuc dung, tranh liet ke dai.\n\n"
            f"Cau hoi: {question}\n\n"
            "Context:\n"
            + "\n\n".join(snippets)
        )

    def _get_embedding_model(self) -> SentenceTransformer:
        if self._embedding_model is None:
            self._embedding_model = SentenceTransformer(self._settings.embedding_model)
        return self._embedding_model

    def _get_collection(self):
        if self._collection is not None:
            return self._collection
        with self._collection_lock:
            if self._collection is not None:
                return self._collection
            try:
                self._client = chromadb.PersistentClient(path=str(self._settings.chroma_dir))
                self._collection = self._client.get_or_create_collection(name=self.COLLECTION_NAME)
            except Exception:
                self._client = None
                self._collection = None
        return self._collection

    def _empty_answer(self) -> RAGAnswer:
        return RAGAnswer(
            answer="Toi chua co tai lieu phu hop trong kho RAG de tra loi cau nay.",
            sources=[],
        )
