# IncuSmart AI Chatbox

Python service v1 cho `knowledge` bằng RAG và `recommend` bằng `Gemini + KNN inference + rule + optional prebuilt model`.

## Scope

- `knowledge`: hỏi đáp kỹ thuật ấp trứng bằng tài liệu chunked + vector search
- `recommend`: sinh cấu hình theo phase/batch từ template, dữ liệu DB, synthetic data, rule hậu kiểm và bộ chấm điểm inference
- Không hỗ trợ `query_data`
- Không ghi vào PostgreSQL trong v1
- Không train model tại local hoặc VPS trong workflow mặc định

## Structure

- `app/main.py`: FastAPI entrypoint
- `app/api/chat.py`: `POST /chat`
- `app/services/rag_service.py`: ingest/retrieve/answer cho knowledge
- `app/services/recommend_service.py`: parse request, candidate generation, DB/synthetic KNN inference, heuristic fallback, optional prebuilt-model scoring, post-check
- `app/pipelines/ingest_rag_documents.py`: ingest tài liệu vào Chroma
- `app/streamlit_app.py`: frontend chatbox demo
- `scripts/test_gemini.py`: test kết nối Gemini local
- `scripts/generate_synthetic_data_with_gemini.py`: bơm synthetic hatch data qua Gemini

## Setup

1. Tạo virtual environment Python 3.11+
2. Cài package:

```bash
pip install -e .[dev]
```

3. Copy `.env.example` thành `.env` và chỉnh:
   - `AI_CHATBOX_POSTGRES_DSN`
   - `AI_CHATBOX_DOCS_DIR`
   - `AI_CHATBOX_LLM_*` để dùng Gemini
   - model mặc định nên là `gemini-2.5-flash`

4. Đặt tài liệu vào thư mục `docs/`, ví dụ:
   - FAO hatchery guide
   - SOP vận hành nội bộ
   - file markdown context nội bộ

Hoặc dùng script bootstrap:

```powershell
.\scripts\bootstrap_local.ps1
```

Tạo nhanh `.env` bằng prompt:

```powershell
.\scripts\make_env.ps1
```

Sau khi `.env` đã sẵn sàng, chạy one-command preflight:

```powershell
.\scripts\preflight.ps1
```

Nếu muốn chạy gần như toàn bộ local flow trong một lệnh:

```powershell
.\scripts\all_in_one.ps1
```

## Local-First Workflow

Ingest tài liệu RAG:

```bash
python scripts/ingest_rag.py
```

Re-run ingest whenever `AI_CHATBOX_EMBEDDING_MODEL` changes so stored document vectors and runtime query vectors use the same embedding space.

Test Gemini connection:

```bash
python scripts/test_gemini.py
```

`.env` tối thiểu cho Gemini:

```env
AI_CHATBOX_LLM_PROVIDER=gemini
AI_CHATBOX_LLM_API_KEY=YOUR_KEY
AI_CHATBOX_LLM_MODEL=gemini-2.5-flash
AI_CHATBOX_LLM_BASE_URL=https://generativelanguage.googleapis.com/v1beta/models
AI_CHATBOX_LLM_TIMEOUT_SECONDS=60
AI_CHATBOX_LLM_MAX_RETRIES=3
AI_CHATBOX_LLM_RETRY_BACKOFF_SECONDS=1
```

Generate synthetic dataset bằng Gemini:

```bash
python scripts/generate_synthetic_data_with_gemini.py
```

Generate and append multiple Gemini batches for local spam data:

```powershell
.\scripts\generate_synthetic_data_with_gemini.ps1 --records 10 --batches 5 --append
.\scripts\generate_synthetic_data_with_gemini.ps1 --egg-type duck --records 10 --batches 5 --append
```

Generate a balanced dataset for all supported egg types:

```powershell
.\scripts\spam_balanced_synthetic.ps1 -RecordsPerBatch 10 -BatchesPerEggType 3
```

The generated file is read as additional recommend candidates during inference. No local or VPS training is performed.

