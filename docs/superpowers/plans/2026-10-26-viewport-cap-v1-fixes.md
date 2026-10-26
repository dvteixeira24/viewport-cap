# viewport-cap v1 Review Fixes

**Branch:** `feat/v1`
**Scope:** Fix the review findings without redesigning the app.
**Baseline:** 66 unit tests pass; release build has 0 errors and 3 warnings.

## Rules

- Work on `feat/v1`.
- Make minimal, targeted changes.
- Do not rewrite working code or rename public APIs.
- Add a regression test for every testable fix.
- Run the relevant test/build after each task.
- Commit after each task with the exact message shown.
- Do not merge to `main`.

## Task 1: Preserve video when exporting audio

**Problem:** `FfmpegArgumentBuilder` adds only `-map 0:a:0?` when audio is kept. An explicit map disables FFmpeg's automatic video selection, so MP4/WebM exports contain only audio. Reproduced with the bundled FFmpeg.

**Files:**
- Modify: `src/ViewportCap.Encoding/FfmpegArgumentBuilder.cs`
- Test: `tests/ViewportCap.Encoding.Tests/FfmpegArgumentBuilderTests.cs`

### Step 1: Write the failing tests

Add these tests to `FfmpegArgumentBuilderTests`:

```csharp
[Fact]
public void Mp4_WithAudio_MapsVideoAndAudio()
{
    var settings = Settings(hasAudio: true, mute: false);
    var args = FfmpegArgumentBuilder.Build(settings, @"C:\out\clip.mp4", AllCapabilities);

    Assert.Contains("0:v:0", args);
    Assert.Contains("0:a:0?", args);
    Assert.DoesNotContain("-an", args);
}

[Fact]
public void Gif_WithAudio_StillDropsAudio()
{
    var settings = Settings(format: OutputFormat.Gif, frameRate: 15, hasAudio: true, mute: false);
    var args = FfmpegArgumentBuilder.Build(settings, @"C:\out\clip.gif", AllCapabilities);

    Assert.Contains("-an", args);
    Assert.DoesNotContain("0:a:0?", args);
}
```

Run:
```powershell
dotnet test tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj -c Debug --filter "FullyQualifiedName~Mp4_WithAudio_MapsVideoAndAudio|FullyQualifiedName~Gif_WithAudio_StillDropsAudio"
```

Expected: `Mp4_WithAudio_MapsVideoAndAudio` fails because `0:v:0` is missing. The GIF test may pass.

### Step 2: Fix the mapping

In `FfmpegArgumentBuilder.Build`, replace the audio branch with:

```csharp
a.Add("-map");
a.Add("0:v:0");
if (audio)
{
    a.Add("-map");
    a.Add("0:a:0?");
}
else
{
    a.Add("-an");
}
```

The final order must be `-map 0:v:0` then `-map 0:a:0?` when audio is kept, and `-map 0:v:0` then `-an` otherwise.

### Step 3: Run tests

```powershell
dotnet test tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj -c Debug
```

Expected: all tests pass.

### Step 4: Commit

```powershell
git add src/ViewportCap.Encoding/FfmpegArgumentBuilder.cs tests/ViewportCap.Encoding.Tests/FfmpegArgumentBuilderTests.cs
git commit -m "fix: map video stream when exporting audio"
```

## Task 2: Make the review window close after a successful export

**Problem:** `ReviewWindow.Complete()` calls `Close()` while `_busy` is still true. `OnClosing()` treats that as an active export, shows the cancel prompt, and cancels the close. After a successful export the window remains open in `Reviewing`, so the hotkey is ignored.

**Files:**
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`

### Step 1: Change `Complete`

Replace:

```csharp
private void Complete(string path){_deleted=true;TryDelete();_exported(path);Close();}
```

with:

```csharp
private void Complete(string path)
{
    _deleted = true;
    _busy = false;
    TryDelete();
    _exported(path);
    Close();
}
```

### Step 2: Guard `OnClosing`

At the very start of `OnClosing`, add:

```csharp
if (_deleted)
    return;
```

The existing `if (_busy)` branch stays after that guard.

### Step 3: Build and manually check

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
```

Manual: export a clip; the review window must close immediately with no "Cancel the export and close?" prompt, and the toast must appear.

### Step 4: Commit

```powershell
git add src/ViewportCap.App/Windows/ReviewWindow.xaml.cs
git commit -m "fix: close review after successful export"
```

## Task 3: Apply changed settings

**Problem:** Changing the hotkey in Settings saves the value but never registers it. The old hotkey stays active until restart. Start with Windows changes are also not applied until restart.

**Files:**
- Modify: `src/ViewportCap.App/App.xaml.cs`

### Step 1: Apply hotkey and startup values in the Settings callback

