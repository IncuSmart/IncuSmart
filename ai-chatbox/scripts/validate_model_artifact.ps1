$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

. (Join-Path $ScriptDir "_resolve_python.ps1")
$PythonExe = Resolve-AiChatboxPython -ProjectRoot $ProjectRoot

& $PythonExe "scripts\validate_model_artifact.py"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