Inspect synthetic labels before using them for KNN inference:

```powershell
.\scripts\inspect_synthetic_data.ps1
.\scripts\inspect_synthetic_data.ps1 --strict
.\scripts\inspect_synthetic_data.ps1 --minimum-per-egg-type 10
```

Optional hardware-aware recommend context:

```json
{
  "user_context": {
    "incubator_id": "incubator-uuid",
    "ambient_temperature": 31,
    "ambient_humidity": 67
  }
}
```

When `incubator_id` is provided, final parameter values are clamped to read-only limits from `incubator_config_instances`.

Smoke test chat API:

```bash
python scripts/smoke_chat.py
```

Smoke test debug recommend:

```bash
python scripts/smoke_debug_recommend.py
```

Smoke test knowledge:

```bash
python scripts/smoke_knowledge.py
```

Smoke test debug knowledge:

```bash
python scripts/smoke_debug_knowledge.py
```

Run full API smoke suite after the server is up:

```powershell
.\scripts\smoke_suite.ps1
```

Optional:

```powershell
.\scripts\smoke_suite.ps1 --skip-knowledge
.\scripts\smoke_suite.ps1 --skip-recommend
.\scripts\smoke_suite.ps1 --include-debug
```

Debug endpoints are disabled by default. To enable `/debug/recommend` and `/debug/knowledge` for local testing:

```env
AI_CHATBOX_ENABLE_DEBUG_ENDPOINTS=true
```

Inspect whether ML inference has enough DB/synthetic labeled references:

```powershell
.\scripts\smoke_ml_status.ps1 --egg-type chicken
.\scripts\smoke_ml_evaluation.ps1 --egg-type chicken
.\scripts\smoke_ml_all.ps1
```

Default scoring precedence is `DB completed-season KNN -> blended/synthetic cold-start KNN -> heuristic`. KNN reads labels at runtime and requires no training job. Prebuilt RandomForest support remains optional but is disabled by default.

`ml-evaluation` performs leave-one-out inference evaluation and reports KNN MAE versus a mean-label baseline. It does not fit or save a model.

When testing newly updated DB outcomes, call `POST /debug/ml-cache/clear` to invalidate read-only ML reference caches without restarting the API.

Recommend responses include `scoring_mode`, `estimated_success_rate`, and `estimated_success_confidence` when a labeled KNN estimate is available. Heuristic fallback returns null estimates.

Every selected recommendation is validated again after rule and hardware-limit clamping. Invalid ML/template output falls back to the built-in safe profile, changes `scoring_mode` to `heuristic`, clears ML estimates/artifact trace, and returns details in `validation_warnings`.

Debug candidates and benchmark mode results expose `passed_output_validation` and `validation_warnings`, so model evaluation cannot hide unsafe or structurally incomplete selected outputs.
The strict model acceptance report rejects a prebuilt model when any supported egg type selects a config that fails output validation.
Colab validation must cover every egg type declared by the artifact. Runtime also rejects prebuilt predictions that are non-finite or outside the valid success-rate range `[0, 1]`.
Dataset manifests carry the configured DB/synthetic source weights into Colab, and model status exposes `validation_coverage_matches`.
Artifact quality gates apply the configured MAE limit both globally and independently to every supported egg type.

DB outcomes have a higher default KNN trust weight than Gemini synthetic labels. Synthetic references are capped per egg type so spam volume cannot completely dominate real outcomes.

Gemini `expected_success_rate` labels are calibrated with deterministic technical heuristics before KNN or RandomForest training. The raw Gemini label is never exported as an input feature.
Gemini synthetic records are accepted only when all three phases are contiguous and each phase has finite TEMP/HUMID/TURN/FAN ranges; the final phase must increase humidity and reduce turning.
Balanced spam/bundle scripts require the full requested `records per batch × batches per egg type` usable volume before export.
Synthetic generation persists the combined dataset atomically after every successful Gemini batch, so a later quota/network failure does not discard earlier valid records.

Export DB + Gemini labeled rows for Google Colab RandomForest training:

