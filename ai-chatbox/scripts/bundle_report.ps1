$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$ReportsRoot = Join-Path $ProjectRoot "storage\reports"
$BundleDir = Join-Path $ReportsRoot "bundle-$Timestamp"
$LogFile = Join-Path $ProjectRoot "storage\logs\uvicorn.log"
$TailFile = Join-Path $BundleDir "uvicorn-tail.log"
$SummaryFile = Join-Path $BundleDir "bundle-summary.txt"

if (-not (Test-Path $ReportsRoot)) {
    New-Item -ItemType Directory -Force -Path $ReportsRoot | Out-Null
}
New-Item -ItemType Directory -Force -Path $BundleDir | Out-Null

Write-Host "Creating bundle at $BundleDir"

& ".\scripts\session_report.ps1"
& ".\scripts\smoke_suite_report.ps1"

$LatestSessionReport = Get-ChildItem $ReportsRoot -Filter "session-report-*.json" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

$LatestSmokeReport = Get-ChildItem $ReportsRoot -Filter "smoke-suite-report-*.json" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($LatestSessionReport) {
    Copy-Item $LatestSessionReport.FullName (Join-Path $BundleDir $LatestSessionReport.Name) -Force
}

if ($LatestSmokeReport) {
    Copy-Item $LatestSmokeReport.FullName (Join-Path $BundleDir $LatestSmokeReport.Name) -Force
}

if (Test-Path $LogFile) {
    Get-Content -Path $LogFile -Tail 120 | Set-Content -Path $TailFile -Encoding utf8
}

$Lines = @()
$Lines += "bundle_dir=$BundleDir"
$Lines += "generated_at=$(Get-Date -Format s)"
$Lines += "session_report=" + $(if ($LatestSessionReport) { $LatestSessionReport.Name } else { "" })
$Lines += "smoke_suite_report=" + $(if ($LatestSmokeReport) { $LatestSmokeReport.Name } else { "" })
$Lines += "log_tail_file=" + $(if (Test-Path $TailFile) { (Split-Path $TailFile -Leaf) } else { "" })
$Lines | Set-Content -Path $SummaryFile -Encoding utf8

Write-Host "Bundle complete."
Write-Host "Summary: $SummaryFile"
