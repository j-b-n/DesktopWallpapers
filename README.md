# DesktopWallpapers

DesktopWallpapers is a Windows app that downloads the current Bing image and lets you:

- set it as your desktop wallpaper
- optionally export it to one or more MediaPortal image files

## Usage

### Install (recommended)

Use the published releases in this repository:

https://github.com/j-b-n/DesktopWallpapers/releases

1. Open the latest release.
2. Download the release asset for Windows.
3. Start `DesktopWallpapers.exe`.

### Run modes

DesktopWallpapers supports both GUI mode and CLI mode.

#### GUI mode

Run `DesktopWallpapers.exe` without arguments.

In the app menu (`FILE`):

- `Set wallpaper`: applies the current downloaded image as wallpaper
- `Set+MP`: sets wallpaper and writes MediaPortal files
- `Force update`: clears cached metadata/image and refreshes from Bing
- `Options`: configure MediaPortal target files

#### CLI mode

Run with one or more flags:

```powershell
DesktopWallpapers.exe -s
DesktopWallpapers.exe -s -f
DesktopWallpapers.exe -m -f
DesktopWallpapers.exe -s -m -f -v
```

Available flags:

- `-s`, `--setwallpaper`: set current image as wallpaper
- `-m`, `--mediaportal`: save image to configured MediaPortal files
- `-f`, `--force`: force metadata/image refresh
- `-v`, `--verbose`: enable verbose logging
- `-h`, `--help`: show CLI help

### First-time setup for MediaPortal export

1. Open the app in GUI mode.
2. Go to `FILE` -> `Options`.
3. Add one or more MediaPortal file paths.
4. Click `Save`.
5. Use `Set+MP` in GUI mode, or run CLI with `-m`.

### Data, cache, and logs

DesktopWallpapers stores data under:

`%AppData%\j-b-n\DesktopWallpaper\`

Typical files:

- `settings.jsn`
- `BingMetadata.json`
- `backgroundImage` (and a generated `.png` for wallpaper)
- `DesktopWallpapers.log`

## Build from source

```powershell
dotnet build DesktopWallpapers.sln
```

## Releases

This repository publishes release builds at:

https://github.com/j-b-n/DesktopWallpapers/releases