```powershell
.\scripts\export_ml_training_data.ps1
.\scripts\inspect_ml_export.ps1 --strict --require-all-egg-types
```

Use [colab/README.md](colab/README.md) for the actual training step. After downloading the artifact, validate it locally with `.\scripts\validate_model_artifact.ps1`.

Import a downloaded Colab ZIP safely, backup the previous artifact, and optionally enable it:

```powershell
.\scripts\import_model_artifact.ps1 C:\path\to\incusmart-model-output.zip
.\scripts\import_model_artifact.ps1 C:\path\to\incusmart-model-output.zip --enable
```

Artifacts are rejected by default when metrics are missing, test MAE exceeds `AI_CHATBOX_PREBUILT_MAX_MAE=0.20`, or RandomForest does not improve the mean-label baseline.

Rollback to the latest validated artifact backup:

```powershell
.\scripts\restore_model_backup.ps1
```

One-command import, enable, and validation:

```powershell
.\scripts\activate_colab_model.ps1 -Artifact C:\path\to\incusmart-model-output.zip
```

After restarting the API with debug endpoints enabled, run the strict post-activation acceptance gate:

```powershell
.\scripts\model_acceptance_report.ps1 --strict
```

Prepare a complete ZIP for Colab:

```powershell
.\scripts\prepare_colab_training_bundle.ps1
.\scripts\prepare_colab_training_bundle.ps1 -SkipGemini
```

With debug endpoints enabled, inspect the artifact loaded by the API:

```powershell
.\scripts\smoke_model_artifact_status.ps1
```

Artifact status also compares its training dataset SHA-256 with the current local export manifest, making stale-model drift visible.

Compare heuristic, KNN, and prebuilt-model outputs on the same candidate pool:

```powershell
.\scripts\smoke_ml_benchmark.ps1 --egg-type chicken --save-report
.\scripts\smoke_ml_benchmark_all.ps1
```

If you need another host or port, set:

```powershell
$env:AI_CHATBOX_BASE_URL="http://127.0.0.1:8002"
```

`run_api.ps1` also respects:

```powershell
$env:AI_CHATBOX_API_HOST="127.0.0.1"
$env:AI_CHATBOX_API_PORT="8002"
```

API logs are written to `storage/logs/uvicorn.log`.

Useful log helpers:

```powershell
.\scripts\clear_log.ps1
.\scripts\tail_log.ps1
.\scripts\open_log_dir.ps1
```

Quick environment summary:

```powershell
.\scripts\doctor.ps1
```

Export a session report:

```powershell
.\scripts\session_report.ps1
```

Reports include a masked config snapshot, so useful settings are visible without exposing the full Gemini key or full Postgres DSN.

Export a smoke suite report:

```powershell
.\scripts\smoke_suite_report.ps1
```

Export a bundled snapshot:

```powershell
.\scripts\bundle_report.ps1
```

Bundle first, then optionally stop API and cleanup:

```powershell
.\scripts\bundle_and_cleanup.ps1
.\scripts\bundle_and_cleanup.ps1 --stop-api
.\scripts\bundle_and_cleanup.ps1 --stop-api --include-synthetic
.\scripts\bundle_and_cleanup.ps1 --stop-api --full-cleanup
```

Cleanup helpers:

```powershell
.\scripts\cleanup.ps1
.\scripts\cleanup.ps1 --include-synthetic
.\scripts\cleanup.ps1 --include-chroma
.\scripts\cleanup.ps1 --full
```

Seed local docs và ingest trong một flow:

```powershell
.\scripts\ingest_seeded_docs.ps1
```

Preflight options:

```powershell
.\scripts\preflight.ps1 --skip-ingest
.\scripts\preflight.ps1 --skip-gemini
```

All-in-one options:

```powershell
.\scripts\all_in_one.ps1 --skip-ingest
.\scripts\all_in_one.ps1 --skip-gemini
.\scripts\all_in_one.ps1 --skip-bootstrap
.\scripts\all_in_one.ps1 --skip-make-env
.\scripts\all_in_one.ps1 --foreground
```

