$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
$LogDir = Join-Path $ProjectRoot "storage\logs"
$LogFile = Join-Path $LogDir "uvicorn.log"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
}

if (Test-Path $LogFile) {
    Clear-Content -Path $LogFile
    Write-Host "Cleared $LogFile"
} else {
    New-Item -ItemType File -Path $LogFile | Out-Null
    Write-Host "Created empty log file at $LogFile"
}
