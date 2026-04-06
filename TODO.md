# PowerTranslate Extension - Pre-Publishing Plan

## Phase 1: Version & Packaging (CRITICAL)

- [x] Update version to 1.0.0.0 in Package.appxmanifest
- [x] Update version in PowerTranslateExtension.csproj AssemblyVersion
- [ ] Update PublisherDisplayName if needed (currently "Lam Tran")
- **Commit**: "chore: bump version to 1.0.0.0 release"

## Phase 2: Documentation

- [x] Add "Requirements" section to README.md
  - Windows 10 Build 19041 or later
  - PowerToys with Command Palette
  - DeepL API key (free or paid)
- [x] Add "Installation" section with Microsoft Store link placeholder
- [x] Add "Troubleshooting" section with cache location
- [x] Add "Privacy" section explaining API communication and encryption
- [x] Update CONTRIBUTING.md with code quality standards
- **Commit**: "docs: add requirements, installation, troubleshooting, and privacy sections"

## Phase 3: Code Quality & Security

- [x] Review error handling in DeepLTranslator.cs (network failures, invalid responses)
- [x] Add user-friendly error messages for common failures
- [x] Verify API key is never logged or exposed
- [x] Add try-catch blocks where missing
- [ ] Test with expired/invalid API keys
- **Commit**: "refactor: improve error handling and user-facing messages"

## Phase 4: Assets & Branding

- [x] Keep `PowerTranslateExtension/Assets/PowerTranslateLogo.png` as the only trusted final branding asset
- [x] Treat every other image under `PowerTranslateExtension/Assets/` as placeholder until explicitly replaced
- [x] Do placeholder handling by file path only (no image-content review):
  - `LockScreenLogo.scale-200.png`
  - `Square150x150Logo.scale-200.png`
  - `SplashScreen.scale-200.png`
  - `Square44x44Logo.scale-200.png`
  - `Wide310x150Logo.scale-200.png`
  - `StoreLogo.png`
  - `Square44x44Logo.targetsize-24_altform-unplated.png`
- [ ] Replace placeholder assets with final branded PNGs at the same filenames (to avoid manifest path changes)
- [ ] Add a pre-release check that blocks submission if any placeholder-marked filename is still pending replacement
- [ ] Verify transparency and store-required dimensions for each replacement asset
- **Commit**: "docs: define placeholder asset replacement policy"

## Phase 5: Testing

- [ ] Test on Windows 10 x64 (clean build)
- [ ] Test on Windows 11 x64 (clean build)
- [ ] Test on Windows 11 ARM64 (if possible)
- [ ] Test without API key (should show helpful error)
- [ ] Test with invalid API key (should show helpful error)
- [ ] Test with no internet connection (should show helpful error)
- [ ] Test language selection persistence across sessions
- [ ] Test translation with all major language pairs
- [ ] Test copy-to-clipboard functionality
- **Commit**: "test: validation on target platforms passed"

## Phase 6: Create Privacy Policy

- [x] Create PRIVACY.md or host privacy policy URL
- [x] Document:
  - Data sent to DeepL (text to translate)
  - Data stored locally (encrypted API key, language preferences)
  - No telemetry or tracking
  - GDPR/CCPA compliance
- **Commit**: "docs: add privacy policy"

## Phase 7: Code Signing Preparation

- [x] Research certificate options for MSIX signing
  - Self-signed (for testing)
  - Commercial certificate (for Store)
- [x] Document signing process in CONTRIBUTING.md
- [ ] Create pre-build signing script (optional)
- **Commit**: "chore: add code signing documentation"

## Phase 8: Microsoft Store Submission Prep

- [ ] Create 3-5 screenshots showing:
  - Translation UI
  - Language selection
  - Settings page
  - Result with language indicators
- [x] Write concise app description (50 words)
- [x] Write detailed app description (1000 chars, benefits & features)
- [x] Create category tags (Productivity, Translation, Utilities)
- [x] Set up support/contact information
- **Commit**: "docs: prepare Microsoft Store submission materials"

## Phase 9: Final Pre-Release

- [ ] Run full test suite end-to-end
- [ ] Verify all commits follow Conventional Commits format
- [ ] Create CHANGELOG.md with v1.0.0 entry
- [ ] Tag release as v1.0.0 in git
- [ ] Build final MSIX packages (x64 & ARM64)
- **Commit**: "release: prepare v1.0.0 for public launch"

## Phase 10: Post-Launch (After Store approval)

- [ ] Update README with Microsoft Store link
- [ ] Announce on GitHub Releases
- [ ] Monitor store reviews and issues
- [ ] Plan v1.1.0 features based on feedback

---

## Implementation Order

Start with Phase 1 (critical), then phases 2-6 in parallel, complete 7-9 before submission.
