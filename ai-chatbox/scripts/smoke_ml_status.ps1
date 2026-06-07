$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

. (Join-Path $ScriptDir "_resolve_python.ps1")
$PythonExe = Resolve-AiChatboxPython -ProjectRoot $ProjectRoot

& $PythonExe "scripts\smoke_ml_status.py" @args
