# viewport-cap — Design

**Date:** 2026-10-26  
**Status:** Draft for user review  
**Target:** Windows 10 2004+ and Windows 11, x64

## 1. Summary

viewport-cap is a tray-first Windows application for recording a short, silent-by-default screencast of a screen region. A global hotkey opens a snipping-tool-style overlay. The user drags a free rectangle, a three-second countdown runs, and recording starts. A small floating HUD shows the elapsed time and provides Stop and Cancel. On Stop, a review window opens with a preview, in/out trim handles, and export settings for MP4, WebM, or GIF.

The product is intentionally narrow: it replaces the "open a screen recorder, position a frame, press Record, press Stop, save a clip" loop with one hotkey. It is inspired by Peek, but built for Windows and focused on fast regional captures for bug reports, UI demos, and documentation.

## 2. Goals

- Capture a region in a few seconds from a single global hotkey.
- Produce shareable MP4, WebM, and GIF files from the same recording.
- Make the common path silent and frictionless, while allowing optional microphone and system audio.
- Keep recordings local; no account, cloud, telemetry, or network access at runtime.
- Provide a review step so a clip can be trimmed and sized before export.
- Handle multiple monitors, mixed DPI, cancellation, and failure without leaving the app stuck.

## 3. Non-goals for v1

- Pause/resume while recording.
- Webcam, annotations, drawing, keystroke overlays, or mouse-click highlights.
- HDR capture or tone mapping. v1 is SDR only.
- Capturing a region spanning more than one monitor.
- Per-application or per-window capture.
- Cloud upload, sharing links, or a built-in editor timeline.
- Installer, auto-update, code signing, or arm64 build.
- Clipboard image/GIF copy. v1 copies the file path, not the media.
- Adjusting the selection after the mouse is released. Cancel and reselect instead.

## 4. Requirements

### 4.1 Platform

- Windows 10 version 2004 (build 19041) or newer, and Windows 11.
- x64 first. arm64 is future work.
- .NET 8 desktop runtime, published self-contained.
- Visual C++ runtime and Media Foundation available. Windows N/KN editions require the Media Feature Pack.
- No administrator privileges required.

### 4.2 Capture and output

- Free rectangular selection via a snipping-tool-style overlay.
- Default selection behavior: confined to the monitor under the cursor when the hotkey is pressed.
- Minimum region size: 32 × 32 physical pixels.
- Default countdown: 3 seconds, configurable to 0, 3, 5, or 10 seconds.
- Default capture: H.264 MP4, 30 fps, mouse pointer included, hardware encoding when available.
- Audio modes: Off (default), Microphone, System audio, Both.
- Export formats: MP4 (H.264/AAC), WebM (VP9/Opus), GIF (indexed palette up to 256 colors, silent).
- Review-time trim, frame-rate, scale, quality, and audio keep/mute controls.
- Default output folder: `Videos\ViewportCap`, created if missing.
- Default filename: `viewport-cap_yyyy-MM-dd_HH-mm-ss.<ext>`.

### 4.3 Shell behavior

