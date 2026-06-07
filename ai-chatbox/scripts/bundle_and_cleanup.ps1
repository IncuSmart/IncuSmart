$ErrorActionPreference = "Stop"

param(
    [switch]$StopApi,
    [switch]$IncludeSynthetic,
    [switch]$IncludeChroma,
    [switch]$IncludeModels,
    [switch]$FullCleanup
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

Write-Host ""
Write-Host "== Bundle report ==" -ForegroundColor Cyan
& ".\scripts\bundle_report.ps1"

if ($StopApi) {
    Write-Host ""
    Write-Host "== Stop API ==" -ForegroundColor Cyan
    try {
        & ".\scripts\stop_api.ps1"
    } catch {
        Write-Warning "stop_api.ps1 did not complete cleanly: $($_.Exception.Message)"
    }
}

$CleanupArgs = @()
if ($IncludeSynthetic) { $CleanupArgs += "--include-synthetic" }
if ($IncludeChroma) { $CleanupArgs += "--include-chroma" }
if ($IncludeModels) { $CleanupArgs += "--include-models" }
if ($FullCleanup) { $CleanupArgs += "--full" }

if ($CleanupArgs.Count -gt 0) {
    Write-Host ""
    Write-Host "== Cleanup ==" -ForegroundColor Cyan
    & ".\scripts\cleanup.ps1" @CleanupArgs
} else {
    Write-Host ""
    Write-Host "Skipping cleanup. Pass flags if you want cleanup after bundling." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "bundle_and_cleanup completed." -ForegroundColor Green
