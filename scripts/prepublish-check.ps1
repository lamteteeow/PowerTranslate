param(
    [string]$AssetsPath = "$PSScriptRoot\..\PowerTranslateExtension\Assets",
    [string]$ManifestPath = "$PSScriptRoot\..\PowerTranslateExtension\Package.appxmanifest"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$requiredAssets = @(
    @{ Name = "Square44x44Logo.png"; Width = 44; Height = 44; MinBytes = 1000 },
    @{ Name = "Square44x44Logo.scale-200.png"; Width = 88; Height = 88; MinBytes = 1000 },
    @{ Name = "Square44x44Logo.targetsize-24_altform-unplated.png"; Width = 24; Height = 24; MinBytes = 1000 },
    @{ Name = "Square150x150Logo.png"; Width = 150; Height = 150; MinBytes = 2000 },
    @{ Name = "Square150x150Logo.scale-200.png"; Width = 300; Height = 300; MinBytes = 4000 },
    @{ Name = "Wide310x150Logo.png"; Width = 310; Height = 150; MinBytes = 3000 },
    @{ Name = "Wide310x150Logo.scale-200.png"; Width = 620; Height = 300; MinBytes = 5000 },
    @{ Name = "SplashScreen.png"; Width = 620; Height = 300; MinBytes = 5000 },
    @{ Name = "SplashScreen.scale-200.png"; Width = 1240; Height = 600; MinBytes = 8000 },
    @{ Name = "StoreLogo.png"; Width = 50; Height = 50; MinBytes = 1000 },
    @{ Name = "LockScreenLogo.scale-200.png"; Width = 48; Height = 48; MinBytes = 1000 }
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($asset in $requiredAssets) {
    $path = Join-Path $AssetsPath $asset.Name
    if (-not (Test-Path $path)) {
        $failures.Add("Missing required asset: $($asset.Name)")
        continue
    }

    $file = Get-Item $path
    if ($file.Length -lt $asset.MinBytes) {
        $failures.Add("Asset appears placeholder or too small: $($asset.Name) ($($file.Length) bytes)")
    }

    $img = $null
    try {
        $img = [System.Drawing.Image]::FromFile($path)
        if ($img.Width -ne $asset.Width -or $img.Height -ne $asset.Height) {
            $failures.Add("Unexpected dimensions for $($asset.Name): got $($img.Width)x$($img.Height), expected $($asset.Width)x$($asset.Height)")
        }
    }
    catch {
        $failures.Add("Unable to read image file: $($asset.Name)")
    }
    finally {
        if ($null -ne $img) {
            $img.Dispose()
        }
    }
}

if (-not (Test-Path $ManifestPath)) {
    $failures.Add("Manifest not found: $ManifestPath")
}
else {
    try {
        [xml]$manifestXml = Get-Content -Path $ManifestPath

        $identityVersion = $manifestXml.Package.Identity.Version
        if ([string]::IsNullOrWhiteSpace($identityVersion)) {
            $failures.Add("Manifest Identity.Version is missing")
        }
        elseif ($identityVersion -notmatch '^\d+\.\d+\.\d+\.0$') {
            $failures.Add("Manifest Identity.Version must use revision 0 for Store submissions. Current: $identityVersion")
        }

        $comServer = $manifestXml.Package.Applications.Application.Extensions.Extension |
        Where-Object { $_.Category -eq 'windows.comServer' } |
        Select-Object -First 1

        if (-not $comServer) {
            $failures.Add("Missing windows.comServer extension in manifest")
        }

        $appExtension = $manifestXml.Package.Applications.Application.Extensions.Extension |
        Where-Object { $_.Category -eq 'windows.appExtension' } |
        Select-Object -First 1

        if (-not $appExtension) {
            $failures.Add("Missing windows.appExtension registration in manifest")
        }
        else {
            $appExtName = $appExtension.AppExtension.Name
            if ($appExtName -ne 'com.microsoft.commandpalette') {
                $failures.Add("AppExtension Name must be 'com.microsoft.commandpalette'. Current: $appExtName")
            }

            $classId = $appExtension.AppExtension.Properties.CmdPalProvider.Activation.CreateInstance.ClassId
            if ([string]::IsNullOrWhiteSpace($classId)) {
                $failures.Add("CmdPalProvider Activation.CreateInstance ClassId is missing")
            }
        }
    }
    catch {
        $failures.Add("Unable to parse manifest file: $ManifestPath")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Pre-publish check failed:" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host " - $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Pre-publish check passed. Required branding assets are present and validated." -ForegroundColor Green
