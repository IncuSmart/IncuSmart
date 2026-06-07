$ErrorActionPreference = "Stop"

function Resolve-AiChatboxPython {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot
    )

    $VenvPython = Join-Path $ProjectRoot ".venv\Scripts\python.exe"
    if (Test-Path $VenvPython) {
        return (Resolve-Path $VenvPython).Path
    }

    foreach ($CommandName in @("python", "py", "python3")) {
        $Command = Get-Command $CommandName -ErrorAction SilentlyContinue
        if ($Command) {
            return $Command.Source
        }
    }

    throw "Python was not found. Install Python 3.11+ or create .venv, then rerun this script."
}
