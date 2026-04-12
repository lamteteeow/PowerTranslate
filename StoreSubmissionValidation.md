# Store Submission Validation (x64-only)

This checklist maps Microsoft Command Palette publish guidance to current repository evidence and submission scope.

## Scope

- Submission architecture: x64 only
- Submission artifact type: `.msixupload`
- ARM64: intentionally excluded from this submission cycle
- Windows 10 S: not supported for this full-trust desktop extension model

## Requirement Checklist

| Requirement | Source | Status | Evidence |
| --- | --- | --- | --- |
| Partner Center identity values are applied in manifest | Publish extension guide: Prepare extension | PASS | `Package/Identity` in `PowerTranslateExtension/Package.appxmanifest` |
| Manifest version uses Store-safe revision `.0` | App package requirements / Store validation | PASS | `Version="1.1.1.0"` in `PowerTranslateExtension/Package.appxmanifest` |
| Project Appx identity values are set in csproj | Publish extension guide: Prepare extension | PASS | `AppxPackageIdentityName`, `AppxPackagePublisher`, `AppxPackageVersion` in `PowerTranslateExtension/PowerTranslateExtension.csproj` |
| App extension registration exists for Command Palette | Extensibility overview | PASS | `windows.appExtension` with `Name="com.microsoft.commandpalette"` in `PowerTranslateExtension/Package.appxmanifest` |
| COM server registration exists | Extensibility overview | PASS | `windows.comServer` in `PowerTranslateExtension/Package.appxmanifest` |
| CmdPalProvider activation CLSID exists and matches COM class usage | Extensibility overview | PASS | `CreateInstance ClassId` in `PowerTranslateExtension/Package.appxmanifest` |
| Required base assets exist and match expected dimensions | Publish extension guide: icon prerequisites | PASS | `scripts/prepublish-check.ps1` passes |
| Store upload artifact generated successfully | Upload app packages guidance | PASS | `PowerTranslateExtension/AppPackages/PowerTranslateExtension_1.1.1.0_x64.msixupload` |
| GitHub release sideload artifacts use signed Release package | Release distribution validation | PASS | `PowerTranslateExtension_1.1.1.0_x64.msix` and `.cer` attached to `v1.1.1.0` |
| GitHub release artifact digests match local signed Release outputs | Integrity verification | PASS | msix `sha256:48764bee768a9878558e171a6b8008e865e499b1c28e325045986e312c9a7f2d`, cer `sha256:0a3698f9edeb68cec64a703b75c2299533c1ed9df1f485ea4ec91d6b54d69aea` |
| x64-only support explicitly documented for reviewers | Submission/testing guidance best practice | PASS | `TesterInstructions.md` package scope and install steps |
| Startup path from COM activation does not crash in Release x64 smoke test | Certification readiness requirement | PASS | `%LocalAppData%/PowerTranslateExtension/startup.log` contains repeated `COM server started successfully.` entries |

## Mandatory Commands Run

1. Prepublish validation:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\prepublish-check.ps1
```

1. Build submission artifact (x64 StoreUpload):

```powershell
dotnet msbuild .\PowerTranslateExtension\PowerTranslateExtension.csproj /restore /p:Configuration=Release /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true /p:UapAppxPackageBuildMode=StoreUpload /p:AppxPackageSigningEnabled=true /p:PackageCertificateThumbprint=<thumbprint> /v:m
```

1. COM startup smoke test:

```powershell
.\PowerTranslateExtension\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\PowerTranslateExtension.exe -RegisterProcessAsComServer
```

## Upload Artifact

Use this file in Partner Center:

- `PowerTranslateExtension/AppPackages/PowerTranslateExtension_1.1.1.0_x64.msixupload`

## GitHub Release Artifacts

For local sideload installation, the `v1.1.1.0` GitHub release includes:

- `PowerTranslateExtension_1.1.1.0_x64.msix`
- `PowerTranslateExtension_1.1.1.0_x64.cer`

## Notes

- The command palette publish page shows a dual-arch example, but x64-only submission is acceptable when scope is explicitly declared and matching package artifacts/docs are provided.
- Ensure Additional Testing Information in Partner Center states:
  - PowerToys + Command Palette prerequisite
  - x64-only support for this submission
  - Windows 10 S is not supported
  - steps to launch `Translate text` and `Configure DeepL API key`
