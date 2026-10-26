# viewport-cap v1 Final Fixes

**Branch:** `feat/v1`
**Scope:** Close the remaining review findings without redesigning the app.

## Rules

- Work on `feat/v1`.
- Minimal targeted changes; preserve public APIs.
- Build after each task.
- Commit after each task with the exact message shown.
- Do not merge to `main`.

## Task 1: Add the preview fallback

**Problem:** When `MediaElement` cannot decode the fragmented MP4, the user only sees a status message. The spec requires an "Open in default player" fallback.

**Files:**
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`

### Step 1: Add the button

In `ReviewWindow.xaml`, replace:

```xml
<Button x:Name="PlayButton" Content="Play" Click="Play_Click" HorizontalAlignment="Left"/>
```

with:

```xml
<StackPanel Orientation="Horizontal">
  <Button x:Name="PlayButton" Content="Play" Click="Play_Click"/>
  <Button x:Name="OpenPreviewButton"
          Content="Open in default player"
          Click="OpenPreview_Click"
          Visibility="Collapsed"/>
</StackPanel>
```

### Step 2: Wire the fallback

In `ReviewWindow.xaml.cs`, add `using System.Diagnostics;`.

Change `Preview_MediaFailed` to also show the button:

```csharp
private void Preview_MediaFailed(object s, ExceptionRoutedEventArgs e)
{
    StatusText.Text = "Preview unavailable. Timecode trimming remains available.";
    PlayButton.IsEnabled = false;
    OpenPreviewButton.Visibility = Visibility.Visible;
}
```

Add:

```csharp
private void OpenPreview_Click(object s, RoutedEventArgs e)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = _result.FilePath,
        UseShellExecute = true
    });
}
```

### Step 3: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Windows/ReviewWindow.xaml src/ViewportCap.App/Windows/ReviewWindow.xaml.cs
git commit -m "fix: add preview fallback"
```

## Task 2: Cancel an export when the app exits

**Problem:** `App` calls `_flow.Cancel()`, which cancels the capture token but not the review window's export token. An FFmpeg child can outlive the app.

**Files:**
- Modify: `src/ViewportCap.App/Services/CaptureFlowController.cs`
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`
- Modify: `src/ViewportCap.App/App.xaml.cs`

### Step 1: Expose export cancellation

In `ReviewWindow.xaml.cs`, add:

```csharp
public void CancelExport()
{
    _cts?.Cancel();
}
```

### Step 2: Track the review window in the flow

In `CaptureFlowController.cs`, add:

```csharp
private ReviewWindow? _review;
```

When creating the review window:

```csharp
var review = new ReviewWindow(result, settings, _exporter, _showToast);
_review = review;
review.Closed += (_, _) =>
{
    _review = null;
    if (_state.State == RecordingState.Reviewing)
        GoIdle();
};
review.Show();
```

Update `Cancel()`:

```csharp
public void Cancel()
{
    _cts?.Cancel();
    _stop?.TrySetResult(false);
    _review?.CancelExport();
}
```

### Step 3: Cancel on application exit

In `App.xaml.cs`, add `_flow?.Cancel();` at the start of `OnExit`, before disposing services.

### Step 4: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Services/CaptureFlowController.cs src/ViewportCap.App/Windows/ReviewWindow.xaml.cs src/ViewportCap.App/App.xaml.cs
git commit -m "fix: cancel export on application exit"
```

## Task 3: Guard the countdown audio save

**Problem:** `_store.Save(settings)` in the countdown audio callback can throw on a full disk, locked settings file, or permission failure.

**Files:**
- Modify: `src/ViewportCap.App/Services/CaptureFlowController.cs`

### Step 1: Wrap the save

Replace the callback body:

```csharp
mode =>
{
    settings.AudioMode = mode;
    _store.Save(settings);
});
```

with:

```csharp
mode =>
{
    try
    {
        settings.AudioMode = mode;
        _store.Save(settings);
    }
    catch (Exception ex)
    {
        FileLog.Error("Could not save the audio mode.", ex);
    }
});
```

### Step 2: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Services/CaptureFlowController.cs
git commit -m "fix: guard countdown audio settings save"
```

## Task 4: Disable review controls during export

**Problem:** Export controls remain interactive while an export is running.

**Files:**
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`

### Step 1: Name the root grid

Change the root element in `ReviewWindow.xaml`:

```xml
<Grid Margin="16">
```

to:

```xml
<Grid x:Name="RootGrid" Margin="16">
```

### Step 2: Toggle it during export

In `ExportAsync`, where `_busy = true` is set, add:

```csharp
RootGrid.IsEnabled = false;
StatusText.Text = "Exporting...";
```

In the `finally` block, add:

```csharp
RootGrid.IsEnabled = true;
StatusText.Text = "Ready.";
```

### Step 3: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Windows/ReviewWindow.xaml src/ViewportCap.App/Windows/ReviewWindow.xaml.cs
git commit -m "fix: disable review controls during export"
```

## Task 5: Add the Settings logs shortcut

**Problem:** The approved design includes an "Open logs folder" action in Settings; it is missing.

**Files:**
- Modify: `src/ViewportCap.App/Windows/SettingsWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/SettingsWindow.xaml.cs`

### Step 1: Add the button

In `SettingsWindow.xaml`, inside the bottom button `StackPanel`, add this before Cancel:

```xml
<Button Content="Open logs" Click="OpenLogs_Click"/>
```

### Step 2: Implement it

In `SettingsWindow.xaml.cs`, add `using System.Diagnostics;` and:

```csharp
private void OpenLogs_Click(object sender, RoutedEventArgs e)
{
    Directory.CreateDirectory(AppPaths.LogsDirectory);
    Process.Start(new ProcessStartInfo
    {
        FileName = AppPaths.LogsDirectory,
        UseShellExecute = true
    });
}
```

### Step 3: Build and commit

```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
git add src/ViewportCap.App/Windows/SettingsWindow.xaml src/ViewportCap.App/Windows/SettingsWindow.xaml.cs
git commit -m "fix: add settings logs shortcut"
```

## Task 6: Verify and republish

```powershell
dotnet test ViewportCap.sln -c Release --filter "Category!=Integration"
dotnet build ViewportCap.sln -c Release
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

Manual checks:

1. Export a clip successfully; Review closes immediately and the toast appears.
2. If the preview fails, the "Open in default player" button opens the temp file.
3. Exit from the tray while an export is running; the app exits and no FFmpeg process remains.
4. Toggle audio on the countdown; recording continues even if the settings save fails.
5. Open Settings and confirm the logs shortcut opens `%LOCALAPPDATA%\ViewportCap\logs`.

Commit the rebuilt package:

```powershell
git add .
git commit -m "chore: rebuild final v1 package"
```

## Final report

Report branch, commits, test/build results, zip path, manual checks performed, and anything that could not be verified.
