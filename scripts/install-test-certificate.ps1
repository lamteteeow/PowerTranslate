param(
    [string]$SubjectName = "CN=4B22C9A6-04CC-4DE0-A7BE-ED219F6F2DA2"
)

$ErrorActionPreference = "Stop"

Write-Host "Installing test certificate to system Root store..." -ForegroundColor Cyan

# Get the certificate from CurrentUser\My
$cert = Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object { $_.Subject -eq $SubjectName }

if (-not $cert) {
    Write-Error "Certificate not found: $SubjectName"
    exit 1
}

Write-Host "Found certificate: $($cert.Thumbprint)" -ForegroundColor Green

# Export and reimport to system Root store
try {
    $tempPath = Join-Path $env:TEMP "PowerTranslateCert_Install.cer"
    
    # Export the certificate
    Export-Certificate -Cert $cert -FilePath $tempPath -Type CERT | Out-Null
    Write-Host "Exported certificate to $tempPath" -ForegroundColor Green
    
    # Import to LocalMachine\Root using certutil so MSIX trust is recognized reliably
    certutil -addstore -f "Root" $tempPath | Out-Null
    Write-Host "Installed certificate to LocalMachine\Root" -ForegroundColor Green
    
    # Clean up
    Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
    
    Write-Host "Certificate successfully installed!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to install certificate: $_"
    exit 1
}
