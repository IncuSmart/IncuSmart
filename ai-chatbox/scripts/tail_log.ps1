$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
$LogFile = Join-Path $ProjectRoot "storage\logs\uvicorn.log"

if (-not (Test-Path $LogFile)) {
    Write-Error "Log file not found: $LogFile"
}

Get-Content -Path $LogFile -Tail 80 -Wait