In `ShowSettings`, replace the callback passed to `SettingsWindow` with:

```csharp
updated =>
{
    _settings = updated;
    _tray?.UpdateHotkey(updated.Hotkey);
    _tray?.UpdateStartWithWindows(updated.StartWithWindows);

    try
    {
        _startup?.SetEnabled(updated.StartWithWindows);
    }
    catch (Exception ex)
    {
        FileLog.Error("Could not update Start with Windows.", ex);
        MessageBox.Show(
            "Could not update the Start with Windows setting.",
            "viewport-cap",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    if (_hotkey is not null &&
        HotkeyGesture.TryParse(updated.Hotkey, out var gesture) &&
        !_hotkey.TryRegister(gesture, OnCapture))
    {
        MessageBox.Show(
            $"The hotkey {gesture} is already in use.",
            "viewport-cap",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
```

### Step 2: Warn when the startup hotkey is unavailable

Replace:

```csharp
if(HotkeyGesture.TryParse(_settings.Hotkey,out var gesture))_hotkey.TryRegister(gesture,OnCapture);
```

with:

```csharp
if (HotkeyGesture.TryParse(_settings.Hotkey, out var gesture))
{
    if (!_hotkey.TryRegister(gesture, OnCapture))
    {
        var message = $"The hotkey {gesture} is already in use. Choose another hotkey in Settings.";
        FileLog.Warn(message);
        MessageBox.Show(message, "viewport-cap", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
```

### Step 3: Guard startup registry access

Wrap the startup `StartupService.SetEnabled` call in `OnStartup` in `try/catch` and log the error, matching the Settings callback.

