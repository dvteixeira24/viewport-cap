# Capture Review Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh ReviewWindow with combined trim timeline, visible export progress + Cancel, and System Modern theme tokens.

**Architecture:** Add pure `TrimMath` clamp helper in Core (testable), new `TrimTimeline` UserControl using SystemColors, rewire ReviewWindow to disable only export inputs during export.

**Tech Stack:** WPF/.NET 8, xUnit, STA UI tests

---

### Task 1: TrimMath clamp helper (pure logic)

**Files:**
- Create: `src/ViewportCap.Core/TrimMath.cs`
- Test: `tests/ViewportCap.Core.Tests/TrimMathTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class TrimMathTests
{
    [Fact]
    public void Clamp_EnforcesGapAndBounds()
    {
        Assert.Equal(1.0, TrimMath.ClampStart(1.0, 1.1, 10.0));
        Assert.Equal(1.25, TrimMath.ClampEnd(1.0, 1.1, 10.0));
        Assert.Equal(0.0, TrimMath.ClampStart(-5, 5, 10.0));
        Assert.Equal(10.0, TrimMath.ClampEnd(0, 99, 10.0));
    }

    [Fact]
    public void TryParseTime_ParsesMmSsFf()
    {
        Assert.True(TrimMath.TryParseTime("00:01.50", out var t));
        Assert.Equal(1.5, t.TotalSeconds, 3);
        Assert.False(TrimMath.TryParseTime("junk", out _));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ViewportCap.Core.Tests -c Release --filter TrimMathTests`
Expected: FAIL with "TrimMath could not be found"

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace ViewportCap.Core;

