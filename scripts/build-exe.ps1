# TEMPLATE: PowerShell Build Script for Command Palette Extensions
#
# To use this template for a new extension:
# 1. Copy this file to your extension's project folder as "build-exe.ps1"
# 2. Update in param():
#   - EXTENSION_NAME with your extension name (e.g., CmdPalMyExtension)
#   - VERSION with your extension version (e.g., 0.0.1.0)

param(
    [string]$ExtensionName = "PowerTranslateExtension",  # Change to your extension name
    [string]$Configuration = "Release",
    [string]$Version = "",  # Optional. Auto-detected from AppxPackageVersion when omitted.
    [string[]]$Platforms = @("x64")
)

$ErrorActionPreference = "Stop"

Write-Host "Building $ExtensionName EXE installer..." -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor Yellow
Write-Host "Platforms: $($Platforms -join ', ')" -ForegroundColor Yellow

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = $ScriptDir
if (-not (Test-Path "$ProjectDir\$ExtensionName.csproj")) {
    $ProjectDir = Join-Path (Split-Path -Parent $ScriptDir) "PowerTranslateExtension"
}
$ProjectFile = "$ProjectDir\$ExtensionName.csproj"

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content -Path $ProjectFile
    $Version = $projectXml.Project.PropertyGroup.AppxPackageVersion | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw "Unable to detect AppxPackageVersion from $ProjectFile. Pass -Version explicitly."
    }
}

$invalidPlatforms = $Platforms | Where-Object { $_ -ne "x64" }
if ($invalidPlatforms) {
    throw "This project is x64-only. Invalid platform(s): $($invalidPlatforms -join ', ')"
}

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "$ProjectDir\bin") {
    Remove-Item -Path "$ProjectDir\bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "$ProjectDir\obj") {
    Remove-Item -Path "$ProjectDir\obj" -Recurse -Force -ErrorAction SilentlyContinue
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore $ProjectFile -p:GenerateAppxPackageOnBuild=false -p:WindowsPackageType=None

# Build for each platform
foreach ($Platform in $Platforms) {
    Write-Host "`n=== Building $Platform ===" -ForegroundColor Cyan

    # Build and publish
    Write-Host "Building and publishing $Platform application..." -ForegroundColor Yellow
    dotnet publish $ProjectFile --configuration $Configuration --runtime "win-$Platform" --self-contained true --output "$ProjectDir\bin\$Configuration\win-$Platform\publish" -p:GenerateAppxPackageOnBuild=false -p:WindowsPackageType=None

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Build failed for $Platform with exit code: $LASTEXITCODE"
        continue
    }

    # Check if files were published
    $publishDir = "$ProjectDir\bin\$Configuration\win-$Platform\publish"
    $fileCount = (Get-ChildItem -Path $publishDir -Recurse -File).Count
    Write-Host "Published $fileCount files to $publishDir" -ForegroundColor Green

    # Create platform-specific setup script
    Write-Host "Creating installer script for $Platform..." -ForegroundColor Yellow
    $setupTemplate = Get-Content "$ProjectDir\setup-template.iss" -Raw

    # Update version
    $setupScript = $setupTemplate -replace '#define AppVersion ".*"', "#define AppVersion `"$Version`""

    # Update output filename to include platform suffix
    $setupScript = $setupScript -replace 'OutputBaseFilename=(.*?)\{#AppVersion\}', "OutputBaseFilename=`$1{#AppVersion}-$Platform"

    # Update source path for the platform
    $setupScript = $setupScript -replace 'Source: "bin\\Release\\win-x64\\publish', "Source: `"bin\$Configuration\win-$Platform\publish"

    # Add architecture settings after [Setup] section
    if ($Platform -eq "arm64") {
        $setupScript = $setupScript -replace '(\[Setup\][^\[]*)(MinVersion=)', "`$1ArchitecturesAllowed=arm64`r`nArchitecturesInstallIn64BitMode=arm64`r`n`$2"
    }
    else {
        $setupScript = $setupScript -replace '(\[Setup\][^\[]*)(MinVersion=)', "`$1ArchitecturesAllowed=x64compatible`r`nArchitecturesInstallIn64BitMode=x64compatible`r`n`$2"
    }

    $setupScript | Out-File -FilePath "$ProjectDir\setup-$Platform.iss" -Encoding UTF8

    # Create installer with Inno Setup
    Write-Host "Creating $Platform installer with Inno Setup..." -ForegroundColor Yellow
    $InnoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe"
    if (-not (Test-Path $InnoSetupPath)) {
        $InnoSetupPath = "${env:ProgramFiles}\Inno Setup 6\iscc.exe"
    }
    if (-not (Test-Path $InnoSetupPath)) {
        $InnoSetupPath = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    }
    if (-not (Test-Path $InnoSetupPath)) {
        $isccCmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
        if ($isccCmd) {
            $InnoSetupPath = $isccCmd.Source
        }
    }

    if (Test-Path $InnoSetupPath) {
        & $InnoSetupPath "$ProjectDir\setup-$Platform.iss"

        if ($LASTEXITCODE -eq 0) {
            $installer = Get-ChildItem "$ProjectDir\bin\$Configuration\installer\*-$Platform.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($installer) {
                $sizeMB = [math]::Round($installer.Length / 1MB, 2)
                Write-Host "Created $Platform installer: $($installer.Name) ($sizeMB MB)" -ForegroundColor Green
            }
            else {
                Write-Warning "Installer file not found for $Platform"
            }
        }
        else {
            Write-Warning "Inno Setup failed for $Platform with exit code: $LASTEXITCODE"
        }
    }
    else {
        Write-Warning "Inno Setup not found at expected locations"
    }
}

Write-Host "`nBuild completed successfully." -ForegroundColor Green
