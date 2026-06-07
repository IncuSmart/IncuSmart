from app.config import Settings
from app.schemas import PredictSuccessRequest
from app.services.bigquery_ml_service import BigQueryMlService
from app.services.bigquery_rag_service import BigQueryRagService
from app.services.llm_service import EmbeddingResult


class _FakeQueryJob:
    def __init__(self, rows=None):
        self._rows = rows or []

    def result(self, *args, **kwargs):
        return self._rows


class _FakeLoadJob:
    def result(self):
        return None


class _FakeBigQueryModule:
    class QueryJobConfig:
        def __init__(self, **kwargs):
            self.kwargs = kwargs

    class ScalarQueryParameter:
        def __init__(self, name, kind, value):
            self.name = name
            self.kind = kind
            self.value = value

    class ArrayQueryParameter:
        def __init__(self, name, kind, value):
            self.name = name
            self.kind = kind
            self.value = value

    class LoadJobConfig:
        def __init__(self, **kwargs):
            self.kwargs = kwargs

    class SchemaField:
        def __init__(self, name, kind, mode=None):
            self.name = name
            self.kind = kind
            self.mode = mode

    class Dataset:
        def __init__(self, dataset_id):
            self.dataset_id = dataset_id
            self.location = None

    class WriteDisposition:
        WRITE_TRUNCATE = "WRITE_TRUNCATE"


class _FakeClient:
    def __init__(self, rows=None):
        self.rows = rows or []
        self.queries = []
        self.loaded_rows = None

    def query(self, query, job_config=None, location=None, timeout=None):
        self.queries.append((query, job_config, location))
        return _FakeQueryJob(self.rows)

    def create_dataset(self, dataset, exists_ok=False):
        return dataset

    def load_table_from_json(self, rows, table_id, job_config=None, location=None):
        self.loaded_rows = rows
        return _FakeLoadJob()


class _EmbeddingStub:
    def __init__(self):
        self.calls = []

    def embed_texts(self, texts, task_type):
        self.calls.append((texts, task_type))
        return [EmbeddingResult(values=[0.1, 0.2, 0.3]) for _ in texts]


def test_bigquery_ml_predict_generates_ml_predict_query() -> None:
    client = _FakeClient(rows=[{"predicted_success_rate": 0.87}])
    service = BigQueryMlService(
        Settings(
            bigquery_ml_enabled=True,
            bigquery_project_id="project-1",
            bigquery_dataset="incusmart_ml",
        )
    )
    service._client = client
    service._bigquery_module = lambda: _FakeBigQueryModule

    result = service.predict_success(PredictSuccessRequest(egg_type="chicken", total_eggs=100))

    assert result.success_rate == 0.87
    assert "ML.PREDICT" in client.queries[0][0]
    assert "`project-1.incusmart_ml.hatching_success_model`" in client.queries[0][0]


def test_bigquery_rag_upload_embeds_with_gemini_and_loads_vectors() -> None:
    client = _FakeClient()
    embedder = _EmbeddingStub()
    service = BigQueryRagService(
        Settings(
            rag_provider="bigquery",
            bigquery_project_id="project-1",
        ),
        embedder,
    )
    service._client = client
    service._bigquery_module = lambda: _FakeBigQueryModule

    uploaded = service.upload_and_embed(
        [
            {
                "chunk_id": "chunk-1",
                "source": "guide.md",
                "section": "chunk-1",
                "topic": "guide",
                "content": "hello",
            }
        ]
    )

    assert uploaded == 1
    assert embedder.calls == [(["hello"], "RETRIEVAL_DOCUMENT")]
    assert client.loaded_rows[0]["embedding"] == [0.1, 0.2, 0.3]
    assert client.queries == []


def test_bigquery_rag_retrieve_uses_vector_search() -> None:
    client = _FakeClient(
        rows=[
            {
                "source": "guide.md",
                "section": "chunk-1",
                "topic": "guide",
                "content": "content",
            }
        ]
    )
    embedder = _EmbeddingStub()
    service = BigQueryRagService(Settings(rag_provider="bigquery", bigquery_project_id="project-1"), embedder)
    service._client = client
    service._bigquery_module = lambda: _FakeBigQueryModule

    chunks = service.retrieve("question", 3)

    assert chunks[0].source == "guide.md"
    assert "VECTOR_SEARCH" in client.queries[0][0]
    assert "ML.GENERATE_EMBEDDING" not in client.queries[0][0]
    assert embedder.calls == [(["question"], "RETRIEVAL_QUERY")]
