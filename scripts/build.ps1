<#
.SYNOPSIS
    Builds the Caret Crosshair portable exe, then (if Inno Setup is
    available) the installer on top of it.

.DESCRIPTION
    1. dotnet publish -c Release for src\CaretCrosshair
       -> bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\CaretCrosshair.exe
    2. ISCC.exe installer\CaretCrosshair.iss (unless -SkipInstaller)
       -> installer\dist\CaretCrosshairSetup.exe

.PARAMETER SkipInstaller
    Build only the portable exe; don't look for or run Inno Setup.

.EXAMPLE
    .\scripts\build.ps1
    .\scripts\build.ps1 -SkipInstaller
#>
[CmdletBinding()]
param(
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'src\CaretCrosshair'
$publishDir = Join-Path $projectDir 'bin\Release\net8.0-windows10.0.19041.0\win-x64\publish'
$exePath = Join-Path $publishDir 'CaretCrosshair.exe'
$installerScript = Join-Path $repoRoot 'installer\CaretCrosshair.iss'
$installerOutput = Join-Path $repoRoot 'installer\dist\CaretCrosshairSetup.exe'

function Write-Step($message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

# --- 1. Portable exe -------------------------------------------------------

Write-Step "Publishing portable exe (dotnet publish -c Release)"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found on PATH. Install the .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
}

Push-Location $projectDir
try {
    dotnet publish -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $exePath)) {
    throw "Expected publish output not found at: $exePath"
}

Write-Host "Built: $exePath" -ForegroundColor Green

# --- 2. Installer ------------------------------------------------------------

if ($SkipInstaller) {
    Write-Step "Skipping installer (-SkipInstaller)"
    exit 0
}

Write-Step "Building installer (Inno Setup)"

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if ($iscc) {
    $isccPath = $iscc.Source
}
else {
    # Hardcoded rather than built from $env:ProgramFiles(x86) -- the parens in
    # that variable name don't interpolate safely inside a string.
    $candidates = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )
    $isccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $isccPath) {
    Write-Warning "Inno Setup's ISCC.exe was not found (checked PATH and the default install locations)."
    Write-Warning "Install Inno Setup 6 (https://jrsoftware.org/isinfo.php) or re-run with -SkipInstaller to build just the exe."
    exit 1
}

& $isccPath $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $installerOutput)) {
    throw "Expected installer output not found at: $installerOutput"
}

Write-Host "Built: $installerOutput" -ForegroundColor Green

Write-Step "Done"
Write-Host "  Portable exe: $exePath"
Write-Host "  Installer:    $installerOutput"
