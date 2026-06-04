$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
$PidFile = Join-Path $ProjectRoot "storage\run\api.pid"
$LogFile = Join-Path $ProjectRoot "storage\logs\uvicorn.log"

if (-not (Test-Path $PidFile)) {
    Write-Host "API status: not running (no PID file)"
    if (Test-Path $LogFile) {
        Write-Host "Log file: $LogFile"
    }
    exit 0
}

$PidValue = Get-Content $PidFile -ErrorAction Stop
$Process = Get-Process -Id $PidValue -ErrorAction SilentlyContinue

if ($Process) {
    Write-Host "API status: running"
    Write-Host "PID: $PidValue"
    Write-Host "Process name: $($Process.ProcessName)"
    Write-Host "Started: $($Process.StartTime)"
    if (Test-Path $LogFile) {
        Write-Host "Log file: $LogFile"
    }
} else {
    Write-Host "API status: stale PID file"
    Write-Host "PID: $PidValue"
    if (Test-Path $LogFile) {
        Write-Host "Log file: $LogFile"
    }
}