- Runs from the system tray with no persistent main window.
- Default global hotkey: `Ctrl+Alt+Shift+R`, configurable.
- Second press of the hotkey while recording stops the recording.
- Single instance. Launching a second copy opens Settings in the first.
- Optional Start with Windows, implemented through `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- No elevation.

## 5. User experience

### 5.1 Startup and tray

On launch the app adds a tray icon and, on first run, shows a one-time balloon: "Press Ctrl+Alt+Shift+R to capture a region." A `hasShownFirstRunTip` flag in settings prevents it from repeating. There is no main window.

Tray context menu:

- **Capture now** — same as the hotkey.
- **Settings…**
- **Open recordings folder**
- **Start with Windows** (checkable)
- **Exit**

Left-clicking the tray icon starts a capture. The tray tooltip shows the current hotkey.

### 5.2 Region selection

The hotkey opens one dimmed, topmost, borderless overlay per monitor. The overlay under the cursor is active; the others remain dimmed and non-interactive.

- Cursor changes to a crosshair.
- Dragging draws a selection rectangle with a visible border and live dimensions in physical pixels.
- The rest of the active monitor is dimmed; the selected area remains undimmed.
- Releasing the mouse finalizes the rectangle and starts the countdown.
- **Esc** or right-click cancels the capture and closes all overlays.
- A drag smaller than 32 × 32 physical pixels is rejected with a brief inline hint; the overlay stays open for another drag.
- v1 does not offer post-release resize handles. Cancel and start again to change the selection.
- The selection is clipped to the active monitor.

### 5.3 Countdown

A compact countdown HUD appears near the selection, positioned to stay on-screen:

- Counts down from the configured duration.
- Shows the selected dimensions and the current audio mode.
- **Start now** skips the remaining countdown.
- **Cancel** (or Esc) aborts and returns to idle.
- An audio button cycles through the available modes (Off → Microphone → System → Both), skipping modes with no devices, and persists the choice for the next capture.
- The countdown HUD is excluded from capture.

When the countdown reaches zero, all overlays and HUDs close and recording starts. A countdown setting of 0 skips the countdown HUD and begins recording immediately after the drag.

### 5.4 Recording

A small floating HUD appears near the region and stays topmost:

- Red dot and elapsed time in `mm:ss`.
- Audio indicator.
- **Stop** and **Cancel** buttons.
- Dragging the HUD is not supported; it repositions automatically to avoid covering the selected region when possible.
- The global hotkey also stops the recording.
- **Cancel** stops capture, discards the temp file, and returns to idle without opening Review.
- The HUD and any app windows are excluded from capture with `WDA_EXCLUDEFROMCAPTURE`.
- The mouse pointer is intentionally included in the clip. Click highlights are not implemented in v1.

### 5.5 Review and export

Stopping a recording opens a non-modal review window. Its title includes the duration and resolution. Only one review window exists at a time; a new capture cannot start until it is closed.

Layout:

- **Preview** — WPF `MediaElement` playing the temp MP4; play/pause, timecode, and a loop over the trim range.
- **Timeline** — custom dual-thumb range slider for in/out points, with timecodes.
- **Format** — MP4, WebM, or GIF. Defaults to the format selected in Settings (MP4 by default).
- **Quality** — High, Balanced, Small. Defaults to Balanced.
- **Frame rate** — 15 or 30 fps for MP4/WebM; 10, 15, or 20 fps for GIF.
- **Scale** — 100%, 75%, 50%, or fit to 960 px wide. Defaults to 100% for video and fit-to-960 for GIF.
- **Audio** — Keep or Mute when the recording contains audio; "Silent recording" when it does not.
- **Export** — writes to the default folder.
- **Save As…** — chooses a path, then exports.
- **Discard** — deletes the temp recording after confirmation. Closing the window behaves like Discard.

Validation:

- Trim length must be at least 0.25 seconds.
- GIF exports longer than 30 seconds or wider than 1280 px show a warning but are allowed.
- The output file is written as `<name>.partial.<ext>` and moved into place only after FFmpeg succeeds.
- Existing filenames get `-2`, `-3`, and so on.

After a successful export, the review window closes, the temp file is deleted, and a toast appears with **Open file**, **Reveal in Explorer**, and **Copy path**. On failure, the review window stays open, the temp file is retained, and the error shows the FFmpeg stderr tail. Closing the review window during an export asks for confirmation and cancels the export.

### 5.6 Settings

A single settings window with:

- Hotkey (key-capture control with reset-to-default).
- Output folder (Browse).
- Countdown length.
- Default format.
- Audio mode and default microphone/output devices.
- Start with Windows.
- Open logs folder.
- Version and third-party notices link.

Capture defaults apply to the next capture; hotkey and Start with Windows apply immediately. Invalid or conflicting hotkeys are rejected with an explanation; the old hotkey stays active.

## 6. Architecture

### 6.1 Solution layout

```
ViewportCap.sln
src/
  ViewportCap.App/          WPF application, windows, tray, hotkey, single instance
  ViewportCap.Core/         domain models, state machine, interfaces, export planning
  ViewportCap.Capture/      ScreenRecorderLib adapter
  ViewportCap.Encoding/     FFmpeg location, process runner, export implementation
