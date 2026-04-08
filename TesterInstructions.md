# PowerTranslate Command Palette Extension - Testing Instructions

## Prerequisites

### System Requirements

- Windows 11 or Windows 10 version 2004 (build 19041) or newer
- x64 processor
- Internet connection for translation requests

### Required Software

1. **PowerToys with Command Palette**
   - Install from Microsoft Store: <https://apps.microsoft.com/detail/xp89dcgq3k6vld>
   - Ensure Command Palette is enabled in PowerToys
2. **PowerTranslate package**
   - Use the release bundle: `PowerTranslateExtension_1.0.0.1_Bundle.msixbundle`

### Package Scope

- Current release validation scope is **x64 only**
- Use release packages for final verification
- Use debug deploy only for local development verification
- ARM64 can be validated as a **build/package check** where environment allows

### DeepL Account

You need a DeepL API key before the app can translate text.

1. Create or sign in to a DeepL account at <https://www.deepl.com/>
2. Get an API key from the DeepL account dashboard
3. Keep the key handy for the setup step below

## Installation Steps

### Step 1: Install PowerToys

1. Open Microsoft Store
2. Search for PowerToys or use the link above
3. Install PowerToys and launch it once
4. Verify Command Palette is enabled

### Step 2: Install PowerTranslate

1. Open the release bundle in File Explorer or run it directly with App Installer
2. If the bundle was copied into package output, it will be under `PowerTranslateExtension/AppPackages/..._x64_Test/`
3. Approve the certificate prompt if Windows asks for it
4. Finish the install wizard

### Optional: Developer Deploy Path (Debug x64)

For local validation builds only:

1. Run VS Code task: `Deploy Extension (Debug x64)`
2. Wait for deployment to finish
3. Restart or reload Command Palette extensions

### Optional: ARM64 Build Validation (Where Possible)

Use this when you want to verify ARM64 packaging output on a development machine:

1. Open PowerShell in repo root
2. Run:
   `dotnet msbuild .\PowerTranslateExtension\PowerTranslateExtension.csproj /restore /p:Configuration=Debug /p:Platform=ARM64 /p:GenerateAppxPackageOnBuild=true /p:AppxPackageSigningEnabled=false /v:m`
3. Confirm build succeeds and output is generated under `PowerTranslateExtension/AppPackages/..._arm64_Debug_Test/`
4. If testing on an x64-only machine, treat this as a build verification only (not an install validation)

### Step 3: Refresh PowerToys

1. Open Command Palette
2. Run `Reload Command Palette extensions`
3. Confirm PowerTranslate appears in the list of available extensions

### Step 4: Configure DeepL

1. Open PowerTranslate from Command Palette
2. Run `Configure DeepL API key`
3. Paste in your DeepL API key
4. Click `Save`
5. If API key is valid, translation should be successful from here
6. Run `Reload Command Palette extensions`

## Testing the Extension

### Basic Functionality Test

1. Open Command Palette
2. Search for Translate or PowerTranslate
3. Launch the translation command
4. Enter text to translate
5. Choose a source language or leave it on AUTO
6. Choose a target language
7. Verify the translated text is returned
8. Confirm copy-to-clipboard works from the result view
9. If translation fails, check that the DeepL key was entered correctly

### Settings and Persistence Test

1. Open the DeepL API key settings command
2. Enter a valid API key
3. Close and reopen Command Palette
4. Verify the saved API key and language choices persist
5. Change source and target languages
6. Reopen the Command Palette extension settings
7. Confirm the selections are still saved

### Error Handling Test

1. **Invalid API Key**: Try an invalid API key and confirm you see a clear error message
2. **No Internet Connection**:
   - Disable your internet or turn off Wi-Fi
   - Try to translate text
   - Confirm you see an error message that says the connection failed
   - The error should be clear about the network issue, not a server error
   - Turn internet back on and confirm translation works again
3. **Valid Flow After Error**: After testing the error cases above, ensure translation works normally with correct settings
4. **Multiple Language Pairs**: Test with different language combinations, including AUTO source detection

## Expected Behavior

- PowerTranslate appears in Command Palette after reload
- Translation requests complete successfully with a valid DeepL API key
- Invalid API keys and network failures show clear messages
- Language preferences persist across restarts
- Copy-to-clipboard works from the translation result view

## Troubleshooting

### Extension Not Appearing

- Restart PowerToys
- Run `Reload Command Palette extensions`
- Verify the package installed successfully in Windows Settings > Apps

### Install Fails

- Make sure the bundle is signed with the current publisher certificate
- Reinstall the certificate if Windows asks for trust approval
- Use an elevated PowerShell session if certificate trust prompts fail

### Translation Does Not Work

- Check the DeepL API key
- Verify internet connectivity
- Confirm the target language is supported
- Confirm your DeepL plan supports the feature and usage limits are not exceeded

### Settings Not Persisting

- Reload Command Palette extensions after changing settings
- Check local cache files under `C:\Users\[User]\AppData\Local\Packages\PowerTranslateExtension_8wekyb3d8bbwe\LocalCache\Local\PowerTranslateExtension\`
- Confirm `deepl.key`, `source-language.txt`, and `target-language.txt` are present

## What to Test

1. Installation from the release bundle
2. Extension discovery after reload
3. Valid translation flow
4. Invalid API key handling
5. No internet handling
6. Language persistence across restarts
7. AUTO source detection
8. Copy-to-clipboard behavior
9. ARM64 package build verification where environment supports ARM64 validation

## Reporting Issues

If you encounter problems, include:
- Windows version and build number
- PowerToys version
- Exact error message
- Which package file you installed
- Steps to reproduce
- Screenshots if applicable
