# Security Checklist: IIS + FastAPI + PostgreSQL

Checklist tối thiểu cho `ai-chatbox` khi deploy phía sau IIS.

## Network

- Chỉ public `80/443` trên IIS.
- Không public `8001`.
- FastAPI bind `127.0.0.1`, không bind `0.0.0.0`.
- Giữ Windows Firewall block inbound tới `8001`.

## IIS

- Bật HTTPS và redirect HTTP -> HTTPS.
- Bật `ARR` proxy nhưng chỉ route path cần thiết.
- Giới hạn request size cho `/ai`.
- Tắt directory browsing.
- Không để listing file hay lộ source/config.
- Thêm log cho path `/ai/*`.

## Python service

- Không chạy `--reload` ở production.
- Không bật debug mode.
- Không public OpenAPI docs nếu không cần.
- Dùng `.env` ngoài public web root.
- Không commit API key, DB password, LLM key.
- Log exception nhưng tránh log secret/input nhạy cảm nguyên văn.

## Authentication and abuse control

- Nếu chatbox không public hoàn toàn, thêm auth trước `/ai/chat`.
- Thêm rate limit ở IIS hoặc ở app layer.
- Giới hạn timeout request tới LLM và RAG.
- Giới hạn body size vì endpoint chỉ nhận text ngắn.

## Database

- Dùng account PostgreSQL read-only cho `ai-chatbox`.
- Chỉ cấp quyền `SELECT` cho bảng cần đọc.
- Không reuse credential full quyền từ backend chính.
- Giới hạn network access DB theo IP/VPS nếu có thể.

## Files and documents

- Tài liệu RAG chỉ chứa nội dung được phép expose cho chatbox.
- Không ingest file chứa credential, internal token, hoặc PII không cần thiết.
- Thư mục `storage/` và `docs/` không nên nằm dưới path public của IIS.

## Monitoring

- Có health check `/ai/health`.
- Theo dõi CPU/RAM vì embedding model và Chroma có thể tăng tải.
- Theo dõi log 4xx/5xx riêng cho `/ai/*`.
- Có quy trình restart service nếu uvicorn treo.

## Fast rollback

- Có thể tắt riêng route `/ai/*` mà không ảnh hưởng `.NET API`.
- Có thể stop Python service mà IIS/.NET vẫn chạy bình thường.
- Giữ deployment `ai-chatbox` tách biệt với publish `.NET`.