tests/
  ViewportCap.Core.Tests/
  ViewportCap.Encoding.Tests/
tools/
  fetch-ffmpeg.ps1
  publish.ps1
docs/
  superpowers/specs/2026-10-26-viewport-cap-design.md
Directory.Build.props
.editorconfig
.gitignore
README.md
```

`ViewportCap.Core` targets `net8.0` and has no WPF or Windows-specific references. `ViewportCap.Capture` targets `net8.0-windows`. `ViewportCap.Encoding` targets `net8.0` and shells out to FFmpeg. `ViewportCap.App` targets `net8.0-windows` with `UseWPF` and `UseWindowsForms` (for `NotifyIcon`).

### 6.2 Dependencies

- **ScreenRecorderLib 7.x** — recordable displays, region crop, media foundation H.264/AAC encoding, microphone and loopback audio. MIT.
- **CommunityToolkit.Mvvm** — observable properties and relay commands.
- **FFmpeg** — LGPL build, pinned and checksum-verified, used for GIF/WebM and MP4 trim/re-encode. Not committed to the repository.
- Built-in WPF and WinForms types for UI and tray. No third-party UI theme package in v1.

### 6.3 Components

**ViewportCap.App**

- `App` — composition root, single-instance gate, startup health checks.
- `TrayService` — `NotifyIcon`, menu, tooltip, balloon.
- `HotkeyService` — `RegisterHotKey`, `WM_HOTKEY` handling, conflict testing.
- `SingleInstanceService` — named mutex; activates Settings in the existing instance.
- `StartupService` — HKCU Run value.
- `RegionSelector` — one overlay window per monitor; returns a `CaptureRegion` or cancellation.
- `CountdownWindow` — countdown and audio quick toggle.
- `RecordingHudWindow` — timer, Stop, Cancel; excluded from capture.
- `ReviewWindow` / `ReviewViewModel` — preview, trim, export controls.
- `SettingsWindow` / `SettingsViewModel`.
- `ToastService` — completion actions.
- `CaptureFlowController` — wires the state machine to the windows and services.

**ViewportCap.Core**

- `CaptureRegion` — monitor device name, physical x/y/width/height, DPI.
- `RecordingSession` — temp file path, start/stop times, audio presence, region.
- `CaptureSettings`, `ReviewSettings`, `AppSettings`.
- `RecordingState` and `RecordingStateMachine`.
- `ICaptureEngine` — start/stop/cancel; events for failure and completion.
- `IExporter` — export a `ReviewSettings` to an output path; progress and cancellation.
- `ISettingsStore`, `IClock`, `IFileSystem`.
- `ExportPlan` and `ExportNaming` — pure functions validated by unit tests.
- `CaptureCoordinator` — orchestrates the flow and owns cleanup.

**ViewportCap.Capture**

- `ScreenRecorderCaptureEngine` — maps `CaptureRegion` to a `DisplayRecordingSource` with a physical-pixel `SourceRect`; configures video, audio, mouse pointer, and temp MP4 output.
- `CapturePreflight` — checks monitor availability and that ScreenRecorderLib's native dependencies load.

**ViewportCap.Encoding**

- `FfmpegLocator` — finds `ffmpeg.exe` beside the app or in the configured tools directory; validates version 6.0 or newer.
- `FfmpegCapabilities` — probes available encoders (`h264_mf`, `libopenh264`, `libvpx-vp9`) lazily on first export.
- `FfmpegProcessRunner` — starts FFmpeg with argument list (no shell), captures stdout/stderr, parses progress, supports cancellation.
- `FfmpegExporter` — builds and runs the format-specific export.
- `FfmpegArgumentBuilder` — pure argument construction, unit tested.

### 6.4 State machine

```
Idle
  → Selecting      hotkey / Capture now

