$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "scripts\build.ps1"
powershell.exe -ExecutionPolicy Bypass -File $scriptPath

if ($LASTEXITCODE -ne 0) {
    throw "build failed with exit code $LASTEXITCODE"
}