Recommend DB fallback:

- If PostgreSQL/template fetch fails, API still returns a built-in default config after rule post-check.
- The response marks `template_source` with `:db_fallback` and answer text explains that DB templates were not readable.
- This keeps local Gemini/RAG smoke testing usable without requiring a live database.

`recommend` chạy được ngay cả khi không có model artifact. Nếu sau này team có train model ở môi trường ngoài local/VPS, chỉ cần copy artifact vào `storage/models` và bật `AI_CHATBOX_USE_PREBUILT_MODEL=true`.

Lưu ý:

- Local repo chỉ dùng để test kết nối Gemini, ingest RAG và spam synthetic data
- Không xem local hoặc VPS là nơi train model
- Nếu cần model ML thật, artifact phải được train ở môi trường ngoài và copy vào `storage/models`

## Run API

```bash
uvicorn app.main:app --host 127.0.0.1 --port 8001 --reload
```

PowerShell helper:

```powershell
.\scripts\run_api.ps1
```

Run in background:

```powershell
.\scripts\run_api_background.ps1
.\scripts\api_status.ps1
.\scripts\stop_api.ps1
```

## Run Streamlit

```bash
streamlit run app/streamlit_app.py --server.port 8501
```

## IIS Deployment

Nếu deploy trên Windows/IIS, xem:

- [deploy/iis/README.md](C:/Users/GIGA/source/repos/IncuSmart/ai-chatbox/deploy/iis/README.md)
- [deploy/iis/security-checklist.md](C:/Users/GIGA/source/repos/IncuSmart/ai-chatbox/deploy/iis/security-checklist.md)
- [deploy/iis/web.config.example](C:/Users/GIGA/source/repos/IncuSmart/ai-chatbox/deploy/iis/web.config.example)

## API Contract

`POST /chat`

```json
{
  "message": "Đề xuất thông số ấp trứng gà cho 300 trứng",
  "session_id": "demo",
  "user_context": {
    "ambient_temperature": 31,
    "ambient_humidity": 68
  }
}
```

Response:

```json
{
  "intent": "recommend",
  "answer": "Đề xuất 3 phase cho mẻ trứng gà...",
  "sources": [],
  "recommended_config": []
}
```

`POST /predict-success`

```json
{
  "egg_type": "chicken",
  "total_eggs": 300,
  "incubator_id": "optional-guid",
  "ambient_temperature": 31,
  "ambient_humidity": 67
}
```

This endpoint reads completed seasons from PostgreSQL and keeps only a short-lived in-memory cache (`AI_CHATBOX_KNN_DB_CACHE_SECONDS`, default 300 seconds). Restarting the service clears that cache but does not lose ML data or require a training job; the durable source remains PostgreSQL. It uses DB-only KNN when enough real completed seasons exist. Gemini synthetic records are only a cold-start fallback.

The current Chroma RAG index is persisted in `AI_CHATBOX_CHROMA_DIR`; restarting the same service instance does not require re-ingesting documents. Set `AI_CHATBOX_RAG_PROVIDER=bigquery` after cloud ingestion to use BigQuery-hosted chunks and embeddings instead of local Chroma.

## BigQuery ML cloud training

BigQuery ML is the primary cloud-trained predictor when enabled. Local/VPS code only exports completed season outcomes and requests a Google Cloud training job; it does not fit a model locally.

1. Create a billed Google Cloud project and enable the BigQuery API.
2. Create a service account with BigQuery Job User and BigQuery Data Editor permissions.
3. Set `GOOGLE_APPLICATION_CREDENTIALS` to the service-account JSON path.
4. Configure:

```env
AI_CHATBOX_BIGQUERY_ML_ENABLED=true
AI_CHATBOX_BIGQUERY_PROJECT_ID=your-project-id
AI_CHATBOX_BIGQUERY_DATASET=incusmart_ml
AI_CHATBOX_BIGQUERY_LOCATION=asia-southeast1
```

Train or retrain the cloud model:

