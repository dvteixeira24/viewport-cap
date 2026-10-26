# viewport-cap

A tray-first Windows app for recording a free screen region and exporting it as MP4, WebM, or GIF.

Press `Ctrl+Alt+Shift+R`, drag a rectangle, wait for the countdown, and stop with the hotkey or floating HUD. Review the clip, trim it, choose a format, and export.

## Requirements

- Windows 10 version 2004 or newer, or Windows 11; x64.
- Microsoft Visual C++ runtime and Media Foundation. Windows N/KN users need the Media Feature Pack.

## Build and run

```powershell
dotnet build ViewportCap.sln -c Release
dotnet run --project src/ViewportCap.App/ViewportCap.App.csproj
```

The app starts in the tray. Open **Settings...** to change the hotkey, output folder, countdown, audio devices, or Start with Windows.

## Recordings and settings

- Settings: `%LOCALAPPDATA%\ViewportCap\settings.json`
- Logs: `%LOCALAPPDATA%\ViewportCap\logs`
- Temporary recordings: `%LOCALAPPDATA%\ViewportCap\recordings`
- Default exports: `Videos\ViewportCap`

Temporary recordings are deleted on discard, after successful export, and after seven days at startup.

## FFmpeg and publish

MP4 recording works without FFmpeg. GIF/WebM export and MP4 trimming require `ffmpeg.exe`. Fetch and publish with:

```powershell
powershell -ExecutionPolicy Bypass -File tools/fetch-ffmpeg.ps1
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

The result is `dist/viewport-cap-<version>-win-x64.zip`. The fetch script verifies the pinned LGPL archive SHA-256.

That zip is the release: unzip it anywhere and run `viewport-cap.exe`; `ffmpeg.exe` ships beside it. `dist/publish` is only the staging folder the zip is built from. A local `dotnet build` also gets `ffmpeg.exe` beside the app once `fetch-ffmpeg.ps1` has run.

## Limitations

- SDR only; selections cannot span monitors; no pause/resume or annotations.
- Protected/DRM content may record black. No installer, auto-update, or code signing.

## License

MIT for the app code. See `LICENSE` and `THIRD-PARTY-NOTICES.md`.
