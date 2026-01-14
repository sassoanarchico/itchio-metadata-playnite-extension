# Build script for itch.io Metadata Provider

param(
    [string]$Configuration = "Release",
    [switch]$CreatePackage
)

Write-Host "Building itch.io Metadata Provider..." -ForegroundColor Cyan

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore packages!" -ForegroundColor Red
    exit 1
}

# Build
Write-Host "Building project ($Configuration)..." -ForegroundColor Yellow
dotnet build -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Create package if requested
if ($CreatePackage) {
    Write-Host "Creating .pext package..." -ForegroundColor Yellow
    
    $outputDir = "bin\$Configuration"
    $packageName = "ItchioMetadata.pext"
    
    # Remove old package if exists
    if (Test-Path $packageName) {
        Remove-Item $packageName
    }
    
    # Create ZIP (which is a .pext file)
    $filesToInclude = @(
        "$outputDir\ItchioMetadata.dll",
        "$outputDir\HtmlAgilityPack.dll",
        "$outputDir\extension.yaml"
    )
    
    # Add icon if exists
    if (Test-Path "$outputDir\icon.png") {
        $filesToInclude += "$outputDir\icon.png"
    }
    elseif (Test-Path "icon.png") {
        Copy-Item "icon.png" "$outputDir\icon.png"
        $filesToInclude += "$outputDir\icon.png"
    }
    
    # Create as .zip first, then rename to .pext (PowerShell doesn't support .pext extension directly)
    $tempZip = "ItchioMetadata.zip"
    Compress-Archive -Path $filesToInclude -DestinationPath $tempZip -Force
    
    # Rename to .pext
    if (Test-Path $tempZip) {
        Move-Item -Path $tempZip -Destination $packageName -Force
    }
    
    if (Test-Path $packageName) {
        Write-Host "Package created: $packageName" -ForegroundColor Green
    }
    else {
        Write-Host "Failed to create package!" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Cyan
Write-Host ""
Write-Host "To install manually, copy the contents of 'bin\$Configuration' to:" -ForegroundColor White
Write-Host "  %AppData%\Playnite\Extensions\ItchioMetadata\" -ForegroundColor Gray
