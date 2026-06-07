from __future__ import annotations

from dataclasses import dataclass
import time
from typing import Any, Callable

import requests

from app.config import Settings


@dataclass
class LlmResult:
    text: str


@dataclass
class EmbeddingResult:
    values: list[float]


class LlmServiceError(RuntimeError):
    pass


class LlmService:
    def __init__(self, settings: Settings):
        self._settings = settings

    def is_enabled(self) -> bool:
        provider = self._settings.llm_provider.lower()
        if provider == "none":
            return False
        if provider == "gemini":
            return bool(self._settings.llm_api_key) and bool(self._settings.llm_model)
        return (
            bool(self._settings.llm_api_key)
            and bool(self._settings.llm_model)
            and bool(self._settings.llm_base_url)
        )

    def complete(self, prompt: str, temperature: float = 0.2) -> LlmResult:
        if not self.is_enabled():
            return LlmResult(text="")

        provider = self._settings.llm_provider.lower()
        if provider == "gemini":
            return self._complete_gemini(prompt, temperature)
        return self._complete_openai_compatible(prompt, temperature)

    def embed_texts(self, texts: list[str], task_type: str) -> list[EmbeddingResult]:
        if not texts:
            return []
        provider = self._settings.llm_provider.lower()
        if provider != "gemini" or not self._settings.llm_api_key:
            raise LlmServiceError("Gemini API key is required for cloud RAG embeddings.")
        return self._embed_texts_gemini(texts, task_type)

    def _complete_gemini(self, prompt: str, temperature: float) -> LlmResult:
        base_url = (
            self._settings.llm_base_url.rstrip("/")
            if self._settings.llm_base_url
            else "https://generativelanguage.googleapis.com/v1beta/models"
        )
        url = f"{base_url}/{self._settings.llm_model}:generateContent"
        response = self._post_with_retries(
            label="Gemini generateContent",
            request_fn=lambda: requests.post(
                url,
                headers={
                    "Content-Type": "application/json",
                },
                params={"key": self._settings.llm_api_key},
                json={
                    "contents": [{"parts": [{"text": prompt}]}],
                    "generationConfig": {
                        "temperature": temperature,
                    },
                },
                timeout=self._settings.llm_timeout_seconds,
            ),
        )
        data = response.json()
        candidates = data.get("candidates", [])
        if not candidates:
            return LlmResult(text="")
        parts = candidates[0].get("content", {}).get("parts", [])
        text = "\n".join(part.get("text", "") for part in parts if part.get("text"))
        return LlmResult(text=text.strip())

    def _embed_texts_gemini(self, texts: list[str], task_type: str) -> list[EmbeddingResult]:
        base_url = (
            self._settings.llm_base_url.rstrip("/")
            if self._settings.llm_base_url
            else "https://generativelanguage.googleapis.com/v1beta/models"
        )
        model = self._settings.llm_embedding_model
        url = f"{base_url}/{model}:batchEmbedContents"
        response = self._post_with_retries(
            label="Gemini batchEmbedContents",
            request_fn=lambda: requests.post(
                url,
                headers={"Content-Type": "application/json"},
                params={"key": self._settings.llm_api_key},
                json={
                    "requests": [
                        {
                            "model": f"models/{model}",
                            "content": {"parts": [{"text": text}]},
                            "taskType": task_type,
                        }
                        for text in texts
                    ]
                },
                timeout=self._settings.llm_timeout_seconds,
            ),
        )
        data = response.json()
        embeddings = data.get("embeddings", [])
        return [
            EmbeddingResult(values=[float(value) for value in item.get("values", [])])
            for item in embeddings
        ]

    def _complete_openai_compatible(self, prompt: str, temperature: float) -> LlmResult:
        response = self._post_with_retries(
            label="OpenAI-compatible chat/completions",
            request_fn=lambda: requests.post(
                self._settings.llm_base_url.rstrip("/") + "/chat/completions",
                headers={
                    "Authorization": f"Bearer {self._settings.llm_api_key}",
                    "Content-Type": "application/json",
                },
                json={
                    "model": self._settings.llm_model,
                    "temperature": temperature,
                    "messages": [{"role": "user", "content": prompt}],
                },
                timeout=self._settings.llm_timeout_seconds,
            ),
        )
        data = response.json()
        text = data["choices"][0]["message"]["content"]
        return LlmResult(text=text.strip())

    def _post_with_retries(self, label: str, request_fn: Callable[[], requests.Response]) -> requests.Response:
        max_retries = max(1, self._settings.llm_max_retries)
        backoff = max(0.0, self._settings.llm_retry_backoff_seconds)
        last_error: Exception | None = None

        for attempt in range(1, max_retries + 1):
            try:
                response = request_fn()
                if response.status_code in {408, 429, 500, 502, 503, 504} and attempt < max_retries:
                    time.sleep(backoff * attempt)
                    continue
                if not response.ok:
                    raise LlmServiceError(self._format_http_error(label, response))
                return response
            except (requests.Timeout, requests.ConnectionError) as exc:
                last_error = exc
                if attempt >= max_retries:
                    break
                time.sleep(backoff * attempt)

        raise LlmServiceError(f"{label} failed after {max_retries} attempts: {last_error}")

    def _format_http_error(self, label: str, response: requests.Response) -> str:
        details: Any
        try:
            details = response.json()
        except ValueError:
            details = response.text[:500]
        return f"{label} failed with HTTP {response.status_code}: {details}"
