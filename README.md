# PowerTranslate Extension

<p align="center">
 <img src="PowerTranslateExtension/Assets/PowerTranslateLogo.png" alt="PowerTranslate Logo" width="280" />
</p>

A lightweight translation extension for Microsoft PowerToys Command Palette via DeepL API.

## Overview

PowerTranslate brings fast, accurate translation directly into your PowerToys Command Palette workflow. Open the Command Palette, search for "Translate", and instantly translate text between 30+ language pairs using DeepL's industry-leading neural translation engine.

## Key Features

- **Quick Translation**: Translate text directly from the Command Palette with automatic language detection
- **Multi-Language Support**: Support for 30+ languages via DeepL API, with intelligent AUTO-detection for source language
- **Flexible Language Selection**: Easily switch source and target languages with an intuitive picker interface
- **Persistent Settings**: Source/target language preferences saved between sessions for seamless workflow
- **API Key Management**: Built-in configuration page for secure, encrypted DeepL API key storage
- **Cached Language Lists**: Efficient language list caching to minimize API calls
- **Lightweight Design**: Minimal dependencies, optimized for responsiveness
- **Copy-Friendly Results**: Translation results display with source→target language indicator for clarity

## Requirements

- **OS**: Windows 10 Build 19041 or later, or Windows 11
- **PowerToys**: Latest version with Command Palette support
- **.NET**: .NET 9.0 (included in packaged app)
- **DeepL API Key**: Free or paid account at [deepl.com](https://www.deepl.com/docs-api/accessing-the-api)
- **Architecture**: x64 (AMD64) only

## Installation

### From Microsoft Store (Recommended)

_Coming soon_ - App will be available on Microsoft Store. Link will be updated here upon release.

### Manual Installation (Developer)

1. Clone this repository
2. Build the project in Visual Studio 2022
3. Deploy using `Deploy Extension (Debug x64)` task
4. Reload PowerToys Command Palette extensions

## How It Works

1. Open PowerToys Command Palette
2. Type "Translate" to access translation commands
3. Configure your DeepL API key (one-time setup)
4. Select or leave source language as AUTO for automatic detection
5. Choose target language
6. Enter text to translate
7. View results with automatic copy-to-clipboard support
8. Change languages on-the-fly without re-entering text

## Screenshots

### Translation UI

![Translation UI](PowerTranslateExtension/Assets/TranslationUI.png)

### Language Selection

![Language Selection](PowerTranslateExtension/Assets/LanguageSelect.png)

### Settings Page

![Settings Page](PowerTranslateExtension/Assets/SettingsPage.png)

## Configuration

After installation, use the "Configure DeepL API key" command in the palette to:
- Enter your DeepL API key (get one at [deepl.com](https://www.deepl.com/docs-api/accessing-the-api))
- Validate connectivity to the DeepL API
- Save encrypted key for future use

> **Note**: After setting the API key, reload Command Palette extensions to refresh language choices. Use `Ctrl+Shift+P` and search "Reload Command Palette extensions".

## Architecture

```text
PowerTranslateExtension/
├── Services/
│   ├── DeepLTranslator.cs       # DeepL API integration with language caching
│   └── LocalSettingsStore.cs    # Encrypted key storage & language preferences
├── Pages/
│   ├── PowerTranslateExtensionPage.cs  # Main translation UI
│   └── DeepLSettingsPage.cs            # API key configuration
└── PowerTranslateExtensionCommandsProvider.cs  # Extension entry point
```

### Key Components

- **DeepLTranslator**: Handles all API communication, language list caching, and translation requests with metadata
- **LocalSettingsStore**: Manages encrypted API key storage and language preference persistence with thread-safe caching
- **PowerTranslateExtensionPage**: Interactive translation page with copy button and language picker navigation
- **DeepLSettingsPage**: Configuration form for API key setup and validation

## Troubleshooting

**Languages not loading?**
- Verify your DeepL API key is valid: use "Configure DeepL API key" command
- Check your internet connection
- Ensure you've reloaded Command Palette extensions after saving API key

**Translation not working?**
- Check API key validity (free accounts have usage limits)
- Verify internet connectivity
- Try a different language pair to isolate the issue

**Settings not persisting?**
- Settings are cached in: `C:\Users\[User]\AppData\Local\Packages\PowerTranslateExtension_8wekyb3d8bbwe\LocalCache\Local\PowerTranslateExtension\`
- Verify folder exists and is accessible
- Clear cache files if corrupted: `deepl.key`, `source-language.txt`, `target-language.txt`

**API key not saving?**
- Ensure API key is not empty
- Verify Windows can encrypt data (Windows Data Protection API must be available)
- Run "Configure DeepL API key" command and try again

## Privacy

See [PRIVACY.md](PRIVACY.md) for the formal policy used for release and store submission.

### Data Handling

- **Translation requests**: Sent to DeepL API servers over HTTPS
- **API key**: Stored locally and encrypted using Windows Data Protection API
- **Language preferences**: Stored locally in plain text (no sensitive data)
- **No telemetry**: No usage data, tracking, or analytics collected

### Data Stored Locally

- Encrypted API key (_not_ accessible outside secure storage)
- Selected source language
- Selected target language

### External Communication

- Only communicates with DeepL API for translation requests
- No communication with Microsoft or PowerToys telemetry systems beyond Command Palette discovery

### GDPR/CCPA Compliance

- No personal data collected or stored
- No third-party tracking
- Users have full control over API key and language settings

## Microsoft Store Submission Copy

### Concise Description (50 words)

PowerTranslate brings DeepL-powered translation directly into PowerToys Command Palette. Translate text instantly with automatic source detection, flexible source/target language selection, and encrypted local API key storage. Built for speed, privacy, and daily workflow efficiency on Windows 10 and Windows 11.

### Detailed Description

PowerTranslate is a productivity-focused translation extension for Microsoft PowerToys Command Palette. It allows you to translate text without leaving your keyboard-driven workflow. Choose source and target languages, or enable AUTO source detection to let DeepL identify input language automatically.

The extension includes encrypted local storage for your DeepL API key using Windows Data Protection API, plus persistent language preferences so your most-used settings are ready every session. It is designed to be lightweight, responsive, and practical for developers, writers, support teams, and multilingual users.

Key benefits include:
- Fast translation from Command Palette
- 30+ supported language pairs through DeepL
- On-the-fly source/target switching
- Clear source to target indicator in translation results
- No telemetry, no analytics, and no third-party tracking

PowerTranslate communicates only with DeepL for translation requests and stores only what is needed for operation. This makes it a focused utility for users who want high-quality translation with minimal friction and strong privacy defaults.

### Suggested Store Categories and Tags

- Categories: Productivity, Utilities
- Tags: translation, language, deepl, command palette, powertoys

### Support Contact

- GitHub Issues: <https://github.com/lamteteeow/PowerTranslateExtension/issues>

## Development Status

**Status**: Stable v1.0.2.0 release

Core translation functionality, language selection, and settings persistence are production-ready. Windows 10/11 support verified.

### Architecture Validation

- **x64 (Windows host)**: Build, package, install, and extension startup verified.
- **ARM64 support**: Not targeted in this release.

## Contributing

Contributions are welcome! Whether it's bug fixes, feature requests, documentation improvements, or translations, we'd love your help.

> **Disclaimer**: The author is inexperienced with common open-source standards and conventions. We greatly appreciate feedback, code reviews, and guidance from experienced contributors.

Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - See [LICENSE](LICENSE) file for details.

This software is provided "AS IS" without warranty of any kind. The author assumes no liability for any damages, data loss, or issues arising from use of this extension.
