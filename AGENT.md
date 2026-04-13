# Agent Standards

This file defines the working standards followed for this repository.

## Response Style

- Keep responses concise by default.
- Use short, simple wording when possible.
- Avoid extra explanation unless the user asks for it.

## Branch and Commit Workflow

- Work on `dev` for all active development.
- Do not do feature or fix development directly on `main`.
- Keep `dev` up to date with `main` before starting new work when release commits were added to `main`.
- Use `main` only as the final release branch.
- Merge to `main` only for reviewed, release-ready changes.
- Make small, focused commits for each milestone.
- Do not revert unrelated user changes.
- If local and remote diverge, prefer `git pull --rebase` to keep history linear.
- After each major completed phase, push to `origin/dev`.

## Internal Docs Branch Policy

- Keep these files on `dev` and `local-docs-private` only:
  - `StoreSubmissionValidation.md`
  - `TesterInstructions.md`
  - `WINGET_PUBLISHING.md`
- Keep these files on `local-docs-private` only:
  - `local_guide_documentation.md`
- Do not keep the files above on `main`.
- Do not keep local-only files above on `dev`.

## Commit and PR Naming Convention

- Use Conventional Commits for commit subjects.
- Preferred types: `feat`, `fix`, `docs`, `refactor`, `chore`, `test`, `release`.
- Commit format:
  - `<type>: <short summary>`
  - Example: `fix: add base tile assets for Start menu logo`
- Keep one logical change per commit.
- PR titles and descriptions must align with Conventional Commit style and include:
  - What changed
  - Why it changed
  - How it was validated

## Testing Flow (x64-Only Release)

- Architecture scope for current release is x64 only.
- Use debug deploy flow for local validation:
  - `Deploy Extension (Debug x64)` task
- Required manual validation scenarios before release:
  - Missing API key
  - Invalid API key
  - No internet connection
  - Language persistence across reloads
  - Multiple language pairs including AUTO source detection
  - Copy-to-clipboard behavior
- Prefer fixing warnings that indicate maintainability issues when practical.

## Release Chores and Sequence

- Keep manifest and project version aligned.
- Ensure required app assets exist with manifest-expected base names.
- Run branding gate before release packaging:
  - `pwsh -File scripts/prepublish-check.ps1`
- Build final Release package for x64:
  - `pwsh -File scripts/deploy-dev.ps1 -Configuration Release -Platform x64 -SkipInstall`
- Maintain `CHANGELOG.md` with release entry.
- Create annotated git tag for release:
  - `v1.0.0` style
- Publish GitHub Release from the tag with structured notes.
- GitHub Release title naming is required:
  - `PowerTranslate v<version>`
  - Example: `PowerTranslate v1.0.0.1`

## GitHub Release Notes

- Keep GitHub release bodies minimal and consistent.
- Prefer a single `**Full Changelog**:` line that links the compare view for the release range.
- Match the style used by `v1.0.2.0` for all future releases.
- Do not use freeform release prose unless the user explicitly asks for it.
- If a release already exists with the wrong body, edit the published release instead of creating a duplicate.
- For the first release in a line, use the best available left-side anchor for the compare link if there is no prior tag.

## Asset and File Naming Convention

- Base brand source image:
  - `PowerTranslateExtension/Assets/PowerTranslateLogo.png`
- Required tile/splash naming must match manifest references exactly:
  - `Square44x44Logo.png`
  - `Square150x150Logo.png`
  - `Wide310x150Logo.png`
  - `SplashScreen.png`
- Keep scale variants when applicable:
  - `*.scale-200.png`
- Screenshot asset naming for docs/store prep:
  - `TranslationUI.png`
  - `LanguageSelect.png`
  - `SettingsPage.png`

## Security and Privacy Standards

- Never commit API keys, secrets, or local cache artifacts.
- Do not log sensitive user content or credentials.
- Keep API communication and data handling aligned with `PRIVACY.md` and `README.md`.

## Packaging and Publish Standards

- Use x64 publish target for this release train.
- Debug builds are for local iteration.
- Release builds are required for Store publishing.
- Keep deployment scripts fail-fast on build errors.

## Release Execution Summary (v1.0.0.0)

- Updated Partner Center identity values in manifest and project Appx properties.
- Standardized asset inclusion with `Assets\**\*.png` and added `PrepareAssets` pre-build copy target.
- Regenerated tile/splash/store logos from `PowerTranslateLogo.png`.
- Added additional store variants: `SmallTile.png` (71x71) and `LargeTile.png` (310x310).
- Ran branding validation gate: `scripts/prepublish-check.ps1`.
- Built Release MSIX packages for x64 and ARM64.
- Created `bundle_mapping.txt` and generated final `.msixbundle` with `makeappx`.
- Published GitHub release and release tag for final submission milestone.
