# Run FastAPI as a Windows Service with NSSM

Ví dụ dùng `NSSM` để chạy `ai-chatbox` như service nền.

## Prepare

1. Tạo virtualenv trong `ai-chatbox`
2. Cài dependency
3. Tạo `.env`
4. Chạy thử local:

```powershell
.\deploy\iis\run_uvicorn.ps1
```

Nếu `http://127.0.0.1:8001/health` trả `ok`, mới chuyển sang service.

## NSSM example

Giả sử:

- project root: `C:\deploy\IncuSmart\ai-chatbox`
- powershell: `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`

Command:

```powershell
nssm install IncuSmartAiChatbox
```

Thiết lập:

- Application: `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
- Startup directory: `C:\deploy\IncuSmart\ai-chatbox`
- Arguments:

```text
-ExecutionPolicy Bypass -File C:\deploy\IncuSmart\ai-chatbox\deploy\iis\run_uvicorn.ps1
```

## Recommended NSSM settings

- Startup type: `Automatic`
- I/O redirection: log stdout/stderr ra file riêng
- Restart action: restart on failure

## After install

1. Start service
2. Kiểm tra `http://127.0.0.1:8001/health`
3. Kiểm tra route public qua IIS `/ai/health`
