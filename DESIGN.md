---
name: viewport-cap
description: A familiar, readable Windows recording utility
colors:
  surface: Canvas
  text: CanvasText
  action: Highlight
  action-text: HighlightText
  chrome: ButtonFace
  border: ActiveBorder
  secondary-text: ThreeDDarkShadow
  divider: ThreeDLightShadow
  recording: "#C42B1C"
  preview: "#181818"
typography:
  heading:
    fontFamily: Segoe UI
    fontSize: 28px
    fontWeight: 600
  section:
    fontFamily: Segoe UI
    fontSize: 18px
    fontWeight: 600
  body:
    fontFamily: Segoe UI
    fontSize: 14px
    fontWeight: 400
  detail:
    fontFamily: Segoe UI
    fontSize: 12px
    fontWeight: 400
rounded:
  control: 4px
  overlay: 8px
spacing:
  compact: 8px
  group: 16px
  section: 24px
  window: 28px
components:
  button-primary:
    backgroundColor: "{colors.action}"
    textColor: "{colors.action-text}"
    rounded: "{rounded.control}"
    padding: 7px 16px
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.control}"
    padding: 7px 16px
---

# Design System: viewport-cap

## Overview

**Creative North Star: "Familiar Windows utility"**

The user requested a familiar Windows appearance across every window. The interface uses native WPF controls, restrained separators, readable grouping, and clear completion actions. Desktop operation matters more than decorative identity.

**Key Characteristics:**
- System-colored surfaces and selection, rather than a fixed dark palette.
- Native window management, keyboard navigation, and accessible field labels.
- Consistent action placement and compact floating overlays.

## Colors

The system color names in the frontmatter describe roles, not hard-coded RGB defaults. `App.xaml` uses `DynamicResource` references to the corresponding WPF `SystemColors` brush keys: Window, WindowText, Highlight, HighlightText, Control, ActiveBorder, ControlDarkDark, and ControlLight. Red marks active recording; the preview remains dark behind video.

**The System Pair Rule.** Pair Window with WindowText and Highlight with HighlightText. Do not restore fixed light text over native light dropdowns. These classic system brushes support system/high-contrast colors but do not implement automatic Windows app-mode dark theme switching.

## Typography

Segoe UI is the native UI voice. Window headings and section headings use the frontmatter's two semibold steps. Body controls inherit the window font; detail text is smaller. Recording time uses semibold 24-pixel type; countdowns use semibold 48-pixel type. Timecodes use tabular figures.

## Layout

Settings and review use generous window gutters, compact field groups, and fixed action footers. Settings scrolls vertically at constrained heights; review allocates remaining height to video. Review's export controls use four equal flexible columns. The minimum window sizes are 560×580 for Settings and 760×680 for review, in device-independent units.

## Elevation & Depth

Flat surfaces, system-colored footer/header regions, and thin borders provide hierarchy. Floating overlays use transparent outer windows with opaque rounded content. No custom shadows or entrance animations are used.

## Shapes

Controls use subtly rounded corners; floating overlays use the larger radius. Text boxes, checkboxes, sliders, scrollbars, and combo items retain their native WPF behavior and theme-driven parts. Native title bars remain OS-owned.

## Components

- **Buttons:** minimum 36-pixel height and 80-pixel width. Primary actions use paired system selection brushes. Hover, pressed, keyboard focus, and disabled states are provided by the shared template. Disabled chrome uses half opacity.
- **Fields:** text boxes and noneditable combo boxes share comfortable padding and minimum heights. Labels target their input and provide access keys. The combo template retains `PART_Popup`, selection content, a two-way dropdown toggle, and a scrollable item presenter; F4 opening is regression-tested.
- **Overlays:** recording state is written explicitly next to the red indicator; warning text wraps below actions. Export paths truncate visually and retain the full path in a tooltip and Copy path action.

## Do's and Don'ts

- **Do** reuse the application styles and system color pairs.
- **Do** preserve named controls, event handlers, keyboard operation, and existing recording/export safeguards.
- **Do** check constrained window sizes and long file/device names.
- **Don't** introduce a fixed dark foreground palette into native light controls.
- **Don't** add decorative icons or imagery to task-focused windows.
