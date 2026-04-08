param(
    [string]$SubjectName = "CN=4B22C9A6-04CC-4DE0-A7BE-ED219F6F2DA2",
    [int]$ValidityYears = 10
)

$ErrorActionPreference = "Stop"

Write-Host "Generating test certificate for MSIX signing..." -ForegroundColor Cyan
Write-Host "Subject: $SubjectName" -ForegroundColor Cyan

# Check if certificate already exists and remove it
$existingCert = Get-ChildItem -Path "Cert:\CurrentUser\My" | 
    Where-Object { $_.Subject -eq $SubjectName }

if ($existingCert) {
    Write-Host "Removing existing certificate with subject '$SubjectName'..." -ForegroundColor Yellow
    Remove-Item -Path "Cert:\CurrentUser\My\$($existingCert.Thumbprint)" -Force
}

# Create new self-signed certificate valid for the specified years
$cert = New-SelfSignedCertificate `
    -Subject $SubjectName `
    -Type CodeSigningCert `
    -TextExtension "2.5.29.37={text}1.3.6.1.5.5.7.3.3" `
    -FriendlyName "PowerTranslate Test Signing Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears($ValidityYears)

Write-Host "Certificate created successfully!" -ForegroundColor Green
Write-Host "Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green
Write-Host "Valid until: $($cert.NotAfter)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run as Administrator:" -ForegroundColor Cyan
Write-Host "   pwsh -File scripts/install-test-certificate.ps1" -ForegroundColor White
Write-Host "2. Then deploy:" -ForegroundColor Cyan
Write-Host "   pwsh -File scripts/deploy-dev.ps1 -Configuration Release -Platform x64" -ForegroundColor White
