# WinGet Publishing Guide (PowerTranslate)

This guide follows Microsoft Command Palette publishing guidance:
https://learn.microsoft.com/en-us/windows/powertoys/command-palette/publish-extension

## Current Repo State

- Extension package identity: `lamteteeow.PowerTranslate`
- Extension CLSID: `fd4bd242-c3f8-47bb-89f0-5a6f7f14aecf`
- Store release flow currently publishes `.msix` artifacts.
- WinGet requires installer URLs (typically `.exe`) hosted in GitHub releases.

## Important WinGet Notes

- First WinGet submission is manual and interactive.
- Command Palette discovery in WinGet requires tag `windows-commandpalette-extension`.
- If using Windows App SDK, include runtime dependency in installer manifest.

## Prerequisites

1. Install GitHub CLI:

```powershell
gh --version
```

2. Install wingetcreate:

```powershell
winget install Microsoft.WingetCreate
wingetcreate --version
```

3. Install Inno Setup 6 (for building EXE installer payloads).

4. Ensure .NET 9 SDK is available:

```powershell
dotnet --version
```

## Build EXE Installers for Release Assets

WinGet entries should point to versioned installer assets in GitHub releases.

Recommended output naming:

- `PowerTranslateExtension-Setup-<version>-x64.exe`
- `PowerTranslateExtension-Setup-<version>-arm64.exe`

If ARM64 is not ready yet, you can submit x64 first and add ARM64 in a later update.

## First Submission (Manual)

1. Create a GitHub release that includes installer URL(s).

2. Run `wingetcreate new` with release asset URL(s):

```powershell
wingetcreate new "<x64-installer-url>" "<arm64-installer-url>"
```

3. Accept inferred metadata unless you need overrides.

4. Submit when prompted.

`wingetcreate` will fork `microsoft/winget-pkgs`, create a branch, and open a PR.

## Required Manifest Fields

After generation, verify these before/after PR submission:

1. In each `.locale.*.yaml` file:

```yaml
Tags:
- windows-commandpalette-extension
```

2. In `.installer.yaml` (if applicable for your build/runtime):

```yaml
Dependencies:
  PackageDependencies:
  - PackageIdentifier: Microsoft.WindowsAppRuntime.#.#
```

Replace `#.#` with the runtime major/minor your installer requires.

## Updating Existing WinGet Package

After the first PR is merged, publish updates by version:

```powershell
wingetcreate update lamteteeow.PowerTranslate --version <version> --urls "<x64-url>|x64" "<arm64-url>|arm64" --submit
```

## Validation Checklist

- Installer URLs are public and downloadable.
- Checksums are stable for uploaded release assets.
- `PackageVersion` matches release version.
- Command Palette tag is present.
- Dependency metadata is correct.

## Troubleshooting

- If `wingetcreate` fails auth, run `gh auth login` first.
- If PR validation fails, inspect reviewer comments and update the generated manifests.
- If only one architecture is available, submit that architecture and include roadmap notes in PR discussion.
