# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0.1] - 2026-04-08

### Added

- Configure DeepL settings context now supports prominent status styling for validation feedback.
- ARM64 build-validation workflow documented for environments where ARM64 packaging checks are needed.
- Helper scripts for generating and installing test certificates in local development workflows.

### Changed

- Improved DeepL settings page feedback behavior for invalid keys and network validation failures.
- Updated tester instructions with x64 deploy flow and ARM64 package verification steps.

### Fixed

- Clarified tester steps and troubleshooting wording for reload flow and local settings persistence.

## [1.0.0] - 2026-04-07

### Added

- Screenshot gallery for translation page, language selection, and settings in the README.
- Formal privacy policy document for release and Store submission.
- Contributing guide with quality, testing, and signing guidance.
- Pre-publish branding validation script at `scripts/prepublish-check.ps1`.

### Changed

- Translation result details now show source to target language indicator.
- Improved user-facing error messages and resilience in translation flow.
- Switched release workflow to x64-only targeting.
- Regenerated app tile/splash/store logo assets from the base PowerTranslate brand logo.

### Fixed

- Removed duplicate AUTO option in language selection.
- Corrected language persistence defaults and settings handling.
- Ensured Start menu tile assets are packaged with expected base filenames.
