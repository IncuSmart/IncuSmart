$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
$TemplatePath = Join-Path $ProjectRoot ".env.example"
$EnvPath = Join-Path $ProjectRoot ".env"

if (-not (Test-Path $TemplatePath)) {
    Write-Error "Template not found: $TemplatePath"
}

$Template = Get-Content -Path $TemplatePath -Raw -Encoding utf8

$DefaultDsn = "postgresql+psycopg://postgres:password@localhost:5432/incu_smart_test"
$DefaultHost = "127.0.0.1"
$DefaultPort = "8001"
$DefaultDocsDir = "./docs"
$DefaultModel = "gemini-2.5-flash"
$DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models"

$GeminiKey = Read-Host "Gemini API key"
$PostgresDsn = Read-Host "Postgres DSN [$DefaultDsn]"
if ([string]::IsNullOrWhiteSpace($PostgresDsn)) { $PostgresDsn = $DefaultDsn }

$ApiHost = Read-Host "API host [$DefaultHost]"
if ([string]::IsNullOrWhiteSpace($ApiHost)) { $ApiHost = $DefaultHost }

$ApiPort = Read-Host "API port [$DefaultPort]"
if ([string]::IsNullOrWhiteSpace($ApiPort)) { $ApiPort = $DefaultPort }

$DocsDir = Read-Host "Docs dir [$DefaultDocsDir]"
if ([string]::IsNullOrWhiteSpace($DocsDir)) { $DocsDir = $DefaultDocsDir }

$Model = Read-Host "Gemini model [$DefaultModel]"
if ([string]::IsNullOrWhiteSpace($Model)) { $Model = $DefaultModel }

$BaseUrl = Read-Host "Gemini base URL [$DefaultBaseUrl]"
if ([string]::IsNullOrWhiteSpace($BaseUrl)) { $BaseUrl = $DefaultBaseUrl }

$Content = $Template `
    -replace '(?m)^AI_CHATBOX_LLM_API_KEY=.*$', "AI_CHATBOX_LLM_API_KEY=$GeminiKey" `
    -replace '(?m)^AI_CHATBOX_POSTGRES_DSN=.*$', "AI_CHATBOX_POSTGRES_DSN=$PostgresDsn" `
    -replace '(?m)^AI_CHATBOX_API_HOST=.*$', "AI_CHATBOX_API_HOST=$ApiHost" `
    -replace '(?m)^AI_CHATBOX_API_PORT=.*$', "AI_CHATBOX_API_PORT=$ApiPort" `
    -replace '(?m)^AI_CHATBOX_DOCS_DIR=.*$', "AI_CHATBOX_DOCS_DIR=$DocsDir" `
    -replace '(?m)^AI_CHATBOX_LLM_MODEL=.*$', "AI_CHATBOX_LLM_MODEL=$Model" `
    -replace '(?m)^AI_CHATBOX_LLM_BASE_URL=.*$', "AI_CHATBOX_LLM_BASE_URL=$BaseUrl"

Set-Content -Path $EnvPath -Value $Content -Encoding utf8

Write-Host "Wrote $EnvPath"
