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
   - Use the x64 package: `PowerTranslateExtension_1.1.0.0_x64.msix`

### DeepL Account

You need a DeepL API key before the app can translate text.
For the sake of testing, a DeepL API key is provided for this submission.

(Optional): Generate your own API key
1. Create or sign in to a DeepL account at <https://www.deepl.com/>
2. Get an API key from the DeepL account dashboard

## Installation Steps

### Step 1: Install PowerToys

1. Open Microsoft Store
2. Search for PowerToys or use the link above
3. Install PowerToys and launch it once
4. Verify Command Palette is enabled

### Step 2: Install PowerTranslate

1. Import `PowerTranslateExtension_1.1.0.0_x64.cer` to `CurrentUser\\TrustedPeople`.
2. Open the `PowerTranslateExtension_1.1.0.0_x64.msix` package in File Explorer or run it directly with App Installer.
3. Approve the certificate prompt if Windows asks for it.
4. Finish the install wizard.

### Step 3: Refresh PowerToys

1. Open Command Palette
2. Run `Reload Command Palette extensions`
3. Confirm PowerTranslate appears in the list of available extensions

### Step 4: Configure DeepL

1. Open PowerTranslate from Command Palette
2. Run `Configure DeepL API key`
3. Paste in the provided or your own DeepL API key
4. Click `Save`
5. Translation functionality should be successful with valid API key.
6. (Optional) Run `Reload Command Palette extensions` once to update the fetched language selection list in Command Palette extension settings

## Testing the Extension

### Basic Functionality Test

1. Open Command Palette
2. Search for `Translate text` and select
3. Type in text to be translated
4. Verify the translated text is returned in the info box
5. Enter to copy text
6. Confirm copy-to-clipboard works from the result view
7. Choose a source language or leave it on AUTO
8. Choose a target language
9. If translation fails, follow shown error handling

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

- `PowerTranslate` appears in Command Palette after reload
- Translation requests complete successfully with a valid DeepL API key
- Invalid API keys and network failures show clear messages
- Language preferences persist across restarts
- Copy-to-clipboard works from the translation result view

## Troubleshooting

### Extension Not Appearing

- Restart PowerToys
- Run `Reload Command Palette extensions`
- Verify the package installed successfully in Windows Settings > Apps

### Translation Does Not Work

- Check the DeepL API key
- Verify internet connectivity
- Confirm the target language is supported

### Settings Not Persisting

- Reload Command Palette extensions after changing settings
- Check local cache files under `C:\Users\[User]\AppData\Local\Packages\lamteteeow.PowerTranslate_8x1cxbv97rw5g\LocalCache\Local\PowerTranslateExtension`
- Confirm `deepl.key`, `source-language.txt`, and `target-language.txt` are present as cached.

## What to Test

1. Installation from the release bundle
2. Extension discovery after reload
3. Valid translation flow
4. Invalid API key handling
5. No internet handling
6. Language persistence across restarts
7. AUTO source detection
8. Copy-to-clipboard behavior

## Reporting Issues

If you encounter problems, include:
- Windows version and build number
- PowerToys version
- Exact error message
- Steps to reproduce
- Screenshots if applicable
