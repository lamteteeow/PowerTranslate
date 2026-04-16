param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("x64")]
    [string]$Platform = "x64",

    [string]$CertificateThumbprint,

    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "PowerTranslateExtension\PowerTranslateExtension.csproj"
$manifestPath = Join-Path $repoRoot "PowerTranslateExtension\Package.appxmanifest"
$projectDir = Split-Path -Parent $projectPath

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not (Test-Path $manifestPath)) {
    throw "Manifest file not found: $manifestPath"
}

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
    [xml]$manifestXml = Get-Content -Path $manifestPath
    $publisher = $manifestXml.Package.Identity.Publisher

    if ([string]::IsNullOrWhiteSpace($publisher)) {
        throw "Publisher is missing in manifest: $manifestPath"
    }

    $matchingCert = Get-ChildItem -Path Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $publisher -and $_.HasPrivateKey } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $matchingCert) {
        throw "No certificate with private key found in Cert:\CurrentUser\My for publisher '$publisher'."
    }

    $CertificateThumbprint = $matchingCert.Thumbprint
}

[xml]$manifestXml = Get-Content -Path $manifestPath
$packageIdentityName = $manifestXml.Package.Identity.Name

if ([string]::IsNullOrWhiteSpace($packageIdentityName)) {
    throw "Package identity name is missing in manifest: $manifestPath"
}

Write-Host "Publishing $Configuration|$Platform ..." -ForegroundColor Cyan
Write-Host "Using certificate thumbprint $CertificateThumbprint for MSIX signing." -ForegroundColor Cyan
dotnet msbuild $projectPath /restore /p:Configuration=$Configuration /p:Platform=$Platform /p:RuntimeIdentifier=win-$Platform /p:SelfContained=true /p:GenerateAppxPackageOnBuild=true /p:WindowsPackageType=MSIX /p:AppxPackageSigningEnabled=true /p:PackageCertificateThumbprint=$CertificateThumbprint /v:m

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

$appPackagesRoot = Join-Path $projectDir "AppPackages"
if (-not (Test-Path $appPackagesRoot)) {
    throw "AppPackages folder not found: $appPackagesRoot"
}

$folderPattern = if ($Configuration -eq "Release") {
    "*_" + $Platform + "_Test"
}
else {
    "*_" + $Platform + "_" + $Configuration + "_Test"
}
$candidateFolder = Get-ChildItem -Path $appPackagesRoot -Directory |
    Where-Object { $_.Name -like $folderPattern } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $candidateFolder) {
    throw "No package folder found under $appPackagesRoot matching $folderPattern"
}

$addDevPackageScript = Join-Path $candidateFolder.FullName "Add-AppDevPackage.ps1"
if (-not (Test-Path $addDevPackageScript)) {
    throw "Installer script missing: $addDevPackageScript"
}

if ($SkipInstall) {
    Write-Host "Package build finished. Install was skipped by configuration." -ForegroundColor Green
    Write-Host "Output folder: $($candidateFolder.FullName)" -ForegroundColor Green
    return
}

$legacyPackageNames = @(
    $packageIdentityName,
    "PowerTranslateExtension"
)

$existingPackages = Get-AppxPackage -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in $legacyPackageNames }

if ($existingPackages) {
    Write-Host "Removing existing installed package(s) for PowerTranslate to avoid duplicate installs ..." -ForegroundColor Yellow
    foreach ($pkg in $existingPackages) {
        Write-Host "Removing $($pkg.PackageFullName)" -ForegroundColor Yellow
        Remove-AppxPackage -Package $pkg.PackageFullName
    }
}

Write-Host "Installing package from $($candidateFolder.FullName) ..." -ForegroundColor Cyan
& $addDevPackageScript -Force

if (-not $?) {
    throw "Add-AppDevPackage.ps1 failed. Check the error details above."
}

if ($LASTEXITCODE -ne 0) {
    throw "Add-AppDevPackage.ps1 exited with code $LASTEXITCODE."
}

Write-Host "Deployment finished. If Command Palette is open, restart it to pick up the updated extension." -ForegroundColor Green
