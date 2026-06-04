# IncuSmart AI Chatbox

Python service v1 cho `knowledge` bằng RAG và `recommend` bằng `Gemini + heuristic scoring + optional prebuilt model`.

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
- `app/services/recommend_service.py`: parse request, candidate generation, heuristic or prebuilt-model scoring, post-check
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

## Minimal Local Test Order

1. `.\scripts\bootstrap_local.ps1`
2. `.\scripts\make_env.ps1`
3. `.\scripts\preflight.ps1`
4. `.\scripts\run_api.ps1`
5. `.\scripts\test_health.ps1`
6. `.\scripts\smoke_suite.ps1`

Fastest path:

1. `.\scripts\all_in_one.ps1`
