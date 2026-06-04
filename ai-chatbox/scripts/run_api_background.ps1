$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

. (Join-Path $ScriptDir "_resolve_python.ps1")
$PythonExe = Resolve-AiChatboxPython -ProjectRoot $ProjectRoot

$HostValue = if ($env:AI_CHATBOX_API_HOST) { $env:AI_CHATBOX_API_HOST } else { "127.0.0.1" }
$PortValue = if ($env:AI_CHATBOX_API_PORT) { $env:AI_CHATBOX_API_PORT } else { "8001" }
$LogDir = Join-Path $ProjectRoot "storage\logs"
$RunDir = Join-Path $ProjectRoot "storage\run"
$LogFile = Join-Path $LogDir "uvicorn.log"
$PidFile = Join-Path $RunDir "api.pid"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
}
if (-not (Test-Path $RunDir)) {
    New-Item -ItemType Directory -Force -Path $RunDir | Out-Null
}

if (Test-Path $PidFile) {
    $ExistingPid = Get-Content $PidFile -ErrorAction SilentlyContinue
    if ($ExistingPid) {
        $ExistingProcess = Get-Process -Id $ExistingPid -ErrorAction SilentlyContinue
        if ($ExistingProcess) {
            Write-Error "API already appears to be running with PID $ExistingPid"
        }
    }
    Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
}

$ArgumentList = "-m uvicorn app.main:app --host $HostValue --port $PortValue --reload"
$Process = Start-Process -FilePath $PythonExe `
    -ArgumentList $ArgumentList `
    -WorkingDirectory $ProjectRoot `
    -RedirectStandardOutput $LogFile `
    -RedirectStandardError $LogFile `
    -WindowStyle Hidden `
    -PassThru

$Process.Id | Out-File -FilePath $PidFile -Encoding ascii -Force

Write-Host "Started API in background."
Write-Host "PID: $($Process.Id)"
Write-Host "Host: $HostValue"
Write-Host "Port: $PortValue"
Write-Host "Log: $LogFile"
Write-Host "PID file: $PidFile"