```powershell
.\.venv\Scripts\python.exe scripts\train_bigquery_ml.py
```

The script uploads one row per completed season and runs `CREATE OR REPLACE MODEL` using `LINEAR_REG` in BigQuery. `/predict-success` then calls `ML.PREDICT`. If BigQuery is unavailable or not configured, the current KNN path remains only as a temporary fallback.

Cost controls:

- Keep the dataset and model in `asia-southeast1`.
- Start with `LINEAR_REG`; do not enable `AUTOML_REGRESSOR` or a continuously deployed Vertex AI endpoint.
- Prediction queries use `AI_CHATBOX_BIGQUERY_PREDICTION_MAXIMUM_BYTES_BILLED` as a hard per-query byte limit.
- Run cloud training manually or on a low-frequency schedule, not on every completed season.

## BigQuery-hosted RAG

BigQuery RAG stores document chunks and embeddings online. The ingestion script extracts/chunks documents once, calls Gemini API `gemini-embedding-001` to generate vectors, and stores the vectors in BigQuery. Runtime retrieval embeds the user question with the same Gemini embedding model and uses BigQuery `VECTOR_SEARCH`; restarting the API does not scan or embed documents again.

Enable only the BigQuery API for Google Cloud. Do not enable Vertex AI Search, BigQuery Connection API, or Vertex AI API for this path.

Required services:

| Purpose | Service | Notes |
| --- | --- | --- |
| ML training and prediction | BigQuery + BigQuery ML | `CREATE OR REPLACE MODEL` and `ML.PREDICT` |
| RAG vector storage/search | BigQuery | Stores chunk metadata, text, and embedding arrays |
| RAG embedding | Gemini API / AI Studio | `gemini-embedding-001`, not Vertex |
| RAG answer generation | Gemini API / AI Studio | Prefer `gemini-2.5-flash-lite` for low cost |

Do not use:

| Service | Reason |
| --- | --- |
| Vertex AI Search / Conversational Search | Different SKU, not needed |
| Vertex AI Endpoint | Can incur online serving cost |
| Vertex AutoML | Not needed for current v1 |
| BigQuery Connection API | Only needed for BigQuery remote Vertex models, which this setup avoids |

Recommended config:

```env
AI_CHATBOX_BIGQUERY_ML_ENABLED=true
AI_CHATBOX_BIGQUERY_PROJECT_ID=your-project-id
AI_CHATBOX_BIGQUERY_DATASET=incusmart_ml
AI_CHATBOX_BIGQUERY_LOCATION=asia-southeast1
AI_CHATBOX_RAG_PROVIDER=bigquery
AI_CHATBOX_BIGQUERY_RAG_EMBEDDINGS_TABLE=rag_embeddings
AI_CHATBOX_LLM_PROVIDER=gemini
AI_CHATBOX_LLM_MODEL=gemini-2.5-flash-lite
AI_CHATBOX_LLM_EMBEDDING_MODEL=gemini-embedding-001
```

```powershell
.\.venv\Scripts\python.exe scripts\ingest_bigquery_rag.py
```

After ingestion succeeds:

```env
AI_CHATBOX_RAG_PROVIDER=bigquery
```

Do not run the ingestion script on API startup. Run it only when documents change because embedding generation uses Gemini API quota/cost.

Pricing references:

- BigQuery query/storage: https://cloud.google.com/bigquery/pricing
- BigQuery ML: https://cloud.google.com/bigquery/pricing#bqml
- Gemini API pricing: https://ai.google.dev/gemini-api/docs/pricing
- Gemini embeddings: https://ai.google.dev/gemini-api/docs/embeddings

## Minimal Local Test Order

1. `.\scripts\bootstrap_local.ps1`
2. `.\scripts\make_env.ps1`
3. `.\scripts\preflight.ps1`
4. `.\scripts\run_api.ps1`
5. `.\scripts\test_health.ps1`
6. `.\scripts\smoke_suite.ps1`

Fastest path:

1. `.\scripts\all_in_one.ps1`
