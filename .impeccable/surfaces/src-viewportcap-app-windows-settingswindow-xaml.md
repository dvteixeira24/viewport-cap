---
version: 1
slug: "src-viewportcap-app-windows-settingswindow-xaml"
primary_target: "src/ViewportCap.App/Windows/SettingsWindow.xaml"
related_targets: ["src/ViewportCap.App/Windows/ReviewWindow.xaml","src/ViewportCap.App/Windows/RecordingHudWindow.xaml","src/ViewportCap.App/Windows/CountdownWindow.xaml","src/ViewportCap.App/Windows/ToastWindow.xaml","src/ViewportCap.App/Windows/RegionOverlayWindow.xaml"]
---

# Windows interface refresh

Scope: all WPF windows. Mode: Operate. The user confirmed all-window scope and familiar Windows styling. This is a native Windows app, not one of the design launcher's supported native mobile targets.

## Direction contract

THESIS: Familiar Windows utility, with recording content ahead of chrome. Replace the cramped dark form with a readable, task-oriented native interface; retain native window management and keyboard controls.

OWN-WORLD: Segoe UI, light system surfaces, system foregrounds and selection color, 4-pixel control corners, 36-pixel button height, restrained separators. System brushes preserve Windows high-contrast compatibility. Red appears only as the recording indicator.

STORY: Configure capture in grouped settings; select a region; see the countdown and recording state; trim in a large preview; choose labeled export settings and export.

FIRST VIEWPORT: Settings has a concise header, scrollable Capture/Audio/Startup sections, and a fixed action footer. Review has a large preview above two labeled trim rows, four labeled export fields, and a fixed action footer. Small overlays clearly separate state from actions.

FORM: The user's pinned Windows-native direction overrides the random assignment, seed:f2537c6ecab7356e. Standard native controls, not a web or artifact metaphor. Signature: consistent reserved action footers, keeping the completion action in the same location throughout the workflow.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance.

No new raster assets are required. Preserve all existing named controls, event handlers, capture geometry, confirmation flows, and export logic. Verify normal/minimum windows and long paths/device names. The native-only change does not use the HTML/CSS detector.
