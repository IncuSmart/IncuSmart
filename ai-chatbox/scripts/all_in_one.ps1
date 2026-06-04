$ErrorActionPreference = "Stop"

param(
    [switch]$SkipBootstrap,
    [switch]$SkipMakeEnv,
    [switch]$SkipIngest,
    [switch]$SkipGemini,
    [switch]$Foreground
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

function Run-Step {
    param(
        [string]$Label,
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "== $Label ==" -ForegroundColor Cyan
    & $Action
}

if (-not $SkipBootstrap) {
    Run-Step "Bootstrap local environment" {
        & ".\scripts\bootstrap_local.ps1"
    }
}

if ((-not (Test-Path ".env")) -and (-not $SkipMakeEnv)) {
    Run-Step "Create .env" {
        & ".\scripts\make_env.ps1"
    }
}

if (-not (Test-Path ".env")) {
    throw ".env is missing. Run .\scripts\make_env.ps1 or create it manually."
}

$PreflightArgs = @()
if ($SkipIngest) { $PreflightArgs += "--skip-ingest" }
if ($SkipGemini) { $PreflightArgs += "--skip-gemini" }

Run-Step "Preflight" {
    & ".\scripts\preflight.ps1" @PreflightArgs
}

Run-Step "Clear log" {
    & ".\scripts\clear_log.ps1"
}

if ($Foreground) {
    Run-Step "Run API in foreground" {
        Write-Host "API will stay attached to this terminal."
        & ".\scripts\run_api.ps1"
    }
    return
}

Run-Step "Run API in background" {
    & ".\scripts\run_api_background.ps1"
}

Run-Step "Wait for API health" {
    $MaxAttempts = 20
    $Succeeded = $false
    for ($Attempt = 1; $Attempt -le $MaxAttempts; $Attempt++) {
        try {
            & ".\scripts\test_health.ps1" | Out-Null
            $Succeeded = $true
            break
        } catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $Succeeded) {
        throw "API health check did not pass in time. See storage/logs/uvicorn.log"
    }
}

Run-Step "Smoke suite" {
    & ".\scripts\smoke_suite.ps1"
}

Write-Host ""
Write-Host "All-in-one flow completed." -ForegroundColor Green
Write-Host "Useful follow-ups:"
Write-Host "  .\scripts\doctor.ps1"
Write-Host "  .\scripts\api_status.ps1"
Write-Host "  .\scripts\tail_log.ps1"
Write-Host "  .\scripts\stop_api.ps1"
