# Local Installation Guide (PowerTranslate 1.1.1.0)

## Goal

Install PowerTranslate on this machine from local build artifacts.

## Prerequisites

- Run commands from repository root.
- Use PowerShell.

## 1) Verify artifacts

```powershell
Test-Path .\PowerTranslateExtension\AppPackages\PowerTranslateExtension_1.1.1.0_x64_Test\PowerTranslateExtension_1.1.1.0_x64.cer
Test-Path .\PowerTranslateExtension\AppPackages\PowerTranslateExtension_1.1.1.0_x64_Test\PowerTranslateExtension_1.1.1.0_x64.msix
```

## 2) Import signing certificate

```powershell
Import-Certificate -FilePath ".\PowerTranslateExtension\AppPackages\PowerTranslateExtension_1.1.1.0_x64_Test\PowerTranslateExtension_1.1.1.0_x64.cer" -CertStoreLocation "Cert:\CurrentUser\TrustedPeople"
```

## 3) Close processes that may lock the package

```powershell
Get-Process | Where-Object { $_.ProcessName -in @('PowerTranslateExtension','PowerToys.PowerLauncher','PowerToys','PowerToys.CmdPal.UI') } | Stop-Process -Force -ErrorAction SilentlyContinue
```

## 4) Install package

```powershell
Add-AppxPackage -Path ".\PowerTranslateExtension\AppPackages\PowerTranslateExtension_1.1.1.0_x64_Test\PowerTranslateExtension_1.1.1.0_x64.msix" -ForceUpdateFromAnyVersion
```

## 5) Verify installation

```powershell
Get-AppxPackage -Name "*PowerTranslate*" | Select-Object Name,PackageFullName,Version,Architecture
```

## 6) Refresh extension in PowerToys

1. Open Command Palette.
2. Run `Reload Command Palette extensions`.

## Troubleshooting

- `0x80073D02` (resources in use): close PowerToys/PowerTranslate processes, then run install again.
- `0x800B0100` (no signature): re-run the signed Release build command and verify `_x64_Test` contains both `.msix` and `.cer`.
- `0x80073CFB` (same identity, different contents): uninstall existing `lamteteeow.PowerTranslate` first, then install the Release `.msix` again.