### Step 4: Build and manually check

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
```

Manual:
1. Open Settings and choose a different hotkey, then save.
2. Confirm the new hotkey immediately opens the overlay and the old one no longer does.
3. Toggle Start with Windows in Settings and confirm the tray checkmark and the HKCU Run value update immediately.

### Step 5: Commit

```powershell
git add src/ViewportCap.App/App.xaml.cs
git commit -m "fix: apply hotkey and startup settings immediately"
```

## Task 4: Add a global UI exception handler

**Problem:** Several async-void paths can throw and terminate the app.

**Files:**
- Modify: `src/ViewportCap.App/App.xaml.cs`

### Step 1: Add the handler

At the top of `OnStartup`, after `base.OnStartup(e);`, add:

```csharp
DispatcherUnhandledException += (_, args) =>
{
    FileLog.Error("Unhandled UI exception.", args.Exception);
    MessageBox.Show(
        $"viewport-cap hit an error:\n\n{args.Exception.Message}",
        "viewport-cap",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    args.Handled = true;
};
```

### Step 2: Harden the MP4 copy path

In `ReviewWindow.ExportAsync`, in the `_exporter == null` copy branch, wrap the copy and create the destination directory:

```csharp
try
{
    var directory = Path.GetDirectoryName(plan.OutputPath);
    if (!string.IsNullOrWhiteSpace(directory))
        Directory.CreateDirectory(directory);

    File.Copy(r.SourcePath, plan.OutputPath, true);
    Complete(plan.OutputPath);
}
catch (Exception ex)
{
    FileLog.Error("MP4 copy failed.", ex);
    MessageBox.Show($"Export failed:\n\n{ex.Message}", "viewport-cap");
}
```

Ensure `using System.IO;` is present in `ReviewWindow.xaml.cs`.

### Step 3: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/App.xaml.cs src/ViewportCap.App/Windows/ReviewWindow.xaml.cs
git commit -m "fix: handle UI exceptions and MP4 copy failures"
```

## Task 5: Keep temp recordings on failure and add recording guards

**Problem:** Capture failures delete the partial recording. Low-disk and 10-minute warnings are not implemented.

**Files:**
- Modify: `src/ViewportCap.App/Services/CaptureFlowController.cs`
- Modify: `src/ViewportCap.App/Windows/RecordingHudWindow.xaml.cs` only if needed

### Step 1: Do not delete on failure

Split the catch blocks:

```csharp
catch (OperationCanceledException)
{
    try { await _capture.CancelAsync(); } catch { }
    GoIdle();
}
catch (Exception ex)
{
    FileLog.Error("Capture failed.", ex);
    MessageBox.Show($"Capture failed:\n\n{ex.Message}", "viewport-cap");
    try { await _capture.CancelAsync(deleteFile: false); } catch { }
    GoIdle();
}
```

### Step 2: Add the recording guard timer

After `_hud.Start();`, add a `DispatcherTimer` with a 5-second interval:

- Read `DriveInfo(Path.GetPathRoot(path)!).AvailableFreeSpace`.
- If below 500 MB: `_hud.ShowWarning("Low disk space.")`.
- If below 250 MB: show a warning, `_stop.TrySetResult(true)`, and stop the timer.
- If elapsed since recording start exceeds 10 minutes and no warning was shown: show "Recording is longer than 10 minutes."
- Stop the timer in a `finally` around `await _stop.Task`.

Add `using System.Windows.Threading;` and `using System.IO;`.

### Step 3: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Services/CaptureFlowController.cs
git commit -m "fix: keep failed recordings and add recording guards"
```

## Task 6: Fix remaining high-impact UI gaps

**Files:**
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`
- Modify: `src/ViewportCap.App/Windows/RecordingHudWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/SettingsWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/SettingsWindow.xaml.cs`
- Modify: `src/ViewportCap.App/Windows/ToastWindow.xaml.cs`

### Step 1: Disable unsupported review controls when FFmpeg is missing

In the `exporter == null` branch of the `ReviewWindow` constructor, set:

```csharp
FormatBox.ItemsSource = new[] { OutputFormat.Mp4 };
FormatBox.SelectedItem = OutputFormat.Mp4;
QualityBox.IsEnabled = false;
FpsBox.IsEnabled = false;
ScaleBox.IsEnabled = false;
StartSlider.IsEnabled = false;
EndSlider.IsEnabled = false;
KeepAudioBox.IsEnabled = false;
KeepAudioBox.IsChecked = result.HasAudio;
StatusText.Text = "FFmpeg not found; untrimmed MP4 only.";
```

### Step 2: Keep the recording HUD from stealing focus

Add `ShowActivated="False"` to `RecordingHudWindow.xaml` on the `Window` element.

### Step 3: Add audio device pickers

Add two combo boxes to `SettingsWindow.xaml`:

```xml
<TextBlock Text="Microphone"/>
<ComboBox x:Name="MicrophoneBox" Margin="0,4,0,14"/>
<TextBlock Text="System audio output"/>
<ComboBox x:Name="SystemAudioBox" Margin="0,4,0,14"/>
```

In `SettingsWindow.xaml.cs`:

- Add `using ScreenRecorderLib;`.
- Add a private record `AudioDeviceOption(string Name, string? DeviceName)`.
- Populate each combo with `new AudioDeviceOption("Default device", null)` plus `Recorder.GetSystemAudioCaptureDevices()` / `Recorder.GetSystemAudioLoopbackDevices()`.
- Select the saved device by `DeviceName`, falling back to the default.
- On save, set `MicrophoneDeviceName` and `SystemAudioDeviceName` from the selected options.
- Catch enumeration failures and keep the default option.

### Step 4: Harden the toast clipboard action

Wrap `Clipboard.SetText(_path)` in `try/catch` and show a short message or ignore a busy clipboard:

```csharp
try { Clipboard.SetText(_path); }
catch (Exception ex) { FileLog.Warn($"Could not copy path: {ex.Message}"); }
```

Add `using ViewportCap.App.Services;`.

### Step 5: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Windows/ReviewWindow.xaml.cs src/ViewportCap.App/Windows/RecordingHudWindow.xaml src/ViewportCap.App/Windows/SettingsWindow.xaml src/ViewportCap.App/Windows/SettingsWindow.xaml.cs src/ViewportCap.App/Windows/ToastWindow.xaml.cs
git commit -m "fix: close remaining review and settings gaps"
```

## Task 7: Verify and republish

### Step 1: Tests and build

```powershell
dotnet test ViewportCap.sln -c Release --filter "Category!=Integration"
dotnet build ViewportCap.sln -c Release
```

Expected: all tests pass, build has 0 errors.

### Step 2: Manual checks

1. Record with microphone/system audio; export MP4 and WebM. Confirm the outputs contain both video and audio streams (e.g. `ffmpeg -i <file>` shows one video and one audio stream).
2. Export successfully; Review closes immediately and the toast appears.
3. Change the hotkey; the new one works immediately and the old one no longer does.
4. Toggle Start with Windows; the HKCU Run value and tray checkmark update immediately.
5. Delete the output folder, then export the untrimmed MP4 copy path; the app recreates the folder or shows an error without crashing.
6. Record a long clip and confirm the HUD warns after 10 minutes; simulate low disk if possible.
7. Confirm the HUD does not steal focus from the app being recorded.

### Step 3: Republish

```powershell
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

Expected: `dist/viewport-cap-0.1.0-win-x64.zip` is rebuilt.

### Step 4: Commit

```powershell
git add .
git commit -m "chore: rebuild v1 package after review fixes"
```

## Final report

Report:

- Branch and commits.
- Test/build results.
- The exact audio export command output showing both streams.
- Manual checks performed and any that could not be performed.
- Any remaining deviations from the review or spec.
