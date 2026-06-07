$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

foreach ($EggType in @("chicken", "duck", "quail", "goose")) {
    Write-Host ""
    Write-Host "ML status: $EggType" -ForegroundColor Cyan
    & ".\scripts\smoke_ml_status.ps1" --egg-type $EggType

    Write-Host ""
    Write-Host "ML evaluation: $EggType" -ForegroundColor Cyan
    & ".\scripts\smoke_ml_evaluation.ps1" --egg-type $EggType
}
