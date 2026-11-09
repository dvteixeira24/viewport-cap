# Capture Review Refresh — Design

Date: 2026-11-09
Status: approved in brainstorm chat (theme A, export A, trim A)
Scope: ReviewWindow only. No capture, HUD, settings, or exporter format changes.

## Decisions (locked via visual companion)

- Theme: A System Modern — evolve SystemColors/Segoe UI, radius 8px overlays / 4px controls, softer dividers. No custom dark palette. Keeps high-contrast support. Updates DESIGN.md + App.xaml tokens only.
- Export progress: A Inline+ — status `Exporting {file} — {p}%`, 6px bar, visible Cancel wired to existing CancellationTokenSource. Stop disabling RootGrid; disable only export inputs. Preview/trim stay live.
- Trim: A Timeline+thumbs — single `TrimTimeline` UserControl: dim cut + Highlight fill for kept, 2 drag handles + playhead tick, middle-drag to move. Typed mm:ss.ff boxes below. 0.25s min gap preserved. Keeps automation names.

## Changes

1. `src/ViewportCap.App/Windows/ReviewWindow.xaml`
   - Replace StartSlider/EndSlider rows with `<local:TrimTimeline>` + Start/End TextBoxes + duration label `kept Xs of Ys`.
   - Export section: add PercentText + CancelButton beside Export; ProgressBar Height 6, Maximum 100.
   - Export grid: VisualState / WrapPanel to 2x2 under 860px width.
2. `src/ViewportCap.App/Windows/TrimTimeline.xaml(.cs)` (new, ~100 lines)
   - DependencyProps Start, End, Duration, Playhead. Thumb drag clamps [0,Duration], enforces gap. Fill widths bound. Keyboard path is TextBoxes, not thumbs.
   - Brushes: Highlight for fill, ControlDark for cut, WindowText for handles. No fixed RGB except preview bg.
3. `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`
   - Replace TrimChanged slider logic with timeline callbacks (same clamp).
   - ExportCoreAsync: set `Progress.Maximum=100`, report `Progress.Value=p` + `PercentText`. Enable CancelButton -> `_cts?.Cancel()`. Disable only export inputs, not RootGrid.
   - Keep partial-file cleanup, FFmpeg-missing guard, GIF-large warning unchanged.
4. `src/ViewportCap.App/App.xaml` + `DESIGN.md`
   - Add spacing/radius tokens only. No brush hue changes.
5. Tests
   - Core: trim clamp (gap, bounds, typed parse). App.Tests: export Cancel leaves no partial, progress 0-100 monotonic smoke via fake IExporter.

## Non-goals

- No ETA (needs speed averaging in FfmpegProgressParser — deferred).
- No touch gestures, no preview overlay, no modal dialog, no new NuGet deps.
- No narrator custom patterns beyond existing automation names; manual narrator check required.

## Self-review

- No TBD/TODO. Architecture matches features. Single-plan scope. Ambiguity fixed: percent is 0-100 integer from existing parser; Cancel aborts export and keeps temp file (current OnClosing behavior for busy stays).
