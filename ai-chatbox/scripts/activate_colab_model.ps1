param(
    [Parameter(Mandatory = $true)]
    [string]$Artifact
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

Write-Host "Importing Colab model artifact" -ForegroundColor Cyan
& ".\scripts\import_model_artifact.ps1" $Artifact --enable
if ($LASTEXITCODE -ne 0) {
    throw "Colab model import failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Validating active model artifact" -ForegroundColor Cyan
& ".\scripts\validate_model_artifact.ps1"
if ($LASTEXITCODE -ne 0) {
    throw "Active model validation failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Colab model activation completed. Restart the API before smoke testing." -ForegroundColor Green
