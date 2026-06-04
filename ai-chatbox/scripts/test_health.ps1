$ErrorActionPreference = "Stop"

$BaseUrl = if ($env:AI_CHATBOX_BASE_URL) { $env:AI_CHATBOX_BASE_URL.TrimEnd('/') } else { "http://127.0.0.1:8001" }
$health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get
$health | ConvertTo-Json -Depth 3
