$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
$PidFile = Join-Path $ProjectRoot "storage\run\api.pid"

if (-not (Test-Path $PidFile)) {
    Write-Error "PID file not found: $PidFile"
}

$PidValue = Get-Content $PidFile -ErrorAction Stop
$Process = Get-Process -Id $PidValue -ErrorAction SilentlyContinue

if ($Process) {
    Stop-Process -Id $PidValue -Force
    Write-Host "Stopped API process PID $PidValue"
} else {
    Write-Host "No running process found for PID $PidValue"
}

Remove-Item $PidFile -Force -ErrorAction SilentlyContinue
