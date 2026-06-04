$ErrorActionPreference = "Stop"

param(
    [switch]$IncludeChroma,
    [switch]$IncludeSynthetic,
    [switch]$IncludeModels,
    [switch]$Full
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")

if ($Full) {
    $IncludeChroma = $true
    $IncludeSynthetic = $true
    $IncludeModels = $true
}

$Targets = @(
    (Join-Path $ProjectRoot "storage\logs"),
    (Join-Path $ProjectRoot "storage\run")
)

if ($IncludeSynthetic) {
    $Targets += (Join-Path $ProjectRoot "storage\synthetic")
}

if ($IncludeChroma) {
    $Targets += (Join-Path $ProjectRoot "storage\chroma")
}

if ($IncludeModels) {
    $Targets += (Join-Path $ProjectRoot "storage\models")
}

foreach ($Target in $Targets) {
    if (Test-Path $Target) {
        Remove-Item -LiteralPath $Target -Recurse -Force
        Write-Host "Removed $Target"
    } else {
        Write-Host "Skip missing $Target"
    }
}

Write-Host "Cleanup completed."
