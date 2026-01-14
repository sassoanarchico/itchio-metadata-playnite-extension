# Installation script for itch.io Metadata Provider

param(
    [string]$Configuration = "Release"
)

$extensionName = "ItchioMetadata"
$playniteExtensionsPath = "$env:APPDATA\Playnite\Extensions\$extensionName"
$sourcePath = "bin\$Configuration"

Write-Host "Installing itch.io Metadata Provider to Playnite..." -ForegroundColor Cyan

# Check if source exists
if (-not (Test-Path $sourcePath)) {
    Write-Host "Build output not found at $sourcePath" -ForegroundColor Red
    Write-Host "Please run 'dotnet build -c $Configuration' first" -ForegroundColor Yellow
    exit 1
}

# Check if Playnite extensions folder exists
$playniteBasePath = "$env:APPDATA\Playnite"
if (-not (Test-Path $playniteBasePath)) {
    Write-Host "Playnite installation not found at $playniteBasePath" -ForegroundColor Red
    Write-Host "Please make sure Playnite is installed" -ForegroundColor Yellow
    exit 1
}

# Create extensions directory if it doesn't exist
$extensionsBasePath = "$playniteBasePath\Extensions"
if (-not (Test-Path $extensionsBasePath)) {
    New-Item -ItemType Directory -Path $extensionsBasePath | Out-Null
}

# Remove old installation if exists
if (Test-Path $playniteExtensionsPath) {
    Write-Host "Removing old installation..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $playniteExtensionsPath
}

# Create new directory
New-Item -ItemType Directory -Path $playniteExtensionsPath | Out-Null

# Copy files
Write-Host "Copying files..." -ForegroundColor Yellow

$filesToCopy = @(
    "ItchioMetadata.dll",
    "HtmlAgilityPack.dll",
    "extension.yaml"
)

foreach ($file in $filesToCopy) {
    $sourceFile = "$sourcePath\$file"
    if (Test-Path $sourceFile) {
        Copy-Item $sourceFile $playniteExtensionsPath
        Write-Host "  Copied: $file" -ForegroundColor Gray
    }
    else {
        Write-Host "  Warning: $file not found" -ForegroundColor Yellow
    }
}

# Copy icon if exists
if (Test-Path "$sourcePath\icon.png") {
    Copy-Item "$sourcePath\icon.png" $playniteExtensionsPath
    Write-Host "  Copied: icon.png" -ForegroundColor Gray
}
elseif (Test-Path "icon.png") {
    Copy-Item "icon.png" $playniteExtensionsPath
    Write-Host "  Copied: icon.png" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "Installed to: $playniteExtensionsPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Please restart Playnite to load the extension." -ForegroundColor Cyan
