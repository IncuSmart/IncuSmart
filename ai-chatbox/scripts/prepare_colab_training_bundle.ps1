param(
    [switch]$SkipGemini,
    [int]$RecordsPerBatch = 10,
    [int]$BatchesPerEggType = 3
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..")
Set-Location $ProjectRoot

if (-not $SkipGemini) {
    & ".\scripts\spam_balanced_synthetic.ps1" `
        -RecordsPerBatch $RecordsPerBatch `
        -BatchesPerEggType $BatchesPerEggType
}

$MinimumPerEggType = $RecordsPerBatch * $BatchesPerEggType
& ".\scripts\inspect_synthetic_data.ps1" --strict --minimum-per-egg-type $MinimumPerEggType
& ".\scripts\export_ml_training_data.ps1"
& ".\scripts\inspect_ml_export.ps1" --strict --require-all-egg-types

$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$BundleRoot = Join-Path $ProjectRoot "storage\colab-bundles"
$BundleDir = Join-Path $BundleRoot "incusmart-colab-$Timestamp"
$ZipPath = "$BundleDir.zip"
New-Item -ItemType Directory -Force -Path $BundleDir | Out-Null

Copy-Item "storage\ml-export\recommendation_training_rows.jsonl" $BundleDir
Copy-Item "storage\ml-export\recommendation_training_rows.manifest.json" $BundleDir
Copy-Item "colab\train_random_forest.py" $BundleDir
Copy-Item "colab\incusmart_random_forest_colab.ipynb" $BundleDir
Copy-Item "colab\README.md" $BundleDir

Compress-Archive -Path (Join-Path $BundleDir "*") -DestinationPath $ZipPath -Force

Write-Host ""
Write-Host "Colab training bundle created:" -ForegroundColor Green
Write-Host $ZipPath
