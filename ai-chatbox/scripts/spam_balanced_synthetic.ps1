param(
    [int]$RecordsPerBatch = 10,
    [int]$BatchesPerEggType = 3
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

foreach ($EggType in @("chicken", "duck", "quail", "goose")) {
    Write-Host ""
    Write-Host "Generating synthetic data for $EggType" -ForegroundColor Cyan
    & ".\scripts\generate_synthetic_data_with_gemini.ps1" `
        --egg-type $EggType `
        --records $RecordsPerBatch `
        --batches $BatchesPerEggType `
        --append
}

Write-Host ""
Write-Host "Inspecting combined synthetic dataset" -ForegroundColor Cyan
$MinimumPerEggType = $RecordsPerBatch * $BatchesPerEggType
& ".\scripts\inspect_synthetic_data.ps1" --strict --minimum-per-egg-type $MinimumPerEggType
