# Changelog

All notable changes to this project will be documented in this file.

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
