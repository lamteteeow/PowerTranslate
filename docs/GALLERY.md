# Submitting to the CmdPal Extension Gallery

This guide walks you through submitting PowerTranslate to the [microsoft/CmdPal-Extensions](https://github.com/microsoft/CmdPal-Extensions) gallery.

## One-time setup

1. Fork https://github.com/microsoft/CmdPal-Extensions
2. Clone your fork:
   ```
   git clone https://github.com/YOUR_USERNAME/CmdPal-Extensions.git
   ```

## Submitting (each update)

1. Copy files from this repo's `gallery/` folder into your fork:
   ```
   Copy-Item -Recurse gallery/* CmdPal-Extensions/extensions/lamteteeow/powertranslate/
   ```
   The target folder path **must** match the `id` in `extension.json` (`lamteteeow.powertranslate` → `lamteteeow/powertranslate`).

2. Commit and push:
   ```
   git add extensions/lamteteeow/powertranslate/
   git commit -m "Add lamteteeow.powertranslate to gallery"
   git push origin main
   ```

3. Open a pull request to `microsoft/CmdPal-Extensions` targeting the `main` branch.

4. CI will auto-validate your submission. If errors occur, fix them and push new commits to your PR branch.

5. A maintainer reviews. Once merged, your extension appears in the Command Palette gallery.

## Files in this repo

| File | Purpose |
|---|---|
| `gallery/extension.json` | Extension metadata (name, description, install sources, etc.) |
| `gallery/icon.png` | 256x256 icon shown in the gallery |
| `gallery/screenshots/` | Up to 5 screenshots shown on the details page |

## Updating

To update your listing (e.g., new description, version, screenshots):
1. Edit the files in `gallery/` in **this** repo
2. Repeat the copy + PR steps above
