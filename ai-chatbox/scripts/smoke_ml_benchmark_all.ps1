$ErrorActionPreference = "Stop"

foreach ($EggType in @("chicken", "duck", "quail", "goose")) {
    Write-Host ""
    Write-Host "ML benchmark: $EggType" -ForegroundColor Cyan
    & ".\scripts\smoke_ml_benchmark.ps1" --egg-type $EggType --save-report
}
