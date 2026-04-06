# Contributing to PowerTranslate Extension

Thanks for your interest in improving PowerTranslate.

## Development Setup

1. Install Visual Studio 2022 with .NET desktop tooling.
2. Clone the repository.
3. Open `PowerTranslateExtension.sln`.
4. Build and deploy with one of the VS Code tasks:
   - `Deploy Extension (Debug x64)`
   - `Deploy Extension (Debug ARM64)`

## Code Quality Standards

- Follow existing architecture and naming patterns.
- Keep methods focused and avoid mixing UI, storage, and API logic.
- Handle external failures (network, API, invalid input) with user-friendly messages.
- Never log, print, or expose DeepL API keys.
- Keep changes small and targeted; avoid unrelated refactors.
- Use nullable-safe code patterns and resolve new warnings when practical.

## Testing Expectations

Before opening a pull request:

1. Build successfully for x64.
2. Validate ARM64 build if your environment supports it.
3. Test these scenarios manually:
   - Missing API key
   - Invalid API key
   - No internet connection
   - At least two language pairs, including AUTO source detection
4. Confirm language preferences persist across extension reload.

## Commit and PR Guidelines

- Use Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, `test:`).
- Keep one logical change per commit.
- Include a clear PR description with:
  - What changed
  - Why it changed
  - How it was tested

## Security and Privacy

- Do not commit secrets, API keys, or local cache files.
- Treat all translation text as potentially sensitive user data.
- Keep data handling aligned with the privacy section in `README.md`.

## License

By contributing, you agree that your contributions are licensed under the MIT License used by this project.