Selecting
  → Countdown      valid region released
  → Selecting      invalid drag (hint shown, overlay stays open)
  → Idle           Esc, right-click, overlay closed, failure

Countdown
  → Recording      countdown elapsed or Start now
  → Idle           Cancel, Esc, failure

Recording
  → Finalizing     Stop / hotkey
  → Idle           Cancel, capture failure

Finalizing
  → Reviewing      temp MP4 finalized
  → Idle           failure (temp retained, error shown)

Reviewing
  → Exporting      Export / Save As
  → Idle           Discard, window closed

Exporting
  → Idle           success (temp deleted, toast)
  → Reviewing      cancel or failure (temp retained, review stays open)
```

Rules:

- Only one capture session may exist at a time.
- Every transition to `Idle` releases the capture engine, closes overlays/HUDs, and cancels pending timers.
- Temp files are deleted on cancel, discard, and successful export; retained on failure and cleaned after 7 days at startup.
- The hotkey is ignored during `Finalizing`, `Reviewing`, and `Exporting`; during `Recording` it stops.

### 6.5 Threading and lifetime

- All UI windows are created and updated on the WPF dispatcher thread.
- ScreenRecorderLib callbacks are marshaled to the dispatcher before touching UI or state.
- FFmpeg runs asynchronously; its output is read asynchronously; cancellation kills the process tree.
- The app exits only when idle. If the user chooses Exit while recording or reviewing, it asks for confirmation and cancels/cleans up.
- A single `CancellationTokenSource` per session is passed through countdown, capture, and export.

## 7. Capture backend

### 7.1 Monitor and region

- The hotkey determines the active monitor with `MonitorFromPoint` on the cursor position, falling back to the primary monitor if the cursor is not on a monitor.
- One overlay is created per monitor so mixed-DPI scaling stays correct. Only the active overlay accepts input.
- The app is Per-Monitor V2 DPI aware. Overlay coordinates are DIPs; the final rectangle is converted to physical pixels with the active monitor's DPI before capture.
- The region is validated against the monitor's physical bounds and the 32 × 32 minimum.

### 7.2 ScreenRecorderLib configuration

- `DisplayRecordingSource` for the active monitor's device name.
- `OutputOptions.SourceRect` set to the physical region.
- Output size equals the region size; review-time scale is applied during export, not capture.
- `VideoEncoderOptions`: H.264, fixed 30 fps, hardware encoding enabled, throttling enabled, low-latency disabled, and fragmented MP4 enabled for crash resilience. Fast-start is enabled when the container supports it.
- Capture bitrate is resolution-aware: approximately 0.12 bits per pixel per frame, clamped to 4–40 Mbps.
- `MouseOptions.IsMousePointerEnabled = true`; click detection is disabled.
- `AudioOptions.IsAudioEnabled` depends on the selected mode; sources are `CaptureAudioSource` and/or `LoopbackAudioSource` using the configured default devices.
- Output: `%LOCALAPPDATA%\ViewportCap\recordings\<guid>.mp4`.

### 7.3 Preflight and limits

Before recording:

- Confirm ScreenRecorderLib's native library loads. If VC++ or Media Foundation is missing, show an actionable error and a link to the prerequisite.
- Confirm at least 1 GB free on the temp volume. If less, block with a clear message.
- Confirm the selected region is still within the monitor.
- Confirm the output folder is writable at export time, not at capture time.

During recording:

- The HUD warns if free disk space drops below 500 MB and stops automatically below 250 MB to avoid filling the disk.
- The HUD shows a non-blocking warning after 10 minutes; recording continues.
- Protected/DRM content may appear black. This is documented, not worked around.

## 8. Audio

- Audio is off by default. The countdown HUD cycles Off → Microphone → System → Both and persists the choice.
- Settings choose the default microphone and output devices. v1 uses the Windows default devices unless the user picks another.
- ScreenRecorderLib mixes selected sources into the temp MP4 (AAC, 48 kHz, stereo, 128 kbps).
- If both microphone and system audio are enabled, the app shows a one-time note that speaker output can be echoed into the microphone. No echo cancellation is attempted.
- If a selected device disappears before recording, the app falls back to the Windows default and shows a warning; if that fails, it records silently with a warning.
- The review window keeps or mutes the captured audio. It cannot add audio.
- GIF always drops audio.

## 9. Export

### 9.1 Common behavior

- Export runs FFmpeg as a child process with an argument list; no shell interpolation.
- Trim uses accurate seek (`-ss` after the input) and `-t` for duration.
- Progress is parsed from `-progress pipe:1`; the UI shows percentage or an indeterminate state when duration is unknown.
- Output is written to `<name>.partial.<ext>` in the destination folder, then atomically moved to `<name>.<ext>`.
- Cancellation kills the process tree and deletes the partial file.
- The temp recording remains until export succeeds or the user discards it.
- The output path is checked; if the folder is missing or unwritable, the user is prompted to choose another.
- Filename collisions append `-2`, `-3`, etc.

### 9.2 Format details

**MP4**

- If the review settings match the capture settings (no trim, scale, fps, or quality change, audio kept), export copies the temp file directly and works without FFmpeg.
- Container: MP4. Re-encoded exports use fast-start; direct copies retain the temp file's fragmented layout.
- Video: H.264 via `h264_mf` when available, otherwise `libopenh264`.
- Audio: AAC 128 kbps when present and not muted; `-an` otherwise.
- Pixel format: `yuv420p`; `scale` uses `-2` to keep even dimensions.
- Quality maps to bits per pixel per frame: High 0.16, Balanced 0.12, Small 0.08. Target bitrate is `width × height × fps × bpp`, clamped to 2–40 Mbps.
- If neither H.264 encoder is available, export fails with an actionable message; recording still works.

**WebM**

- Video: `libvpx-vp9`, CRF 24/32/40 for High/Balanced/Small, `-b:v 0`, `-row-mt 1`.
- Audio: `libopus` 96 kbps when present; `-an` otherwise.
- Pixel format: `yuv420p`.
- If libvpx is missing, WebM is disabled with an explanation.

**GIF**

- Filter graph: fps → scale → `split` → `palettegen` → `paletteuse`.
- `palettegen` uses `stats_mode=diff`; `paletteuse` uses `dither=bayer:bayer_scale=3:diff_mode=rectangle`.
- Quality maps to palette size: High 256 colors, Balanced 128, Small 64.
- Loop forever (`-loop 0`).
- Defaults: Balanced quality (128 colors), 15 fps, max width 960.
- Audio is always dropped.
- Warning for duration > 30 s or width > 1280 px.

### 9.3 Preview

- The review preview plays the temp MP4 through WPF `MediaElement`.
- If playback fails (missing codec, unsupported fragmented MP4), the window shows "Preview unavailable" with **Open in default player** and still permits trimming by timecode.
- The preview loops the selected trim range; changing trim updates the loop points.

## 10. Windows integration

- **Tray:** `System.Windows.Forms.NotifyIcon`; icon shipped as an embedded resource.
- **Hotkey:** `RegisterHotKey` with `MOD_CONTROL | MOD_ALT | MOD_SHIFT` and `R` by default. Registration is tested before saving; conflicts show a clear message.
- **Single instance:** named mutex `Local\ViewportCap.SingleInstance`; a second launch signals the first through a named event and exits. If the first instance is idle, it opens Settings; otherwise it shows a tray balloon that a capture is in progress.
- **Start with Windows:** HKCU Run value `"<path>\viewport-cap.exe" --tray`; removed when disabled.
- **Capture exclusion:** `SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)` on overlay, countdown, HUD, review, settings, and toast windows. This requires Windows 10 2004+.
- **DPI:** Per-Monitor V2 manifest; overlays per monitor.
- **Shutdown:** cancel active work only after confirmation; kill FFmpeg children; dispose the tray icon.
- **Clipboard:** "Copy path" writes the exported file path as text. Media is not copied.

## 11. Persistence and logging

- Settings: `%LOCALAPPDATA%\ViewportCap\settings.json`, schema version 1, atomic replace, UTF-8. Includes `hasShownFirstRunTip`.
- Defaults are used if the file is missing or invalid; a corrupt file is backed up as `settings.json.bad-<timestamp>`.
- Temp recordings: `%LOCALAPPDATA%\ViewportCap\recordings`.
- Logs: `%LOCALAPPDATA%\ViewportCap\logs\viewport-cap.log`, rolling at 2 MB with five files retained.
- Logs contain lifecycle events, errors, FFmpeg exit codes, and paths. They do not contain pixels or window titles.
- At startup, temp files older than 7 days are deleted.
- No telemetry, update checks, or network calls.

## 12. Error handling

| Situation | Behavior |
|---|---|
| Hotkey registration fails | Keep the previous hotkey; show conflict dialog with affected keys. |
| ScreenRecorderLib native load fails | Show prerequisite message (VC++ runtime / Media Foundation / Media Feature Pack); disable capture. |
| FFmpeg missing | Untrimmed MP4 recording can still be saved by copying the temp file; GIF, WebM, and any trim/scale/fps re-encode are disabled with an explanation. |
| Recording fails to start | Return to idle; show error and log path; delete empty temp file. |
| Recording fails mid-way | Keep the partial temp file if readable; show its folder and log path. |
| Disk below 500 MB during recording | Warn; stop automatically below 250 MB and open Review if the file is readable. |
| Export fails | Keep temp and review open; show FFmpeg stderr tail; allow retry or Save As. |
| Output folder unwritable | Prompt for another folder; do not lose the temp. |
| Audio device disappears | Fall back to default or silent; warn once; continue recording. |
| App crashes | Fragmented MP4 may remain recoverable; stale temp files are retained for 7 days. |
| Second instance launches | Activate Settings in the first instance, then exit. |
| Hotkey pressed while busy | Ignored except during Recording, where it stops. |

## 13. Testing

### 13.1 Unit tests

- State machine: every transition, invalid events, cancellation at every state, single-session guarantee.
- Region: drag normalization (negative width/height), monitor clipping, DPI conversion, 32 × 32 minimum, physical bounds.
- Export planning: format, trim, fps, scale, audio keep/mute, codec fallback, filename collisions.
- FFmpeg argument builder: each format, odd dimensions, no audio, edge trim times, output escaping as argument list.
- Settings: defaults, load/save round-trip, corrupt file backup, schema version, hotkey validation.
- Filename generation and timestamp formatting.
- Logging redaction rules if any paths are logged.

### 13.2 Integration tests

Marked `Integration` and not run by default in CI:

- Run FFmpeg against a small generated fixture; export MP4, WebM, and GIF; probe each output for duration, dimensions, and streams.
- Cancellation: start a long export, cancel, assert the process exits and the partial file is removed.
- Missing FFmpeg and missing encoder paths.

### 13.3 Manual acceptance matrix

- Single monitor and two/three monitors in different layouts.
- 100%, 125%, 150%, and 200% scaling on the active monitor.
- Portrait and landscape monitors; selection near every edge and corner.
- Cancel during selection, countdown, recording, review, and export.
- Silent, microphone, system audio, and both; verify A/V sync and mute-on-export.
- MP4, WebM, and GIF exports with trim and scale; verify playback in a browser and Windows.
- Verify overlay, countdown, HUD, and app windows do not appear in the clip; verify the mouse pointer does.
- Hotkey conflict; missing FFmpeg; missing VC++/Media Foundation; low disk space.
- Ten-minute recording and a 4K-region stress test.
- Start with Windows on/off; second launch focuses Settings.

### 13.4 CI

- GitHub Actions on `windows-latest`.
- `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release --filter Category!=Integration`.
- No capture tests in CI.
- Publish job is manual in v1.

## 14. Packaging

- `tools/fetch-ffmpeg.ps1` downloads a pinned LGPL FFmpeg build from BtbN FFmpeg-Builds, verifies SHA-256, and extracts `ffmpeg.exe` into `artifacts/ffmpeg`.
- FFmpeg binaries are not committed.
- `tools/publish.ps1` runs tests, fetches FFmpeg, publishes self-contained `win-x64`, copies FFmpeg, `LICENSE`, `THIRD-PARTY-NOTICES.md`, and `README.md`, then writes `dist/viewport-cap-<version>-win-x64.zip`.
- Publish settings: self-contained, `PublishSingleFile=false` (native dependencies), `PublishTrimmed=false`, `PublishReadyToRun=true`.
- Executable name: `viewport-cap.exe`.
- App version lives in `Directory.Build.props`.
- No installer, auto-update, or code signing in v1.
- App code license: MIT. Third-party notices cover ScreenRecorderLib (MIT), FFmpeg (LGPL), and .NET.

## 15. Risks and mitigations

| Risk | Mitigation |
|---|---|
| ScreenRecorderLib native dependency or Media Foundation issues | Preflight load check; actionable prerequisite message; thin adapter allows a direct WGC backend later. |
| Mixed-DPI/multi-monitor coordinate bugs | Per-monitor overlays, physical-pixel conversion, one-monitor-per-clip rule, manual DPI matrix. |
| Capture HUD appears in the recording | `WDA_EXCLUDEFROMCAPTURE`; acceptance test with bright HUD on dark content. |
| FFmpeg licensing and binary size | LGPL build fetched separately; H.264 export avoids GPL encoders. |
| GIF export is slow or huge | 15 fps and 960 px defaults, 30 s warning, palette pipeline, progress and cancel. |
| H.264 encoder unavailable | Probe `h264_mf` and `libopenh264`; fail with a clear message; recording still works. |
| Temp disk growth | 1 GB preflight, 500 MB warning, 250 MB auto-stop, 7-day cleanup. |
| Preview cannot play fragmented MP4 | Fallback "Open in default player"; timecode trimming remains available. |
| Hotkey conflicts | Test registration before saving; keep previous hotkey; show conflict. |

## 16. Acceptance criteria

The v1 design is complete when:

1. Pressing the default hotkey opens the selection overlay on the monitor under the cursor.
2. A valid drag starts a three-second countdown, then records the region at 30 fps.
3. Stop opens Review with a playable preview and correct duration.
4. Trim, scale, quality, and mute controls affect the exported file.
5. MP4, WebM, and GIF exports succeed from the same recording.
6. Optional microphone and system audio are captured and can be muted at export.
7. Overlay, countdown, HUD, and app windows are absent from the recording; the pointer is present.
8. Cancel at any stage returns to idle and cleans up temp files.
9. Hotkey conflict, missing FFmpeg, missing prerequisites, and low disk space produce actionable errors without crashing.
10. The app starts in the tray, can start with Windows, and exits cleanly.
11. Unit tests pass in CI; integration tests pass locally with FFmpeg.
12. `tools/publish.ps1` produces a runnable self-contained zip.

## 17. Future work

- Post-release selection resize handles and window/aspect snapping.
- Pause/resume.
- Annotations, click highlights, and keystroke overlays.
- HDR capture and tone mapping.
- Multi-monitor spanning selections.
- Installer, auto-update, code signing, and arm64.
- Clipboard media copy and quick-share actions.
- Per-window capture and saved capture presets.
- Optional hardware-accelerated GIF encoding.
