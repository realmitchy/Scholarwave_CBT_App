# Builds a portable dist folder you can copy to another Windows PC (USB, zip, etc.).
# Usage:  .\publish.ps1              # smaller; target needs .NET 10 Desktop Runtime
#         .\publish.ps1 -SelfContained   # larger; runs without .NET on target PC
# Avoids leftover build junk, debug symbols, and reparse points that can block copying.
param([switch]$SelfContained)
$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$projectPath = Join-Path $projectRoot "Scholarwave CBT App.csproj"
$outputDir = Join-Path $projectRoot "dist"

function Copy-TreePortable {
    param(
        [string]$Source,
        [string]$Destination
    )
    if (-not (Test-Path $Source)) {
        Write-Host "Skip (missing): $Source" -ForegroundColor Yellow
        return
    }
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    # /COPY:DAT copies data, attributes, timestamps only (no ACLs/reparse points).
    $null = robocopy $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:2 /W:2 /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed for $Source (exit $LASTEXITCODE)"
    }
}

Write-Host "Publishing Scholarwave CBT App to $outputDir ..." -ForegroundColor Cyan

$running = Get-Process -Name "Scholarwave CBT App" -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "Close 'Scholarwave CBT App' if it is running from dist, then run publish again." -ForegroundColor Red
    exit 1
}

if (Test-Path $outputDir) {
    Write-Host "Cleaning previous dist ..." -ForegroundColor DarkGray
    Remove-Item $outputDir -Recurse -Force
}

Push-Location $projectRoot
try {
    # Framework-dependent: small dist, easy to copy. Target PC needs .NET 10 Desktop Runtime.
    # Pass -SelfContained when you have enough disk (~150 MB free in NuGet cache) for a PC without .NET installed.
    $publishArgs = @(
        "publish", $projectPath,
        "-c", "Release",
        "-p:PublishSingleFile=true",
        "-p:EnableCompressionInSingleFile=true",
        "-p:DebugType=none",
        "-p:DebugSymbols=false",
        "-o", $outputDir
    )
    if ($SelfContained) {
        $publishArgs += @("-r", "win-x64", "--self-contained", "true",
            "-p:IncludeNativeLibrariesForSelfExtract=true")
        Write-Host "Mode: self-contained (no .NET install required on target PC)" -ForegroundColor Cyan
    } else {
        Write-Host "Mode: framework-dependent (install .NET 10 Desktop Runtime on target PC)" -ForegroundColor Cyan
    }
    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit $LASTEXITCODE)"
    }

    Write-Host "Copying runtime data (Questions, Images, Assets) ..." -ForegroundColor Cyan
    Copy-TreePortable (Join-Path $projectRoot "Questions") (Join-Path $outputDir "Questions")
    Copy-TreePortable (Join-Path $projectRoot "Images") (Join-Path $outputDir "Images")
    Copy-TreePortable (Join-Path $projectRoot "Assets") (Join-Path $outputDir "Assets")

    $envFile = Join-Path $projectRoot ".env"
    if (Test-Path $envFile) {
        Copy-Item $envFile (Join-Path $outputDir ".env") -Force
        Write-Host "Copied .env" -ForegroundColor Green
    } else {
        Write-Host "Warning: .env not found. Teacher login may fail until you add it to dist." -ForegroundColor Yellow
    }

    $exe = Join-Path $outputDir "Scholarwave CBT App.exe"
    if (-not (Test-Path $exe)) {
        throw "Expected executable was not produced: $exe"
    }

    $sizeMb = [math]::Round((Get-ChildItem $outputDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
    Write-Host "Build success. Dist is ready ($sizeMb MB total)." -ForegroundColor Green
    Write-Host "Copy the entire 'dist' folder to another PC and run 'Scholarwave CBT App.exe' from inside it." -ForegroundColor Green
}
finally {
    Pop-Location
}
