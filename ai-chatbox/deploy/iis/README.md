# IIS Reverse Proxy Deployment

Tài liệu này hướng dẫn publish `ai-chatbox` phía sau `IIS` bằng `reverse proxy`, theo hướng:

- `IncuSmart.API` vẫn chạy như hiện tại
- `ai-chatbox` chạy process Python riêng ở `127.0.0.1:8001`
- `IIS` public ra internet và route `/ai/*` vào Python service

## Recommended topology

```text
Internet
  |
  v
IIS (80/443)
  |-----------------> /api/*  -> ASP.NET Core backend
  \-----------------> /ai/*   -> http://127.0.0.1:8001/*
```

## Why this is the recommended setup

- Không expose trực tiếp port `8001` ra ngoài.
- SSL/HTTPS terminate ở IIS.
- Có thể gom domain vào một chỗ.
- Dễ log, rate limit, block IP, và quản lý header.
- Python service chỉ cần bind loopback `127.0.0.1`.

## IIS prerequisites

Trên máy Windows/IIS cần cài:

1. `IIS`
2. `Application Request Routing (ARR)`
3. `URL Rewrite`
4. `WebSocket` nếu sau này cần streaming hoặc realtime

Sau khi cài ARR:

1. Mở `IIS Manager`
2. Chọn server node
3. Mở `Application Request Routing Cache`
4. Chọn `Server Proxy Settings`
5. Bật `Enable proxy`

## Run Python service locally only

Không chạy uvicorn ở `0.0.0.0` trong production nếu chỉ dùng qua IIS.

Dùng:

```powershell
uvicorn app.main:app --host 127.0.0.1 --port 8001
```

Hoặc chạy qua script mẫu ở thư mục này.

## Reverse proxy strategy

Có 2 cách phổ biến:

### Option A: Same domain, path-based routing

- `https://api-incusmart.io.vn/api/*` -> `.NET API`
- `https://api-incusmart.io.vn/ai/*` -> `FastAPI`

Đây là cách phù hợp nhất nếu muốn giữ 1 domain.

### Option B: Separate subdomain

- `https://api-incusmart.io.vn/*` -> `.NET API`
- `https://ai-incusmart.io.vn/*` -> `FastAPI`

Cách này sạch hơn về routing nhưng cần thêm DNS/site.

## Sample IIS rule

Xem file [web.config.example](C:/Users/GIGA/source/repos/IncuSmart/ai-chatbox/deploy/iis/web.config.example).

Rule đó giả định:

- Request vào `/ai/...`
- IIS rewrite sang `http://127.0.0.1:8001/...`
- Prefix `/ai` sẽ bị bỏ đi trước khi forward

Ví dụ:

- `/ai/chat` -> `http://127.0.0.1:8001/chat`
- `/ai/health` -> `http://127.0.0.1:8001/health`

## Windows service recommendation

Để process Python tự khởi động cùng máy, nên chạy bằng:

- `NSSM`
- hoặc `Task Scheduler`
- hoặc service wrapper nội bộ của team

Khuyên dùng `NSSM` vì đơn giản và ổn định hơn chạy tay.

Quy trình:

1. Tạo virtualenv
2. Cài dependency
3. Chạy script `run_uvicorn.ps1`
4. Gắn script hoặc python command đó vào `NSSM`

## Firewall

Nên để:

- mở `80/443` public
- chặn inbound public đến `8001`

Nếu service bind `127.0.0.1`, bản thân nó đã không nghe public interface, nhưng vẫn nên giữ firewall chặt.

## Health check

Route:

- public: `/ai/health`
- internal target: `http://127.0.0.1:8001/health`

Nên dùng route này để kiểm tra sau khi setup ARR.

## Important note for existing .NET deployment

Nếu `.NET` backend hiện đang dùng root site ở IIS, đừng sửa publish output của `.NET` để chèn rule này một cách mù quáng.

An toàn hơn:

1. giữ site `.NET` như cũ
2. thêm application hoặc virtual directory riêng cho `/ai`
3. đặt `web.config.example` vào application đó

Nếu team muốn route `/ai/*` ngay từ root site, cần merge rule cẩn thận với config publish hiện có của ASP.NET Core.