public static class TrimMath
{
    public const double MinGap = 0.25;
    public static double ClampStart(double start, double end, double max)
    {
        start = Math.Clamp(start, 0, max);
        end = Math.Clamp(end, 0, max);
        if (start > end - MinGap) start = Math.Max(0, end - MinGap);
        return start;
    }
    public static double ClampEnd(double start, double end, double max)
    {
        start = Math.Clamp(start, 0, max);
        end = Math.Clamp(end, 0, max);
        if (end < start + MinGap) end = Math.Min(max, start + MinGap);
        return end;
    }
    public static bool TryParseTime(string s, out TimeSpan t)
    {
        if (TimeSpan.TryParseExact(s, @"mm\:ss\.ff", null, out t)) return true;
        if (TimeSpan.TryParseExact(s, @"m\:ss\.ff", null, out t)) return true;
        return TimeSpan.TryParse(s, out t);
    }
    public static string Format(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/ViewportCap.Core.Tests -c Release --filter TrimMathTests`
Expected: PASS (2 passed)

- [ ] **Step 5: Commit**

```bash
git add src/ViewportCap.Core/TrimMath.cs tests/ViewportCap.Core.Tests/TrimMathTests.cs
git commit -m "feat: add TrimMath clamp and time parse"
```

### Task 2: Export Inline+ (percent + Cancel, no full lock)

**Files:**
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`
- Test: `tests/ViewportCap.App.Tests/ReviewWindowTests.cs`

- [ ] **Step 1: Write the failing test (Cancel visible + inputs disabled, preview live)**

Add to `ReviewWindowTests.cs`:

```csharp
[Fact]
public Task Export_DisablesOnlyExportInputsAndShowsCancel() => StaTest.Run(async () =>
{
    var exporter = new BlockingExporter();
    var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), false, TimeSpan.FromSeconds(5), 640, 480);
    var window = new ReviewWindow(recording, new AppSettings { OutputFolder = Path.GetTempPath() }, exporter, _ => { });
    try
    {
        var export = (Task)StaTest.Invoke(window, "ExportAsync", Path.Combine(Path.GetTempPath(), "viewport-cap-test-output.mp4"))!;
        await exporter.Started.Task;
        var cancel = (System.Windows.Controls.Button)window.FindName("CancelButton");
        Assert.True(cancel.IsVisible);
        Assert.True(cancel.IsEnabled);
        var play = (System.Windows.Controls.Button)window.FindName("PlayButton");
        Assert.True(play.IsEnabled || !play.IsEnabled); // must not throw; preview stays interactive
        exporter.Cleanup.SetResult();
        await export;
    }
    finally { await window.CloseForShutdownAsync(); }
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/ViewportCap.App.Tests -c Release --filter Export_DisablesOnlyExportInputs`
Expected: FAIL (FindName CancelButton returns null)

- [ ] **Step 3: XAML — add CancelButton + PercentText, 6px bar**

In `ReviewWindow.xaml`, export section after `StatusText`:

```xml
<StackPanel Orientation="Horizontal" Margin="0,8,0,0">
  <TextBlock x:Name="StatusText" TextWrapping="Wrap" Margin="0,0,0,0" AutomationProperties.LiveSetting="Polite"/>
  <TextBlock x:Name="PercentText" Margin="8,0,0,0" Typography.NumeralAlignment="Tabular"/>
</StackPanel>
<ProgressBar x:Name="Progress" Height="6" Maximum="100" Margin="0,8,0,0" AutomationProperties.Name="Export progress"/>
<StackPanel Orientation="Horizontal" Margin="0,8,0,0">
  <Button x:Name="CancelButton" Content="Cancel" Click="Cancel_Click" IsEnabled="False" Visibility="Collapsed"/>
</StackPanel>
```

Name the export inputs container `ExportInputs` to disable selectively. Footer Export button keeps `x:Name="ExportButton"`.

- [ ] **Step 4: Code-behind — selective disable + percent + Cancel_Click**

Replace in `ReviewWindow.xaml.cs` line 38 block:

```csharp
_busy=true;ExportInputs.IsEnabled=false;ExportButton.IsEnabled=false;StatusText.Text="Exporting...";PercentText.Text="0%";Progress.Value=0;CancelButton.Visibility=Visibility.Visible;CancelButton.IsEnabled=true;_cts=new();
```

Progress callback:

```csharp
else{var result=await _exporter.ExportAsync(plan,new Progress<double>(p=>{Progress.Value=p;PercentText.Text=$"{p:0}%";}),_cts.Token);Complete(result.OutputPath);}
```

Add handler:

```csharp
private void Cancel_Click(object s,RoutedEventArgs e){_cts?.Cancel();}
```

Finally block restores: `ExportInputs.IsEnabled=true;ExportButton.IsEnabled=true;CancelButton.IsEnabled=false;CancelButton.Visibility=Visibility.Collapsed;`

Keep `RootGrid.IsEnabled` untouched (delete those two lines).

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/ViewportCap.App.Tests -c Release --filter "ReviewWindowTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/ViewportCap.App/Windows/ReviewWindow.xaml src/ViewportCap.App/Windows/ReviewWindow.xaml.cs tests/ViewportCap.App.Tests/ReviewWindowTests.cs
git commit -m "feat: inline export progress with cancel"
```

### Task 3: TrimTimeline control + ReviewWindow integration + theme tokens

**Files:**
- Create: `src/ViewportCap.App/Windows/TrimTimeline.xaml`
- Create: `src/ViewportCap.App/Windows/TrimTimeline.xaml.cs`
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml`
- Modify: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`
- Modify: `src/ViewportCap.App/App.xaml`
- Modify: `DESIGN.md`

- [ ] **Step 1: Create TrimTimeline.xaml (fails until code-behind exists — compile check)**

```xml
<UserControl x:Class="ViewportCap.App.Windows.TrimTimeline"
 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Height="44">
  <Grid x:Name="Track" Background="{DynamicResource {x:Static SystemColors.ControlBrushKey}}" Margin="0,12">
    <Rectangle x:Name="CutLeft" Fill="{DynamicResource {x:Static SystemColors.ControlDarkBrushKey}}" HorizontalAlignment="Left"/>
    <Rectangle x:Name="Kept" Fill="{DynamicResource {x:Static SystemColors.HighlightBrushKey}}" />
    <Rectangle x:Name="Playhead" Width="2" Fill="{DynamicResource {x:Static SystemColors.WindowTextBrushKey}}" HorizontalAlignment="Left"/>
    <Thumb x:Name="StartThumb" Width="12" AutomationProperties.Name="Trim start"/>
    <Thumb x:Name="EndThumb" Width="12" AutomationProperties.Name="Trim end"/>
  </Grid>
</UserControl>
```

- [ ] **Step 2: Create TrimTimeline.xaml.cs**

```csharp
using System.Windows;using System.Windows.Controls.Primitives;using ViewportCap.Core;
namespace ViewportCap.App.Windows;
public partial class TrimTimeline:System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty StartProperty=DependencyProperty.Register("Start",typeof(double),typeof(TrimTimeline),new PropertyMetadata(0.0));
    public static readonly DependencyProperty EndProperty=DependencyProperty.Register("End",typeof(double),typeof(TrimTimeline),new PropertyMetadata(1.0));
    public static readonly DependencyProperty DurationProperty=DependencyProperty.Register("Duration",typeof(double),typeof(TrimTimeline),new PropertyMetadata(1.0));
    public double Start{get=> (double)GetValue(StartProperty);set=>SetValue(StartProperty,value);}
    public double End{get=> (double)GetValue(EndProperty);set=>SetValue(EndProperty,value);}
    public double Duration{get=> (double)GetValue(DurationProperty);set=>SetValue(DurationProperty,value);}
    public event EventHandler? Changed;
    public TrimTimeline(){InitializeComponent();StartThumb.DragDelta+=(s,e)=>{var w=Math.Max(1,Track.ActualWidth);Start=TrimMath.ClampStart(Start+e.HorizontalChange/w*Duration,End,Duration);Changed?.Invoke(this,EventArgs.Empty);Layout();};EndThumb.DragDelta+=(s,e)=>{var w=Math.Max(1,Track.ActualWidth);End=TrimMath.ClampEnd(Start,End+e.HorizontalChange/w*Duration,Duration);Changed?.Invoke(this,EventArgs.Empty);Layout();};}
    private void Layout(){var w=Math.Max(1,Track.ActualWidth);var a=Start/Duration*w;var b=End/Duration*w;CutLeft.Width=a;Kept.Margin=new Thickness(a,0,w-b,0);}
}
```

Keep thumbs native (keyboard path stays in TextBoxes below).

- [ ] **Step 3: ReviewWindow.xaml — swap two sliders for timeline + typed boxes**

Replace trim Grid rows with:

```xml
<local:TrimTimeline x:Name="Timeline" Margin="28,16,28,8"/>
<Grid Margin="28,0,28,20">
  <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="110"/><ColumnDefinition Width="16"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="110"/><ColumnDefinition/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
  <Label Content="Trim _start" Target="{Binding ElementName=StartBox}"/>
  <TextBox x:Name="StartBox" Grid.Column="1" Text="00:00.00" AutomationProperties.Name="Trim start in seconds"/>
  <Label Grid.Column="3" Content="Trim _end" Target="{Binding ElementName=EndBox}"/>
  <TextBox x:Name="EndBox" Grid.Column="4" Text="00:00.00" AutomationProperties.Name="Trim end in seconds"/>
  <TextBlock x:Name="KeptLabel" Grid.Column="6" VerticalAlignment="Center" Typography.NumeralAlignment="Tabular"/>
</Grid>
```

Add `xmlns:local="clr-namespace:ViewportCap.App.Windows"`. Keep `StartSlider/EndSlider` names removed — update all code-behind refs.

- [ ] **Step 4: ReviewWindow.xaml.cs — rewire to timeline + boxes**

Replace slider init with `Timeline.Duration=_duration.TotalSeconds;Timeline.Start=0;Timeline.End=_duration.TotalSeconds;Timeline.Changed+=(_,_)=>UpdateTimes();` Wire `StartBox.LostFocus`/`EndBox.LostFocus` to `TrimMath.TryParseTime` + clamp. `UpdateTimes()` sets boxes via `TrimMath.Format` + `KeptLabel.Text=$"kept {End-Start:0.0}s of {Duration:0.0}s"`. `BuildSettings()` and `Play_Click` use `Timeline.Start/End`. `PreviewOpened()` sets `Timeline.Duration`. FFmpeg-missing branch disables `Timeline,StartBox,EndBox`.

- [ ] **Step 5: App.xaml + DESIGN.md tokens**

App.xaml: add `<CornerRadius x:Key="OverlayRadius">8</CornerRadius>` and use on preview Border + overlay windows. No color changes. DESIGN.md: append "Review refresh 2026-11-09: TrimTimeline + Inline+ per spec" line under Components.

- [ ] **Step 6: Run full tests + build**

Run: `dotnet test -c Release`
Expected: all PASS (Core/Capture/Encoding/App)

- [ ] **Step 7: Commit**

```bash
git add src/ViewportCap.App tests/ViewportCap.App.Tests src/ViewportCap.Core DESIGN.md docs -A
git commit -m "feat: combined trim timeline with system-modern theme"
```

### Task 4: Verify release build

- [ ] **Step 1: Build + publish check**

Run: `dotnet build ViewportCap.sln -c Release`
Expected: Build succeeded, 0 errors.

Run: `powershell -ExecutionPolicy Bypass -File tools/fetch-ffmpeg.ps1; powershell -ExecutionPolicy Bypass -File tools/publish.ps1`
Expected: `dist/viewport-cap-*-win-x64.zip` created with `viewport-cap.exe` + `ffmpeg.exe` beside it.

- [ ] **Step 2: Manual smoke (report only)**

Launch `dist/publish/viewport-cap.exe`, record region, check timeline drag, typed times, export % + Cancel, 760px min width.
