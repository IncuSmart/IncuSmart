$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

. (Join-Path $ScriptDir "_resolve_python.ps1")
$PythonExe = Resolve-AiChatboxPython -ProjectRoot $ProjectRoot

$HostValue = if ($env:AI_CHATBOX_API_HOST) { $env:AI_CHATBOX_API_HOST } else { "127.0.0.1" }
$PortValue = if ($env:AI_CHATBOX_API_PORT) { $env:AI_CHATBOX_API_PORT } else { "8001" }
$LogDir = Join-Path $ProjectRoot "storage\logs"
$LogFile = Join-Path $LogDir "uvicorn.log"

if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
}

Write-Host "Starting API on $HostValue:$PortValue"
Write-Host "Logging to $LogFile"

@"
[$(Get-Date -Format s)] Starting uvicorn on $HostValue:$PortValue
"@ | Out-File -FilePath $LogFile -Encoding utf8 -Append

& $PythonExe -m uvicorn app.main:app --host $HostValue --port $PortValue --reload *>> $LogFile
