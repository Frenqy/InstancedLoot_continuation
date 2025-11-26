# Build and Package Script for InstancedLoot
# This script automates the release build and packaging process

param(
    [string]$Configuration = "Release"
)

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "=== InstancedLoot Release Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean/Create Release directory
Write-Host "Step 1: Cleaning Release directory..." -ForegroundColor Yellow
$releaseDir = Join-Path $scriptDir "Release"
if (Test-Path $releaseDir) {
    Remove-Item -Path $releaseDir -Recurse -Force
    Write-Host "  - Cleaned existing Release directory" -ForegroundColor Green
}
New-Item -Path $releaseDir -ItemType Directory -Force | Out-Null
Write-Host "  - Created Release directory" -ForegroundColor Green
Write-Host ""

# Step 2: Copy required files to Release directory
Write-Host "Step 2: Copying files to Release directory..." -ForegroundColor Yellow
$filesToCopy = @("CHANGELOG.md", "icon.png", "manifest.json", "README.md")
foreach ($file in $filesToCopy) {
    $sourcePath = Join-Path $scriptDir $file
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $releaseDir -Force
        Write-Host "  - Copied $file" -ForegroundColor Green
    } else {
        Write-Warning "  - File not found: $file"
    }
}
Write-Host ""

# Step 3: Build the project in Release mode
Write-Host "Step 3: Building project in $Configuration mode..." -ForegroundColor Yellow
$projectFile = Join-Path $scriptDir "InstancedLoot\InstancedLoot.csproj"
$buildOutput = dotnet build $projectFile --configuration $Configuration 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  - Build succeeded" -ForegroundColor Green
} else {
    Write-Error "Build failed!"
    Write-Host $buildOutput -ForegroundColor Red
    exit 1
}
Write-Host ""

# Copy DLL to Release/InstancedLoot directory
Write-Host "Step 3b: Copying DLL to Release directory..." -ForegroundColor Yellow
$instancedLootDir = Join-Path $releaseDir "InstancedLoot"
New-Item -Path $instancedLootDir -ItemType Directory -Force | Out-Null

$dllPath = Join-Path $scriptDir "InstancedLoot\bin\$Configuration\netstandard2.1\InstancedLoot.dll"
if (Test-Path $dllPath) {
    Copy-Item -Path $dllPath -Destination $instancedLootDir -Force
    Write-Host "  - Copied InstancedLoot.dll" -ForegroundColor Green
} else {
    Write-Error "DLL not found at: $dllPath"
    exit 1
}
Write-Host ""

# Step 4: Read version number from manifest.json
Write-Host "Step 4: Reading version from manifest.json..." -ForegroundColor Yellow
$manifestPath = Join-Path $scriptDir "manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$versionNumber = $manifest.version_number
Write-Host "  - Version: $versionNumber" -ForegroundColor Green
Write-Host ""

# Step 5: Create ZIP archive
Write-Host "Step 5: Creating ZIP archive..." -ForegroundColor Yellow
$zipFileName = "Nicebowl-InstancedLoot-$versionNumber.zip"
$zipPath = Join-Path $scriptDir $zipFileName

# Remove existing ZIP if it exists
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
    Write-Host "  - Removed existing ZIP file" -ForegroundColor Green
}

# Create ZIP archive
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipPath -Force
Write-Host "  - Created $zipFileName" -ForegroundColor Green
Write-Host ""

Write-Host "=== Build and Package Complete ===" -ForegroundColor Cyan
Write-Host "Output: $zipPath" -ForegroundColor Green
