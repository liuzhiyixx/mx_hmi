$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $root "src"
$assetsDir = Join-Path $root "assets"
$outDir = Join-Path $root "bin"
$releaseDir = Join-Path $root "release"
$exe = Join-Path $outDir "MxHmiWindowHost.exe"
$releaseZip = Join-Path $releaseDir "MxHmiTopMost-v0.2.1.zip"
$icon = Join-Path $assetsDir "logo.ico"
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $csc)) {
    throw "csc.exe not found"
}

if (-not (Test-Path $icon)) {
    throw "logo.ico not found: $icon"
}

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir | Out-Null
}

& $csc `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /codepage:65001 `
    /win32icon:$icon `
    /out:$exe `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "$srcDir\AssemblyInfo.cs" `
    "$srcDir\Program.cs" `
    "$srcDir\AppSettings.cs" `
    "$srcDir\NativeMethods.cs" `
    "$srcDir\SettingsForm.cs" `
    "$srcDir\WindowHostForm.cs"

if ($LASTEXITCODE -ne 0) {
    throw "csc.exe failed with exit code $LASTEXITCODE"
}

Compress-Archive -Path $exe -DestinationPath $releaseZip -Force

Write-Host "Built: $exe"
Write-Host "Release: $releaseZip"
