# Audit fixes implementation plan

**Goal:** Fix the seven concrete findings approved in the repository audit.
**Architecture:** Keep the current WPF app, capture adapter, and exporter. Fix shared file operations once, keep UI work on its dispatcher, and await active work before normal shutdown. No new dependencies.

## Steps
- [x] Protect unrelated partial files in copy and transcode exports; add failure/success regression tests.
- [x] Make both MP4 copy callers use one asynchronous, cancellable implementation; test cancellation and destination preservation.
- [x] Add Windows app regression tests for GIF-default MP4 fallback and fix its settings.
- [x] Test and fix early recorder failures by arming completion before starting capture.
- [x] Test and fix HUD placement within the selected monitor's physical work area.
- [x] Test publish failure propagation with stub commands; check every native exit code.
- [x] Make capture/export work awaitable; test shutdown waits for cleanup, and cancel selection/countdown/probe waits.
- [x] Run Release build, all tests, script checks, and review the final diff.

## Verification
Use `dotnet test ViewportCap.sln -c Release --filter "Category!=Integration"` and `dotnet build ViewportCap.sln -c Release --no-restore`. Run each regression before its fix and again afterwards. Use a Windows STA dispatcher for WPF tests, fake capture/export engines instead of hardware, and isolated temporary directories for file/script checks. Do not publish releases, change registry settings, or commit without a request.
