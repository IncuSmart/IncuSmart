param(
    [string]$IisAppPool = "IncuSmartPool",
    [string]$IisPath = "C:\inetpub\incusmart",
    [string]$PublishPath = "publish",
    [string]$AiPath = "C:\inetpub\incusmart-ai",
    [string]$AiBaseUrl = "http://127.0.0.1:8001",
    [string]$PythonVersion = "3.12.10",
    [string]$PythonInstallDir = "C:\Python312"
)

$ErrorActionPreference = "Stop"

function Invoke-RobocopyMirror {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [string[]]$ExcludeDirs = @(),
        [string[]]$ExcludeFiles = @()
    )

    if (-not (Test-Path $Destination)) {
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    }

    $args = @($Source, $Destination, "/MIR", "/R:2", "/W:2")
    if ($ExcludeDirs.Count -gt 0) {
        $args += "/XD"
        $args += $ExcludeDirs
    }
    if ($ExcludeFiles.Count -gt 0) {
        $args += "/XF"
        $args += $ExcludeFiles
    }

    & robocopy @args
    $code = $LASTEXITCODE
    if ($code -gt 7) {
        throw "robocopy failed with exit code $code from $Source to $Destination"
    }
}

function Resolve-Python {
    param([string]$ProjectRoot)

    $venvPython = Join-Path $ProjectRoot ".venv\Scripts\python.exe"
    if (Test-Path $venvPython) {
        return (Resolve-Path $venvPython).Path
    }

    foreach ($commandName in @("python", "py", "python3")) {
        $command = Get-Command $commandName -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    return $null
}

function Install-PythonIfMissing {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [string]$Version,
        [string]$InstallDir
    )

    $existingPython = Resolve-Python -ProjectRoot $ProjectRoot
    if ($existingPython) {
        Write-Host "Python found: $existingPython"
        return $existingPython
    }

    Write-Host "Python was not found. Installing Python $Version..."

    $winget = Get-Command "winget" -ErrorAction SilentlyContinue
    if ($winget) {
        try {
            & winget install --id Python.Python.3.12 --exact --silent --accept-package-agreements --accept-source-agreements
            $installedPython = Resolve-Python -ProjectRoot $ProjectRoot
            if ($installedPython) {
                Write-Host "Python installed by winget: $installedPython"
                return $installedPython
            }
        } catch {
            Write-Warning "winget Python install failed, falling back to python.org installer: $($_.Exception.Message)"
        }
    }

    if (-not (Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    }

    $installerUrl = "https://www.python.org/ftp/python/$Version/python-$Version-amd64.exe"
    $installerPath = Join-Path $env:TEMP "python-$Version-amd64.exe"
    Write-Host "Downloading Python installer: $installerUrl"
    Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing

    Write-Host "Installing Python to $InstallDir"
    $process = Start-Process -FilePath $installerPath `
        -ArgumentList "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0 TargetDir=`"$InstallDir`"" `
        -Wait `
        -PassThru

    if ($process.ExitCode -ne 0) {
        throw "Python installer failed with exit code $($process.ExitCode)"
    }

    $installedPath = Join-Path $InstallDir "python.exe"
    if (Test-Path $installedPath) {
        Write-Host "Python installed: $installedPath"
        return $installedPath
    }

    $installedPython = Resolve-Python -ProjectRoot $ProjectRoot
    if ($installedPython) {
        Write-Host "Python installed: $installedPython"
        return $installedPython
    }

    throw "Python installation completed but python.exe could not be found."
}

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$PublishFullPath = Resolve-Path (Join-Path $RepoRoot $PublishPath)
$AiSourcePath = Resolve-Path (Join-Path $RepoRoot "ai-chatbox")

Write-Host "Deploying .NET API to IIS path: $IisPath"
& "$env:windir\system32\inetsrv\appcmd.exe" stop apppool "/apppool.name:$IisAppPool" | Out-Host
Invoke-RobocopyMirror `
    -Source $PublishFullPath `
    -Destination $IisPath `
    -ExcludeDirs @("logs") `
    -ExcludeFiles @("appsettings.json", "appsettings.*.json", "web.config")
& "$env:windir\system32\inetsrv\appcmd.exe" start apppool "/apppool.name:$IisAppPool" | Out-Host

Write-Host "Deploying AI service to: $AiPath"
Invoke-RobocopyMirror `
    -Source $AiSourcePath `
    -Destination $AiPath `
    -ExcludeDirs @(".venv", "storage", "secrets", "__pycache__", ".pytest_cache", "incusmart_ai_chatbox.egg-info", "colab") `
    -ExcludeFiles @(".env", "*.pyc")

Push-Location $AiPath
try {
    if (-not (Test-Path ".env")) {
        Copy-Item ".env.example" ".env"
        Write-Warning "Created $AiPath\.env from .env.example. Update production secrets before expecting AI calls to work."
    }

    if (-not (Test-Path ".venv\Scripts\python.exe")) {
        $systemPython = Install-PythonIfMissing -ProjectRoot $AiPath -Version $PythonVersion -InstallDir $PythonInstallDir
        & $systemPython -m venv .venv
    }

    $pythonExe = Resolve-Path ".venv\Scripts\python.exe"
    & $pythonExe -m pip install --upgrade pip
    & $pythonExe -m pip install -e .

    $env:JENKINS_NODE_COOKIE = "dontKillMe"
    $env:AI_CHATBOX_BASE_URL = $AiBaseUrl

    if (Test-Path "storage\run\api.pid") {
        try {
            & powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\stop_api.ps1"
        } catch {
            Write-Warning "AI stop step did not complete cleanly: $($_.Exception.Message)"
            Remove-Item "storage\run\api.pid" -Force -ErrorAction SilentlyContinue
        }
    }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\run_api_background.ps1"

    $deadline = (Get-Date).AddSeconds(45)
    do {
        try {
            $health = Invoke-RestMethod -Uri "$AiBaseUrl/health" -Method Get -TimeoutSec 5
            if ($health.status -eq "ok") {
                Write-Host "AI service health check passed at $AiBaseUrl"
                return
            }
        } catch {
            Start-Sleep -Seconds 3
        }
    } while ((Get-Date) -lt $deadline)

    throw "AI service did not become healthy at $AiBaseUrl within 45 seconds."
} finally {
    Pop-Location
}
