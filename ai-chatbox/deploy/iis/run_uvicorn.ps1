$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..\..")

Set-Location $ProjectRoot

if (Test-Path ".venv\Scripts\python.exe") {
    $PythonExe = Resolve-Path ".venv\Scripts\python.exe"
} else {
    $PythonExe = "python"
}

& $PythonExe -m uvicorn app.main:app --host 127.0.0.1 --port 8001
