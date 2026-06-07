$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot
. (Join-Path $ScriptDir "_resolve_python.ps1")

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "Created .env from .env.example. Update AI_CHATBOX_LLM_API_KEY before calling Gemini."
}

if (-not (Test-Path ".venv\Scripts\python.exe")) {
    $SystemPython = Resolve-AiChatboxPython -ProjectRoot $ProjectRoot
    & $SystemPython -m venv .venv
}

$PythonExe = Resolve-Path ".venv\Scripts\python.exe"

& $PythonExe -m pip install --upgrade pip
& $PythonExe -m pip install -e .[dev]

Write-Host ""
Write-Host "Bootstrap complete."
Write-Host "Next:"
Write-Host "1. Edit .env"
Write-Host "2. Run: .\scripts\test_gemini.ps1"
Write-Host "3. Run: .\scripts\run_api.ps1"
