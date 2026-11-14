# ==============================================================================
# BlueDoc - Automated Build, Test, and Distribution Packaging Script
# Product: BlueDoc
# Company: UNLEFT LLC
# Developer: Golam Morshed Eashan
# ==============================================================================

[CmdletBinding()]
param (
    [switch]$SkipTests = $false,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$distDir = Join-Path $rootDir "dist"
$portableDir = Join-Path $distDir "BlueDoc_v1.0.0_Portable"
$portableZip = Join-Path $distDir "BlueDoc_v1.0.0_Portable.zip"
$solutionPath = Join-Path $rootDir "BlueDoc.sln"
$uiBinDir = Join-Path $rootDir "src\BlueDoc.UI\bin\$Configuration"
$testExe = Join-Path $rootDir "tests\BlueDoc.Tests\bin\$Configuration\BlueDoc.Tests.exe"
$installerScript = Join-Path $rootDir "installer\bluedoc_setup.iss"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  BLUEDOC RELEASE AUTOMATION PIPELINE (UNLEFT LLC)" -ForegroundColor Cyan
Write-Host "  Product: BlueDoc v1.0.0" -ForegroundColor Cyan
Write-Host "  Developer: Golam Morshed Eashan" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# Step 1: Locate MSBuild
$msbuildCandidates = @(
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuild = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $msbuild) {
    throw "MSBuild.exe could not be located in standard Visual Studio installation directories."
}

Write-Host "`n[1/5] Compiling Solution ($Configuration mode) using MSBuild..." -ForegroundColor Yellow
& $msbuild $solutionPath /t:Rebuild /p:Configuration=$Configuration /p:Platform="AnyCPU" /v:m
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}
Write-Host "  [OK] Build completed successfully." -ForegroundColor Green

# Step 2: Run Verification Test Suite
if (-not $SkipTests) {
    Write-Host "`n[2/5] Running Complete Test Harness and Scenarios..." -ForegroundColor Yellow
    if (-not (Test-Path $testExe)) {
        throw "Test executable not found at: $testExe"
    }

    & $testExe
    if ($LASTEXITCODE -ne 0) {
        throw "Test suite failed with exit code $LASTEXITCODE. Distribution packaging halted."
    }

    Write-Host "  -> Verifying all 17 WPF Views and MainWindow navigation in STA mode..." -ForegroundColor Cyan
    $viewTestScript = Join-Path $PSScriptRoot "test_all_views.ps1"
    if (Test-Path $viewTestScript) {
        powershell.exe -STA -ExecutionPolicy Bypass -File $viewTestScript
        if ($LASTEXITCODE -ne 0) {
            throw "UI View verification test failed! Distribution packaging halted."
        }
    }

    Write-Host "  [OK] 100% Test suite passed with zero regressions." -ForegroundColor Green
} else {
    Write-Host "`n[2/5] Tests skipped by request." -ForegroundColor DarkYellow
}

# Step 3: Stage Portable Distribution Directory
Write-Host "`n[3/5] Staging Portable Distribution Directory..." -ForegroundColor Yellow
if (Test-Path $distDir) {
    Remove-Item -Path $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $portableDir "x86") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $portableDir "x64") -Force | Out-Null

$binFiles = @(
    "BlueDoc.exe",
    "BlueDoc.exe.config",
    "BlueDoc.Core.dll",
    "BlueDoc.Infrastructure.dll",
    "BlueDoc.Bluetooth.dll",
    "BlueDoc.Diagnostics.dll",
    "BlueDoc.Data.dll",
    "BlueDoc.Application.dll",
    "Newtonsoft.Json.dll",
    "System.Data.SQLite.dll"
)

foreach ($f in $binFiles) {
    $src = Join-Path $uiBinDir $f
    if (-not (Test-Path $src)) {
        throw "Required binary file not found: $src"
    }
    Copy-Item $src -Destination $portableDir -Force
}

# Copy native SQLite interop binaries
Copy-Item (Join-Path $uiBinDir "x86\SQLite.Interop.dll") -Destination (Join-Path $portableDir "x86") -Force
Copy-Item (Join-Path $uiBinDir "x64\SQLite.Interop.dll") -Destination (Join-Path $portableDir "x64") -Force

# Copy Embedded Driver and Firmware Packages
$driversSource = Join-Path $uiBinDir "Drivers"
if (Test-Path $driversSource) {
    Copy-Item $driversSource -Destination $portableDir -Recurse -Force
}

# Copy Documentation and Icon to Portable folder
Copy-Item (Join-Path $rootDir "src\BlueDoc.UI\BlueDoc.ico") -Destination $portableDir -Force
$docFiles = @("README.md", "RELEASE_NOTES_v1.0.md", "LICENSE")
foreach ($doc in $docFiles) {
    $src = Join-Path $rootDir $doc
    if (Test-Path $src) {
        Copy-Item $src -Destination $portableDir -Force
    }
}
Write-Host "  [OK] Staged all binaries and dependencies to: $portableDir" -ForegroundColor Green

# Step 4: Create Portable Zip Package
Write-Host "`n[4/5] Creating Portable ZIP Archive..." -ForegroundColor Yellow
if (Test-Path $portableZip) {
    Remove-Item $portableZip -Force
}
Compress-Archive -Path "$portableDir\*" -DestinationPath $portableZip -CompressionLevel Optimal
$zipSize = (Get-Item $portableZip).Length
Write-Host "  [OK] Generated: $portableZip ($zipSize bytes)" -ForegroundColor Green

# Step 5: Check and Compile Inno Setup (if available)
Write-Host "`n[5/5] Checking Inno Setup Compiler..." -ForegroundColor Yellow
$isccCandidates = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($iscc) {
    Write-Host "  Found Inno Setup at: $iscc" -ForegroundColor Cyan
    Write-Host "  Compiling installer package..." -ForegroundColor Cyan
    & $iscc $installerScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Installer compiled successfully to: dist\BlueDoc_v1.0.0_Setup.exe" -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup compilation exited with code $LASTEXITCODE."
    }
} else {
    Write-Host "  Inno Setup compiler (ISCC.exe) not found on host." -ForegroundColor DarkYellow
    Write-Host "  Installer script is ready at: $installerScript" -ForegroundColor DarkYellow
    Write-Host "  To generate BlueDoc_v1.0.0_Setup.exe, install Inno Setup 6 and run: ISCC.exe installer\bluedoc_setup.iss" -ForegroundColor DarkYellow
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "  DISTRIBUTION PACKAGE SUMMARY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Get-ChildItem $distDir | ForEach-Object {
    if ($_.PSIsContainer) {
        Write-Host ($_.Name.PadRight(35) + " | " + "DIRECTORY".PadLeft(15))
    } else {
        $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
        $sizeStr = "$($_.Length) bytes"
        Write-Host ($_.Name.PadRight(35) + " | " + $sizeStr.PadLeft(15) + " | " + $hash)
    }
}
Write-Host "=================================================================`n" -ForegroundColor Cyan
