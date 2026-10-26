# viewport-cap v1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a tray-first Windows app that records a free screen region from a global hotkey and exports MP4, WebM, or GIF after review.

**Architecture:** Four projects: `ViewportCap.Core` (pure domain, settings, planning, state machine), `ViewportCap.Encoding` (FFmpeg process wrapper), `ViewportCap.Capture` (ScreenRecorderLib adapter), and `ViewportCap.App` (WPF tray app). UI windows use code-behind and standard WPF controls; this is deliberate to keep the implementation mechanical and reliable. The approved spec is `docs/superpowers/specs/2026-10-26-viewport-cap-design.md`; where this plan names a concrete implementation detail, follow this plan.

**Tech Stack:** .NET 8, WPF, WinForms `NotifyIcon`, ScreenRecorderLib 7.0.1, FFmpeg 8.1 LGPL, xUnit 2.9.3, Windows 10 2004+.

---

## How to execute this plan

- Work from `D:\Code\viewport-cap`.
- Use Windows PowerShell for all commands.
- Create branch `feat/v1` at the start of Task 1.
- Complete tasks in order. Do not skip a failing test or a build failure.
- Commit after every task with the exact commit message shown.
- Do not rename projects, files, types, methods, or properties defined in earlier tasks. Later code depends on them.
- When a code block is marked `Create`, write the whole file. When marked `Modify`, replace the stated region exactly.
- The app is Windows-only. Do not add Linux/macOS support.
- The UI is intentionally plain WPF. Do not add a UI framework or MVVM package.

## File map

```
ViewportCap.sln
Directory.Build.props
.editorconfig
.gitignore
.github/workflows/ci.yml
src/
  ViewportCap.Core/
    AppSettings.cs
    AudioMode.cs
    CaptureBitrateCalculator.cs
    CaptureFailedEventArgs.cs
    CaptureRegion.cs
    CaptureRequest.cs
    CaptureResult.cs
    DragRect.cs
    ExportNaming.cs
    ExportPlan.cs
    ExportPlanner.cs
    HotkeyGesture.cs
    ICaptureEngine.cs
    IExporter.cs
    ISettingsStore.cs
    JsonSettingsStore.cs
    MonitorDescriptor.cs
    OutputFormat.cs
    QualityPreset.cs
    RecordingState.cs
    RecordingStateMachine.cs
    RegionMath.cs
    ReviewSettings.cs
    ScalePreset.cs
  ViewportCap.Encoding/
    ExportException.cs
    FfmpegArgumentBuilder.cs
    FfmpegCapabilities.cs
    FfmpegExporter.cs
    FfmpegLocator.cs
    FfmpegProbe.cs
    FfmpegProgressParser.cs
    IProcessRunner.cs
    ProcessResult.cs
    SystemProcessRunner.cs
  ViewportCap.Capture/
    CapturePreflight.cs
    ScreenRecorderCaptureEngine.cs
    ScreenRecorderOptionsFactory.cs
  ViewportCap.App/
    App.xaml
    App.xaml.cs
    app.manifest
    Assets/app.ico
    Services/
      AppPaths.cs
      CaptureFlowController.cs
      DisplayService.cs
      FileLog.cs
      HotkeyService.cs
      SingleInstanceService.cs
      StartupService.cs
      ToastService.cs
      TrayService.cs
      WindowAffinity.cs
    Windows/
      CountdownWindow.xaml
      CountdownWindow.xaml.cs
      RecordingHudWindow.xaml
      RecordingHudWindow.xaml.cs
      RegionOverlayWindow.xaml
      RegionOverlayWindow.xaml.cs
      RegionSelector.cs
      ReviewWindow.xaml
      ReviewWindow.xaml.cs
      SettingsWindow.xaml
      SettingsWindow.xaml.cs
      ToastWindow.xaml
      ToastWindow.xaml.cs
tests/
  ViewportCap.Core.Tests/
    AppSettingsTests.cs
    CaptureBitrateCalculatorTests.cs
    ExportNamingTests.cs
    ExportPlannerTests.cs
    HotkeyGestureTests.cs
    JsonSettingsStoreTests.cs
    RecordingStateMachineTests.cs
    RegionMathTests.cs
  ViewportCap.Encoding.Tests/
    FfmpegArgumentBuilderTests.cs
    FfmpegCapabilitiesTests.cs
    FfmpegExporterTests.cs
    FfmpegLocatorTests.cs
    FfmpegProgressParserTests.cs
tools/
  fetch-ffmpeg.ps1
  generate-icon.ps1
  publish.ps1
LICENSE
THIRD-PARTY-NOTICES.md
README.md
```

---

### Task 1: Scaffold solution

**Files:**
- Create: `ViewportCap.sln`
- Create: `.gitignore`
- Create: `.editorconfig`
- Create: `Directory.Build.props`
- Create: `.github/workflows/ci.yml`
- Create: `src/ViewportCap.Core/ViewportCap.Core.csproj`
- Create: `src/ViewportCap.Encoding/ViewportCap.Encoding.csproj`
- Create: `src/ViewportCap.Capture/ViewportCap.Capture.csproj`
- Create: `src/ViewportCap.App/ViewportCap.App.csproj`
- Create: `tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj`
- Create: `tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj`

- [ ] **Step 1: Create the branch**

Run:
```powershell
git checkout -b feat/v1
```

Expected: `Switched to a new branch 'feat/v1'`.

- [ ] **Step 2: Generate the projects**

Run:
```powershell
dotnet new sln -n ViewportCap
dotnet new classlib -n ViewportCap.Core -o src/ViewportCap.Core -f net8.0
dotnet new classlib -n ViewportCap.Encoding -o src/ViewportCap.Encoding -f net8.0
dotnet new classlib -n ViewportCap.Capture -o src/ViewportCap.Capture -f net8.0
dotnet new wpf -n ViewportCap.App -o src/ViewportCap.App -f net8.0
dotnet new xunit -n ViewportCap.Core.Tests -o tests/ViewportCap.Core.Tests -f net8.0
dotnet new xunit -n ViewportCap.Encoding.Tests -o tests/ViewportCap.Encoding.Tests -f net8.0
Remove-Item src/ViewportCap.Core/Class1.cs
Remove-Item src/ViewportCap.Encoding/Class1.cs
Remove-Item src/ViewportCap.Capture/Class1.cs
Remove-Item tests/ViewportCap.Core.Tests/UnitTest1.cs
Remove-Item tests/ViewportCap.Encoding.Tests/UnitTest1.cs
dotnet sln add src/ViewportCap.Core/ViewportCap.Core.csproj
dotnet sln add src/ViewportCap.Encoding/ViewportCap.Encoding.csproj
dotnet sln add src/ViewportCap.Capture/ViewportCap.Capture.csproj
dotnet sln add src/ViewportCap.App/ViewportCap.App.csproj
dotnet sln add tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj
dotnet sln add tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj
dotnet add src/ViewportCap.Encoding/ViewportCap.Encoding.csproj reference src/ViewportCap.Core/ViewportCap.Core.csproj
dotnet add src/ViewportCap.Capture/ViewportCap.Capture.csproj reference src/ViewportCap.Core/ViewportCap.Core.csproj
dotnet add src/ViewportCap.App/ViewportCap.App.csproj reference src/ViewportCap.Core/ViewportCap.Core.csproj
dotnet add src/ViewportCap.App/ViewportCap.App.csproj reference src/ViewportCap.Encoding/ViewportCap.Encoding.csproj
dotnet add src/ViewportCap.App/ViewportCap.App.csproj reference src/ViewportCap.Capture/ViewportCap.Capture.csproj
dotnet add tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj reference src/ViewportCap.Core/ViewportCap.Core.csproj
dotnet add tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj reference src/ViewportCap.Core/ViewportCap.Core.csproj
dotnet add tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj reference src/ViewportCap.Encoding/ViewportCap.Encoding.csproj
dotnet add src/ViewportCap.Capture/ViewportCap.Capture.csproj package ScreenRecorderLib --version 7.0.1
```

Expected: each command exits with code 0.

- [ ] **Step 3: Write `Directory.Build.props`**

Create:
```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <Version>0.1.0</Version>
    <Authors>viewport-cap</Authors>
    <Company>viewport-cap</Company>
    <Product>viewport-cap</Product>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Write `.gitignore`**

Create:
```gitignore
bin/
obj/
.vs/
.idea/
*.user
*.suo
TestResults/
artifacts/
dist/
tools/ffmpeg/
*.zip
*.log
```

- [ ] **Step 5: Write `.editorconfig`**

Create:
```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{xml,xaml,csproj,props,targets}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

- [ ] **Step 6: Replace the app project file**

Modify `src/ViewportCap.App/ViewportCap.App.csproj` to:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <AssemblyName>viewport-cap</AssemblyName>
    <RootNamespace>ViewportCap.App</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <ApplicationIcon>Assets\app.ico</ApplicationIcon>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ViewportCap.Core\ViewportCap.Core.csproj" />
    <ProjectReference Include="..\ViewportCap.Encoding\ViewportCap.Encoding.csproj" />
    <ProjectReference Include="..\ViewportCap.Capture\ViewportCap.Capture.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Resource Include="Assets\app.ico" />
  </ItemGroup>
</Project>
```

- [ ] **Step 7: Replace the capture project file**

Modify `src/ViewportCap.Capture/ViewportCap.Capture.csproj` to:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <RootNamespace>ViewportCap.Capture</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ViewportCap.Core\ViewportCap.Core.csproj" />
    <PackageReference Include="ScreenRecorderLib" Version="7.0.1" />
  </ItemGroup>
</Project>
```

- [ ] **Step 8: Pin test packages**

Modify `tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj` to:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ViewportCap.Core\ViewportCap.Core.csproj" />
  </ItemGroup>
</Project>
```

Modify `tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj` to:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ViewportCap.Core\ViewportCap.Core.csproj" />
    <ProjectReference Include="..\..\src\ViewportCap.Encoding\ViewportCap.Encoding.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 9: Create the app manifest and icon**

Create `src/ViewportCap.App/app.manifest`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="ViewportCap.App" />
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
      <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
    </windowsSettings>
  </application>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
</assembly>
```

Create `tools/generate-icon.ps1`:
```powershell
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\src\ViewportCap.App\Assets\app.ico'
New-Item -ItemType Directory -Path (Split-Path $out) -Force | Out-Null

$bitmap = New-Object System.Drawing.Bitmap 32, 32
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$background = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 17, 24, 39))
$border = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 96, 165, 250)), 2

$graphics.FillRectangle($background, 0, 0, 32, 32)
$graphics.DrawRectangle($border, 7, 8, 18, 16)
$graphics.FillEllipse([System.Drawing.Brushes]::White, 14, 14, 4, 4)

$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = [System.IO.File]::Create($out)
$icon.Save($stream)
$stream.Close()

$graphics.Dispose()
$bitmap.Dispose()
$icon.Dispose()
```

Run:
```powershell
powershell -ExecutionPolicy Bypass -File tools/generate-icon.ps1
```

Expected: `src/ViewportCap.App/Assets/app.ico` exists.

- [ ] **Step 10: Write the CI workflow**

Create `.github/workflows/ci.yml`:
```yaml
name: ci

on:
  push:
    branches: [main, feat/v1]
  pull_request:

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
      - run: dotnet restore ViewportCap.sln
      - run: dotnet build ViewportCap.sln -c Release --no-restore
      - run: dotnet test ViewportCap.sln -c Release --no-build --filter "Category!=Integration"
```

- [ ] **Step 11: Build**

Run:
```powershell
dotnet build ViewportCap.sln -c Release
```

Expected: `Build succeeded` with `0 Error(s)`.

- [ ] **Step 12: Commit**

Run:
```powershell
git add .
git commit -m "chore: scaffold viewport-cap solution"
```

Expected: commit created.

---

### Task 2: Core models, hotkey, settings

**Files:**
- Create: `src/ViewportCap.Core/AudioMode.cs`
- Create: `src/ViewportCap.Core/CaptureRegion.cs`
- Create: `src/ViewportCap.Core/OutputFormat.cs`
- Create: `src/ViewportCap.Core/QualityPreset.cs`
- Create: `src/ViewportCap.Core/ScalePreset.cs`
- Create: `src/ViewportCap.Core/HotkeyGesture.cs`
- Create: `src/ViewportCap.Core/AppSettings.cs`
- Create: `src/ViewportCap.Core/ISettingsStore.cs`
- Create: `src/ViewportCap.Core/JsonSettingsStore.cs`
- Test: `tests/ViewportCap.Core.Tests/HotkeyGestureTests.cs`
- Test: `tests/ViewportCap.Core.Tests/AppSettingsTests.cs`
- Test: `tests/ViewportCap.Core.Tests/JsonSettingsStoreTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ViewportCap.Core.Tests/HotkeyGestureTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class HotkeyGestureTests
{
    [Fact]
    public void Default_RoundTrips()
    {
        var text = HotkeyGesture.Default.ToString();
        Assert.Equal("Ctrl+Alt+Shift+R", text);
        Assert.True(HotkeyGesture.TryParse(text, out var parsed));
        Assert.Equal(HotkeyGesture.Default, parsed);
    }

    [Theory]
    [InlineData("ctrl+alt+shift+r", "Ctrl+Alt+Shift+R")]
    [InlineData("Shift+F9", "Shift+F9")]
    [InlineData("Win+PrintScreen", "Win+PrintScreen")]
    [InlineData("Alt+Space", "Alt+Space")]
    public void TryParse_AcceptsSupportedCombinations(string input, string expected)
    {
        Assert.True(HotkeyGesture.TryParse(input, out var gesture));
        Assert.Equal(expected, gesture.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("R")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+Alt+R+E")]
    [InlineData("Ctrl+Alt+NotAKey")]
    public void TryParse_RejectsInvalidInput(string input)
    {
        Assert.False(HotkeyGesture.TryParse(input, out _));
    }
}
```

Create `tests/ViewportCap.Core.Tests/AppSettingsTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void CreateDefault_UsesExpectedValues()
    {
        var settings = AppSettings.CreateDefault();
        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey);
        Assert.Equal(3, settings.CountdownSeconds);
        Assert.Equal(OutputFormat.Mp4, settings.DefaultFormat);
        Assert.Equal(AudioMode.Off, settings.AudioMode);
        Assert.False(settings.StartWithWindows);
        Assert.False(settings.HasShownFirstRunTip);
        Assert.EndsWith(Path.Combine("Videos", "ViewportCap"), settings.OutputFolder);
    }

    [Fact]
    public void Normalize_RepairsInvalidValues()
    {
        var settings = new AppSettings
        {
            Hotkey = "not a hotkey",
            CountdownSeconds = 99,
            DefaultFormat = (OutputFormat)99,
            AudioMode = (AudioMode)99,
            OutputFolder = " "
        };

        settings.Normalize();

        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey);
        Assert.Equal(10, settings.CountdownSeconds);
        Assert.Equal(OutputFormat.Mp4, settings.DefaultFormat);
        Assert.Equal(AudioMode.Off, settings.AudioMode);
        Assert.False(string.IsNullOrWhiteSpace(settings.OutputFolder));
    }
}
```

Create `tests/ViewportCap.Core.Tests/JsonSettingsStoreTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "viewport-cap-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var store = new JsonSettingsStore(Path.Combine(_dir, "settings.json"));
        var settings = store.Load();
        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var path = Path.Combine(_dir, "settings.json");
        var store = new JsonSettingsStore(path);
        var expected = AppSettings.CreateDefault();
        expected.Hotkey = "Ctrl+Shift+F8";
        expected.CountdownSeconds = 5;
        expected.DefaultFormat = OutputFormat.Gif;
        expected.AudioMode = AudioMode.Both;
        expected.HasShownFirstRunTip = true;

        store.Save(expected);
        var actual = store.Load();

        Assert.Equal(expected.Hotkey, actual.Hotkey);
        Assert.Equal(expected.CountdownSeconds, actual.CountdownSeconds);
        Assert.Equal(expected.DefaultFormat, actual.DefaultFormat);
        Assert.Equal(expected.AudioMode, actual.AudioMode);
        Assert.True(actual.HasShownFirstRunTip);
    }

    [Fact]
    public void Load_BacksUpCorruptFile_AndReturnsDefaults()
    {
        var path = Path.Combine(_dir, "settings.json");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(path, "{ broken json");

        var store = new JsonSettingsStore(path);
        var settings = store.Load();

        Assert.Equal("Ctrl+Alt+Shift+R", settings.Hotkey);
        Assert.Single(Directory.GetFiles(_dir, "settings.json.bad-*"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: build fails with errors such as `The type or namespace name 'HotkeyGesture' could not be found`.

- [ ] **Step 3: Add the simple enums**

Create `src/ViewportCap.Core/AudioMode.cs`:
```csharp
namespace ViewportCap.Core;

public enum AudioMode
{
    Off,
    Microphone,
    System,
    Both
}
```

Create `src/ViewportCap.Core/OutputFormat.cs`:
```csharp
namespace ViewportCap.Core;

public enum OutputFormat
{
    Mp4,
    WebM,
    Gif
}
```

Create `src/ViewportCap.Core/QualityPreset.cs`:
```csharp
namespace ViewportCap.Core;

public enum QualityPreset
{
    High,
    Balanced,
    Small
}
```

Create `src/ViewportCap.Core/ScalePreset.cs`:
```csharp
namespace ViewportCap.Core;

public enum ScalePreset
{
    Percent100,
    Percent75,
    Percent50,
    Fit960
}
```

Create `src/ViewportCap.Core/CaptureRegion.cs`:
```csharp
namespace ViewportCap.Core;

public readonly record struct CaptureRegion(
    string MonitorDeviceName,
    int MonitorLeft,
    int MonitorTop,
    int X,
    int Y,
    int Width,
    int Height,
    int Dpi)
{
    public const int MinimumSize = 32;

    public bool IsValid => Width >= MinimumSize && Height >= MinimumSize;

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public int SourceX => X - MonitorLeft;

    public int SourceY => Y - MonitorTop;

    public double Scale => Dpi / 96.0;
}
```

- [ ] **Step 4: Implement the hotkey parser**

Create `src/ViewportCap.Core/HotkeyGesture.cs`:
```csharp
namespace ViewportCap.Core;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public readonly record struct HotkeyGesture(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public static HotkeyGesture Default { get; } =
        new(HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x52);

    public static bool TryParse(string? text, out HotkeyGesture gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        var modifiers = HotkeyModifiers.None;
        uint key = 0;

        foreach (var raw in parts)
        {
            if (raw.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Control;
                continue;
            }

            if (raw.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Alt;
                continue;
            }

            if (raw.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Shift;
                continue;
            }

            if (raw.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= HotkeyModifiers.Win;
                continue;
            }

            if (key != 0) return false;
            if (!KeyNames.TryGetValue(raw, out key)) return false;
        }

        if (key == 0 || modifiers == HotkeyModifiers.None) return false;

        gesture = new HotkeyGesture(modifiers, key);
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyNames.FirstOrDefault(pair => pair.Value == VirtualKey).Key ?? $"0x{VirtualKey:X2}");
        return string.Join("+", parts);
    }

    private static readonly IReadOnlyDictionary<string, uint> KeyNames = CreateKeyNames();

    private static Dictionary<string, uint> CreateKeyNames()
    {
        var names = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        for (var c = 'A'; c <= 'Z'; c++) names[c.ToString()] = (uint)c;
        for (var c = '0'; c <= '9'; c++) names[c.ToString()] = (uint)c;
        for (var i = 1; i <= 12; i++) names[$"F{i}"] = (uint)(0x70 + i - 1);

        names["PrintScreen"] = 0x2C;
        names["Insert"] = 0x2D;
        names["Delete"] = 0x2E;
        names["Home"] = 0x24;
        names["End"] = 0x23;
        names["PageUp"] = 0x21;
        names["PageDown"] = 0x22;
        names["Space"] = 0x20;
        names["Tab"] = 0x09;
        names["Backspace"] = 0x08;
        names["Escape"] = 0x1B;

        return names;
    }
}
```

- [ ] **Step 5: Implement settings**

Create `src/ViewportCap.Core/AppSettings.cs`:
```csharp
namespace ViewportCap.Core;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string Hotkey { get; set; } = "Ctrl+Alt+Shift+R";
    public string OutputFolder { get; set; } = string.Empty;
    public int CountdownSeconds { get; set; } = 3;
    public OutputFormat DefaultFormat { get; set; } = OutputFormat.Mp4;
    public AudioMode AudioMode { get; set; } = AudioMode.Off;
    public string? MicrophoneDeviceName { get; set; }
    public string? SystemAudioDeviceName { get; set; }
    public bool StartWithWindows { get; set; }
    public bool HasShownFirstRunTip { get; set; }

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            OutputFolder = DefaultOutputFolder()
        };
    }

    public static string DefaultOutputFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "ViewportCap");
    }

    public void Normalize()
    {
        SchemaVersion = 1;

        if (string.IsNullOrWhiteSpace(OutputFolder))
            OutputFolder = DefaultOutputFolder();

        CountdownSeconds = Math.Clamp(CountdownSeconds, 0, 10);

        if (!HotkeyGesture.TryParse(Hotkey, out _))
            Hotkey = HotkeyGesture.Default.ToString();

        if (!Enum.IsDefined(DefaultFormat))
            DefaultFormat = OutputFormat.Mp4;

        if (!Enum.IsDefined(AudioMode))
            AudioMode = AudioMode.Off;
    }
}
```

Create `src/ViewportCap.Core/ISettingsStore.cs`:
```csharp
namespace ViewportCap.Core;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
```

Create `src/ViewportCap.Core/JsonSettingsStore.cs`:
```csharp
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViewportCap.Core;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsStore(string path)
    {
        _path = path;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
            return AppSettings.CreateDefault();

        try
        {
            var json = File.ReadAllText(_path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
                return AppSettings.CreateDefault();

            settings.Normalize();
            return settings;
        }
        catch (Exception)
        {
            TryBackupCorruptFile();
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();
        var directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException("Settings path has no directory.");

        Directory.CreateDirectory(directory);
        var temp = _path + ".tmp";
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(temp, json, new UTF8Encoding(false));
        File.Move(temp, _path, true);
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            var backup = $"{_path}.bad-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(_path, backup, true);
        }
        catch
        {
            // A failed backup must not prevent the app from starting.
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: `Passed!` with 15 passed tests.

- [ ] **Step 7: Commit**

Run:
```powershell
git add .
git commit -m "feat: add core settings and hotkey models"
```

---

### Task 3: Region math and recording state machine

**Files:**
- Create: `src/ViewportCap.Core/DragRect.cs`
- Create: `src/ViewportCap.Core/MonitorDescriptor.cs`
- Create: `src/ViewportCap.Core/RegionMath.cs`
- Create: `src/ViewportCap.Core/RecordingState.cs`
- Create: `src/ViewportCap.Core/RecordingStateMachine.cs`
- Test: `tests/ViewportCap.Core.Tests/RegionMathTests.cs`
- Test: `tests/ViewportCap.Core.Tests/RecordingStateMachineTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ViewportCap.Core.Tests/RegionMathTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class RegionMathTests
{
    [Fact]
    public void Normalize_HandlesReversedDrag()
    {
        var rect = RegionMath.Normalize(300, 200, 100, 50);
        Assert.Equal(100, rect.Left);
        Assert.Equal(50, rect.Top);
        Assert.Equal(200, rect.Width);
        Assert.Equal(150, rect.Height);
    }

    [Fact]
    public void ToPhysical_ScalesAt150Percent()
    {
        var monitor = new MonitorDescriptor(@"\\.\DISPLAY1", 0, 0, 1920, 1080, 144, true);
        var rect = RegionMath.ToPhysical(new DragRect(100, 50, 400, 200), monitor);

        Assert.Equal(150, rect.X);
        Assert.Equal(75, rect.Y);
        Assert.Equal(600, rect.Width);
        Assert.Equal(300, rect.Height);
        Assert.Equal(144, rect.Dpi);
        Assert.True(rect.IsValid);
    }

    [Fact]
    public void ToPhysical_ClampsToMonitor()
    {
        var monitor = new MonitorDescriptor(@"\\.\DISPLAY2", -1920, 0, 1920, 1080, 96, false);
        var rect = RegionMath.ToPhysical(new DragRect(-100, -50, 3000, 1500), monitor);

        Assert.Equal(-1920, rect.X);
        Assert.Equal(0, rect.Y);
        Assert.Equal(1920, rect.Width);
        Assert.Equal(1080, rect.Height);
    }

    [Fact]
    public void CaptureRegion_ComputesSourceCoordinates()
    {
        var region = new CaptureRegion(@"\\.\DISPLAY1", 1920, 0, 2000, 100, 400, 300, 120);
        Assert.Equal(80, region.SourceX);
        Assert.Equal(100, region.SourceY);
        Assert.Equal(2400, region.Right);
        Assert.Equal(400, region.Bottom);
        Assert.Equal(1.25, region.Scale);
    }
}
```

Create `tests/ViewportCap.Core.Tests/RecordingStateMachineTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class RecordingStateMachineTests
{
    [Theory]
    [InlineData(RecordingState.Idle, RecordingState.Selecting)]
    [InlineData(RecordingState.Selecting, RecordingState.Countdown)]
    [InlineData(RecordingState.Selecting, RecordingState.Idle)]
    [InlineData(RecordingState.Countdown, RecordingState.Recording)]
    [InlineData(RecordingState.Countdown, RecordingState.Idle)]
    [InlineData(RecordingState.Recording, RecordingState.Finalizing)]
    [InlineData(RecordingState.Recording, RecordingState.Idle)]
    [InlineData(RecordingState.Finalizing, RecordingState.Reviewing)]
    [InlineData(RecordingState.Finalizing, RecordingState.Idle)]
    [InlineData(RecordingState.Reviewing, RecordingState.Exporting)]
    [InlineData(RecordingState.Reviewing, RecordingState.Idle)]
    [InlineData(RecordingState.Exporting, RecordingState.Reviewing)]
    [InlineData(RecordingState.Exporting, RecordingState.Idle)]
    public void AllowsExpectedTransitions(RecordingState from, RecordingState to)
    {
        var machine = new RecordingStateMachine();
        MoveTo(machine, from);
        Assert.True(machine.CanTransitionTo(to));
        machine.TransitionTo(to);
        Assert.Equal(to, machine.State);
    }

    [Theory]
    [InlineData(RecordingState.Idle, RecordingState.Recording)]
    [InlineData(RecordingState.Idle, RecordingState.Idle)]
    [InlineData(RecordingState.Selecting, RecordingState.Reviewing)]
    [InlineData(RecordingState.Countdown, RecordingState.Finalizing)]
    [InlineData(RecordingState.Recording, RecordingState.Reviewing)]
    [InlineData(RecordingState.Reviewing, RecordingState.Recording)]
    public void RejectsInvalidTransitions(RecordingState from, RecordingState to)
    {
        var machine = new RecordingStateMachine();
        MoveTo(machine, from);
        Assert.False(machine.CanTransitionTo(to));
        Assert.Throws<InvalidOperationException>(() => machine.TransitionTo(to));
        Assert.Equal(from, machine.State);
    }

    private static void MoveTo(RecordingStateMachine machine, RecordingState target)
    {
        if (target == RecordingState.Idle) return;
        machine.TransitionTo(RecordingState.Selecting);
        if (target == RecordingState.Selecting) return;
        machine.TransitionTo(RecordingState.Countdown);
        if (target == RecordingState.Countdown) return;
        machine.TransitionTo(RecordingState.Recording);
        if (target == RecordingState.Recording) return;
        machine.TransitionTo(RecordingState.Finalizing);
        if (target == RecordingState.Finalizing) return;
        machine.TransitionTo(RecordingState.Reviewing);
        if (target == RecordingState.Reviewing) return;
        machine.TransitionTo(RecordingState.Exporting);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: build fails because `MonitorDescriptor`, `RegionMath`, `RecordingState`, and `RecordingStateMachine` do not exist.

- [ ] **Step 3: Implement region types**

Create `src/ViewportCap.Core/DragRect.cs`:
```csharp
namespace ViewportCap.Core;

public readonly record struct DragRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
}
```

Create `src/ViewportCap.Core/MonitorDescriptor.cs`:
```csharp
namespace ViewportCap.Core;

public readonly record struct MonitorDescriptor(
    string DeviceName,
    int Left,
    int Top,
    int Width,
    int Height,
    int Dpi,
    bool IsPrimary)
{
    public double Scale => Dpi / 96.0;
}
```

Create `src/ViewportCap.Core/RegionMath.cs`:
```csharp
namespace ViewportCap.Core;

public static class RegionMath
{
    public static DragRect Normalize(double x1, double y1, double x2, double y2)
    {
        return new DragRect(
            Math.Min(x1, x2),
            Math.Min(y1, y2),
            Math.Abs(x2 - x1),
            Math.Abs(y2 - y1));
    }

    public static CaptureRegion ToPhysical(DragRect local, MonitorDescriptor monitor)
    {
        var scale = monitor.Dpi / 96.0;
        var monitorRight = monitor.Left + monitor.Width;
        var monitorBottom = monitor.Top + monitor.Height;

        var left = monitor.Left + (int)Math.Round(local.Left * scale, MidpointRounding.AwayFromZero);
        var top = monitor.Top + (int)Math.Round(local.Top * scale, MidpointRounding.AwayFromZero);
        var right = monitor.Left + (int)Math.Round(local.Right * scale, MidpointRounding.AwayFromZero);
        var bottom = monitor.Top + (int)Math.Round(local.Bottom * scale, MidpointRounding.AwayFromZero);

        left = Math.Clamp(left, monitor.Left, monitorRight);
        top = Math.Clamp(top, monitor.Top, monitorBottom);
        right = Math.Clamp(right, monitor.Left, monitorRight);
        bottom = Math.Clamp(bottom, monitor.Top, monitorBottom);

        return new CaptureRegion(
            monitor.DeviceName,
            monitor.Left,
            monitor.Top,
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top),
            monitor.Dpi);
    }
}
```

- [ ] **Step 4: Implement the state machine**

Create `src/ViewportCap.Core/RecordingState.cs`:
```csharp
namespace ViewportCap.Core;

public enum RecordingState
{
    Idle,
    Selecting,
    Countdown,
    Recording,
    Finalizing,
    Reviewing,
    Exporting
}
```

Create `src/ViewportCap.Core/RecordingStateMachine.cs`:
```csharp
namespace ViewportCap.Core;

public sealed class RecordingStateMachine
{
    private static readonly IReadOnlyDictionary<RecordingState, RecordingState[]> Allowed =
        new Dictionary<RecordingState, RecordingState[]>
        {
            [RecordingState.Idle] = new[] { RecordingState.Selecting },
            [RecordingState.Selecting] = new[] { RecordingState.Countdown, RecordingState.Idle },
            [RecordingState.Countdown] = new[] { RecordingState.Recording, RecordingState.Idle },
            [RecordingState.Recording] = new[] { RecordingState.Finalizing, RecordingState.Idle },
            [RecordingState.Finalizing] = new[] { RecordingState.Reviewing, RecordingState.Idle },
            [RecordingState.Reviewing] = new[] { RecordingState.Exporting, RecordingState.Idle },
            [RecordingState.Exporting] = new[] { RecordingState.Reviewing, RecordingState.Idle }
        };

    public RecordingState State { get; private set; } = RecordingState.Idle;

    public bool CanTransitionTo(RecordingState next)
    {
        return Allowed.TryGetValue(State, out var targets) && targets.Contains(next);
    }

    public void TransitionTo(RecordingState next)
    {
        if (!CanTransitionTo(next))
            throw new InvalidOperationException($"Cannot transition from {State} to {next}.");

        State = next;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: `Passed!` with 38 passed tests.

- [ ] **Step 6: Commit**

Run:
```powershell
git add .
git commit -m "feat: add region math and recording state machine"
```

---

### Task 4: Export settings, naming, and planning

**Files:**
- Create: `src/ViewportCap.Core/ReviewSettings.cs`
- Create: `src/ViewportCap.Core/ExportPlan.cs`
- Create: `src/ViewportCap.Core/IExporter.cs`
- Create: `src/ViewportCap.Core/ExportNaming.cs`
- Create: `src/ViewportCap.Core/ExportPlanner.cs`
- Test: `tests/ViewportCap.Core.Tests/ExportNamingTests.cs`
- Test: `tests/ViewportCap.Core.Tests/ExportPlannerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ViewportCap.Core.Tests/ExportNamingTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class ExportNamingTests
{
    [Theory]
    [InlineData(OutputFormat.Mp4, ".mp4")]
    [InlineData(OutputFormat.WebM, ".webm")]
    [InlineData(OutputFormat.Gif, ".gif")]
    public void Extension_ReturnsExpectedValue(OutputFormat format, string expected)
    {
        Assert.Equal(expected, ExportNaming.Extension(format));
    }

    [Fact]
    public void BuildBaseName_UsesSortableTimestamp()
    {
        var value = new DateTimeOffset(2026, 10, 26, 14, 32, 5, TimeSpan.Zero);
        Assert.Equal("viewport-cap_2026-10-26_14-32-05", ExportNaming.BuildBaseName(value));
    }

    [Fact]
    public void EnsureUnique_AppendsCounter()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\out\viewport-cap_2026-10-26_14-32-05.mp4",
            @"C:\out\viewport-cap_2026-10-26_14-32-05-2.mp4"
        };

        var path = ExportNaming.EnsureUnique(
            @"C:\out",
            "viewport-cap_2026-10-26_14-32-05",
            ".mp4",
            existing.Contains);

        Assert.Equal(@"C:\out\viewport-cap_2026-10-26_14-32-05-3.mp4", path);
    }
}
```

Create `tests/ViewportCap.Core.Tests/ExportPlannerTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class ExportPlannerTests
{
    [Fact]
    public void Create_UsesTimestampedNameAndCopyFastPath()
    {
        var settings = ValidSettings();
        var plan = ExportPlanner.Create(
            settings,
            new DateTimeOffset(2026, 10, 26, 14, 32, 5, TimeSpan.Zero),
            _ => false);

        Assert.Equal(@"C:\out\viewport-cap_2026-10-26_14-32-05.mp4", plan.OutputPath);
        Assert.True(plan.CopySource);
    }

    [Fact]
    public void Create_ReencodesWhenTrimmed()
    {
        var settings = ValidSettings();
        settings = Copy(settings, trimStart: TimeSpan.FromSeconds(1), trimEnd: TimeSpan.FromSeconds(4));
        var plan = ExportPlanner.Create(settings, DateTimeOffset.UtcNow, _ => false);
        Assert.False(plan.CopySource);
    }

    [Fact]
    public void Create_ReencodesForGif()
    {
        var settings = ValidSettings();
        settings = Copy(settings, format: OutputFormat.Gif, frameRate: 15, scale: ScalePreset.Fit960);
        var plan = ExportPlanner.Create(settings, DateTimeOffset.UtcNow, _ => false);
        Assert.False(plan.CopySource);
    }

    [Fact]
    public void Create_ThrowsForInvalidSettings()
    {
        var settings = ValidSettings();
        settings = Copy(settings, trimEnd: TimeSpan.FromMilliseconds(100));
        Assert.Throws<ArgumentException>(() => ExportPlanner.Create(settings, DateTimeOffset.UtcNow, _ => false));
    }

    [Fact]
    public void Validate_RejectsThirtyFpsGif()
    {
        var settings = ValidSettings();
        settings = Copy(settings, format: OutputFormat.Gif, frameRate: 30, scale: ScalePreset.Fit960);
        Assert.Contains(settings.Validate(), error => error.Contains("GIF", StringComparison.OrdinalIgnoreCase));
    }

    private static ReviewSettings ValidSettings()
    {
        return new ReviewSettings
        {
            SourcePath = @"C:\temp\source.mp4",
            OutputDirectory = @"C:\out",
            Format = OutputFormat.Mp4,
            Quality = QualityPreset.Balanced,
            FrameRate = 30,
            Scale = ScalePreset.Percent100,
            TrimStart = TimeSpan.Zero,
            TrimEnd = TimeSpan.FromSeconds(5),
            HasAudio = false,
            MuteAudio = false,
            SourceWidth = 1280,
            SourceHeight = 720,
            SourceDuration = TimeSpan.FromSeconds(5)
        };
    }

    private static ReviewSettings Copy(
        ReviewSettings source,
        OutputFormat? format = null,
        QualityPreset? quality = null,
        int? frameRate = null,
        ScalePreset? scale = null,
        TimeSpan? trimStart = null,
        TimeSpan? trimEnd = null)
    {
        return new ReviewSettings
        {
            SourcePath = source.SourcePath,
            OutputDirectory = source.OutputDirectory,
            Format = format ?? source.Format,
            Quality = quality ?? source.Quality,
            FrameRate = frameRate ?? source.FrameRate,
            Scale = scale ?? source.Scale,
            TrimStart = trimStart ?? source.TrimStart,
            TrimEnd = trimEnd ?? source.TrimEnd,
            HasAudio = source.HasAudio,
            MuteAudio = source.MuteAudio,
            SourceWidth = source.SourceWidth,
            SourceHeight = source.SourceHeight,
            SourceDuration = source.SourceDuration
        };
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: build fails because `ReviewSettings`, `ExportPlan`, `ExportNaming`, and `ExportPlanner` do not exist.

- [ ] **Step 3: Implement review settings**

Create `src/ViewportCap.Core/ReviewSettings.cs`:
```csharp
namespace ViewportCap.Core;

public sealed class ReviewSettings
{
    public string SourcePath { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public OutputFormat Format { get; init; } = OutputFormat.Mp4;
    public QualityPreset Quality { get; init; } = QualityPreset.Balanced;
    public int FrameRate { get; init; } = 30;
    public ScalePreset Scale { get; init; } = ScalePreset.Percent100;
    public TimeSpan TrimStart { get; init; }
    public TimeSpan TrimEnd { get; init; }
    public bool HasAudio { get; init; }
    public bool MuteAudio { get; init; }
    public int SourceWidth { get; init; }
    public int SourceHeight { get; init; }
    public TimeSpan SourceDuration { get; init; }

    public TimeSpan TrimDuration => TrimEnd - TrimStart;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SourcePath))
            errors.Add("Source path is required.");
        if (string.IsNullOrWhiteSpace(OutputDirectory))
            errors.Add("Output directory is required.");
        if (SourceWidth <= 0 || SourceHeight <= 0)
            errors.Add("Source dimensions must be positive.");
        if (SourceDuration <= TimeSpan.Zero)
            errors.Add("Source duration must be positive.");
        if (TrimStart < TimeSpan.Zero)
            errors.Add("Trim start cannot be negative.");
        if (TrimEnd <= TrimStart)
            errors.Add("Trim end must be after trim start.");
        if (TrimDuration < TimeSpan.FromMilliseconds(250))
            errors.Add("The trimmed clip must be at least 0.25 seconds.");
        if (TrimEnd > SourceDuration + TimeSpan.FromMilliseconds(50))
            errors.Add("Trim end cannot be after the source duration.");
        if (!Enum.IsDefined(Format))
            errors.Add("Output format is invalid.");
        if (!Enum.IsDefined(Quality))
            errors.Add("Quality preset is invalid.");
        if (!Enum.IsDefined(Scale))
            errors.Add("Scale preset is invalid.");

        if (Format == OutputFormat.Gif)
        {
            if (FrameRate is not (10 or 15 or 20))
                errors.Add("GIF frame rate must be 10, 15, or 20.");
        }
        else if (FrameRate is not (15 or 30))
        {
            errors.Add("Video frame rate must be 15 or 30.");
        }

        return errors;
    }
}
```

- [ ] **Step 4: Implement naming and planning**

Create `src/ViewportCap.Core/ExportPlan.cs`:
```csharp
namespace ViewportCap.Core;

public sealed record ExportPlan(ReviewSettings Settings, string OutputPath, bool CopySource);
```

Create `src/ViewportCap.Core/IExporter.cs`:
```csharp
namespace ViewportCap.Core;

public sealed record ExportResult(string OutputPath);

public interface IExporter
{
    Task<ExportResult> ExportAsync(
        ExportPlan plan,
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}
```

Create `src/ViewportCap.Core/ExportNaming.cs`:
```csharp
namespace ViewportCap.Core;

public static class ExportNaming
{
    public static string Extension(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Mp4 => ".mp4",
            OutputFormat.WebM => ".webm",
            OutputFormat.Gif => ".gif",
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    public static string BuildBaseName(DateTimeOffset createdAt)
    {
        return $"viewport-cap_{createdAt:yyyy-MM-dd_HH-mm-ss}";
    }

    public static string EnsureUnique(
        string directory,
        string baseName,
        string extension,
        Func<string, bool> fileExists)
    {
        var candidate = Path.Combine(directory, baseName + extension);
        var counter = 2;

        while (fileExists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName}-{counter}{extension}");
            counter++;
        }

        return candidate;
    }
}
```

Create `src/ViewportCap.Core/ExportPlanner.cs`:
```csharp
namespace ViewportCap.Core;

public static class ExportPlanner
{
    public static ExportPlan Create(
        ReviewSettings settings,
        DateTimeOffset createdAt,
        Func<string, bool> fileExists)
    {
        ValidateOrThrow(settings);
        var extension = ExportNaming.Extension(settings.Format);
        var baseName = ExportNaming.BuildBaseName(createdAt);
        var outputPath = ExportNaming.EnsureUnique(
            settings.OutputDirectory,
            baseName,
            extension,
            fileExists);

        return new ExportPlan(settings, outputPath, CanCopySource(settings));
    }

    public static ExportPlan CreateForPath(ReviewSettings settings, string outputPath)
    {
        ValidateOrThrow(settings);
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path is required.", nameof(outputPath));

        return new ExportPlan(settings, outputPath, CanCopySource(settings));
    }

    public static bool CanCopySource(ReviewSettings settings)
    {
        return settings.Format == OutputFormat.Mp4
            && settings.Quality == QualityPreset.Balanced
            && settings.FrameRate == 30
            && settings.Scale == ScalePreset.Percent100
            && settings.TrimStart <= TimeSpan.FromMilliseconds(50)
            && settings.TrimEnd >= settings.SourceDuration - TimeSpan.FromMilliseconds(50)
            && (!settings.HasAudio || !settings.MuteAudio);
    }

    private static void ValidateOrThrow(ReviewSettings settings)
    {
        var errors = settings.Validate();
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors), nameof(settings));
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: `Passed!`.

- [ ] **Step 6: Commit**

Run:
```powershell
git add .
git commit -m "feat: add export planning and naming"
```

---

### Task 5: FFmpeg capabilities, arguments, and exporter

**Files:**
- Create: `src/ViewportCap.Encoding/ProcessResult.cs`
- Create: `src/ViewportCap.Encoding/IProcessRunner.cs`
- Create: `src/ViewportCap.Encoding/SystemProcessRunner.cs`
- Create: `src/ViewportCap.Encoding/ExportException.cs`
- Create: `src/ViewportCap.Encoding/FfmpegCapabilities.cs`
- Create: `src/ViewportCap.Encoding/FfmpegLocator.cs`
- Create: `src/ViewportCap.Encoding/FfmpegProgressParser.cs`
- Create: `src/ViewportCap.Encoding/FfmpegProbe.cs`
- Create: `src/ViewportCap.Encoding/FfmpegArgumentBuilder.cs`
- Create: `src/ViewportCap.Encoding/FfmpegExporter.cs`
- Test: `tests/ViewportCap.Encoding.Tests/FfmpegCapabilitiesTests.cs`
- Test: `tests/ViewportCap.Encoding.Tests/FfmpegLocatorTests.cs`
- Test: `tests/ViewportCap.Encoding.Tests/FfmpegProgressParserTests.cs`
- Test: `tests/ViewportCap.Encoding.Tests/FfmpegArgumentBuilderTests.cs`
- Test: `tests/ViewportCap.Encoding.Tests/FfmpegExporterTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/ViewportCap.Encoding.Tests/FfmpegCapabilitiesTests.cs`:
```csharp
using ViewportCap.Encoding;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class FfmpegCapabilitiesTests
{
    [Fact]
    public void Parse_FindsSupportedEncoders()
    {
        const string output = """
             V..... h264_mf              Media Foundation H.264 encoder
             V..... libopenh264          OpenH264
             V..... libvpx-vp9           libvpx VP9
             A..... libopus              libopus
            """;

        var caps = FfmpegCapabilities.Parse(output);

        Assert.True(caps.HasH264Mf);
        Assert.True(caps.HasOpenH264);
        Assert.True(caps.HasVp9);
        Assert.True(caps.HasOpus);
        Assert.True(caps.CanEncodeH264);
    }

    [Fact]
    public void Parse_ReturnsFalseWhenEncodersMissing()
    {
        var caps = FfmpegCapabilities.Parse(" V..... h264_mf\n");
        Assert.True(caps.HasH264Mf);
        Assert.False(caps.HasOpenH264);
        Assert.False(caps.HasVp9);
        Assert.False(caps.HasOpus);
    }
}
```

Create `tests/ViewportCap.Encoding.Tests/FfmpegLocatorTests.cs`:
```csharp
using ViewportCap.Encoding;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class FfmpegLocatorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "viewport-cap-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void Find_ReturnsFfmpegFromExtraDirectory()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "ffmpeg.exe");
        File.WriteAllText(path, "fake");

        Assert.Equal(path, FfmpegLocator.Find(_dir));
    }

    [Fact]
    public void Find_ReturnsNullWhenMissing()
    {
        Assert.Null(FfmpegLocator.Find(_dir));
    }
}
```

Create `tests/ViewportCap.Encoding.Tests/FfmpegProgressParserTests.cs`:
```csharp
using ViewportCap.Encoding;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class FfmpegProgressParserTests
{
    [Fact]
    public void Feed_ParsesOutTimeMicroseconds()
    {
        var parser = new FfmpegProgressParser(10);
        parser.Feed("out_time_us=5000000");
        Assert.Equal(50, parser.Percent);
    }

    [Fact]
    public void Feed_ParsesEndMarker()
    {
        var parser = new FfmpegProgressParser(10);
        parser.Feed("progress=end");
        Assert.Equal(100, parser.Percent);
    }

    [Fact]
    public void Feed_IgnoresInvalidLines()
    {
        var parser = new FfmpegProgressParser(10);
        parser.Feed("out_time_us=not-a-number");
        Assert.Null(parser.Percent);
    }
}
```

Create `tests/ViewportCap.Encoding.Tests/FfmpegArgumentBuilderTests.cs`:
```csharp
using ViewportCap.Core;
using ViewportCap.Encoding;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class FfmpegArgumentBuilderTests
{
    private static readonly FfmpegCapabilities AllCapabilities = new(true, true, true, true);

    [Fact]
    public void Mp4_ContainsH264AndFastStart()
    {
        var args = FfmpegArgumentBuilder.Build(Settings(), @"C:\out\clip.mp4", AllCapabilities);

        Assert.Contains("h264_mf", args);
        Assert.Contains("-vf", args);
        Assert.Contains("+faststart", args);
        Assert.Contains("-an", args);
        Assert.Equal(@"C:\out\clip.mp4", args[^1]);
    }

    [Fact]
    public void WebM_ContainsVp9AndOpusWhenAudioKept()
    {
        var settings = Settings(format: OutputFormat.WebM, frameRate: 30, hasAudio: true, mute: false);
        var args = FfmpegArgumentBuilder.Build(settings, @"C:\out\clip.webm", AllCapabilities);

        Assert.Contains("libvpx-vp9", args);
        Assert.Contains("libopus", args);
        Assert.Contains("0:a:0?", args);
        Assert.DoesNotContain("-an", args);
    }

    [Fact]
    public void Gif_ContainsPaletteFilterAndNoAudio()
    {
        var settings = Settings(format: OutputFormat.Gif, frameRate: 15, scale: ScalePreset.Fit960);
        var args = FfmpegArgumentBuilder.Build(settings, @"C:\out\clip.gif", AllCapabilities);

        Assert.Contains("-filter_complex", args);
        Assert.Contains(args, value => value.Contains("palettegen=max_colors=128"));
        Assert.Contains("-loop", args);
        Assert.Contains("-an", args);
    }

    [Fact]
    public void Trim_UsesAccurateTimecodes()
    {
        var settings = Settings(trimStart: TimeSpan.FromSeconds(1.5), trimEnd: TimeSpan.FromSeconds(3.75));
        var args = FfmpegArgumentBuilder.Build(settings, @"C:\out\clip.mp4", AllCapabilities);

        Assert.Contains("00:00:01.500", args);
        Assert.Contains("00:00:02.250", args);
    }

    [Fact]
    public void Mp4_ThrowsWhenNoH264Encoder()
    {
        var caps = new FfmpegCapabilities(false, false, true, true);
        Assert.Throws<InvalidOperationException>(() =>
            FfmpegArgumentBuilder.Build(Settings(), @"C:\out\clip.mp4", caps));
    }

    private static ReviewSettings Settings(
        OutputFormat format = OutputFormat.Mp4,
        int frameRate = 30,
        ScalePreset scale = ScalePreset.Percent100,
        bool hasAudio = false,
        bool mute = false,
        TimeSpan? trimStart = null,
        TimeSpan? trimEnd = null)
    {
        return new ReviewSettings
        {
            SourcePath = @"C:\temp\source.mp4",
            OutputDirectory = @"C:\out",
            Format = format,
            Quality = QualityPreset.Balanced,
            FrameRate = frameRate,
            Scale = scale,
            TrimStart = trimStart ?? TimeSpan.Zero,
            TrimEnd = trimEnd ?? TimeSpan.FromSeconds(5),
            HasAudio = hasAudio,
            MuteAudio = mute,
            SourceWidth = 1280,
            SourceHeight = 720,
            SourceDuration = TimeSpan.FromSeconds(5)
        };
    }
}
```

Create `tests/ViewportCap.Encoding.Tests/FfmpegExporterTests.cs`:
```csharp
using ViewportCap.Core;
using ViewportCap.Encoding;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class FfmpegExporterTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "viewport-cap-tests", Guid.NewGuid().ToString("N"));

    public FfmpegExporterTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public async Task CopySource_CopiesFileWithoutRunningFfmpeg()
    {
        var source = Path.Combine(_dir, "source.mp4");
        var output = Path.Combine(_dir, "out.mp4");
        await File.WriteAllTextAsync(source, "video");
        var runner = new FakeRunner();
        var exporter = new FfmpegExporter("ffmpeg.exe", new FfmpegCapabilities(true, true, true, true), runner);
        var plan = new ExportPlan(Settings(source), output, true);

        var result = await exporter.ExportAsync(plan, null, CancellationToken.None);

        Assert.Equal(output, result.OutputPath);
        Assert.Equal("video", await File.ReadAllTextAsync(output));
        Assert.Empty(runner.Calls);
    }

    [Fact]
    public async Task Transcode_MovesPartialFileIntoPlace()
    {
        var source = Path.Combine(_dir, "source.mp4");
        var output = Path.Combine(_dir, "out.webm");
        await File.WriteAllTextAsync(source, "video");
        var runner = new FakeRunner
        {
            OnRun = args => File.WriteAllText(args[^1], "encoded")
        };
        var exporter = new FfmpegExporter("ffmpeg.exe", new FfmpegCapabilities(true, true, true, true), runner);
        var plan = new ExportPlan(Settings(source, OutputFormat.WebM, 30), output, false);

        var result = await exporter.ExportAsync(plan, null, CancellationToken.None);

        Assert.Equal(output, result.OutputPath);
        Assert.Equal("encoded", await File.ReadAllTextAsync(output));
        Assert.False(File.Exists(Path.Combine(_dir, "out.partial.webm")));
    }

    [Fact]
    public async Task Failure_DeletesPartialFileAndThrows()
    {
        var source = Path.Combine(_dir, "source.mp4");
        var output = Path.Combine(_dir, "out.webm");
        await File.WriteAllTextAsync(source, "video");
        var runner = new FakeRunner
        {
            Result = new ProcessResult(1, string.Empty, "encoder failed"),
            OnRun = args => File.WriteAllText(args[^1], "partial")
        };
        var exporter = new FfmpegExporter("ffmpeg.exe", new FfmpegCapabilities(true, true, true, true), runner);
        var plan = new ExportPlan(Settings(source, OutputFormat.WebM, 30), output, false);

        await Assert.ThrowsAsync<ExportException>(() =>
            exporter.ExportAsync(plan, null, CancellationToken.None));

        Assert.False(File.Exists(Path.Combine(_dir, "out.partial.webm")));
        Assert.False(File.Exists(output));
    }

    private static ReviewSettings Settings(
        string source,
        OutputFormat format = OutputFormat.Mp4,
        int frameRate = 30)
    {
        return new ReviewSettings
        {
            SourcePath = source,
            OutputDirectory = Path.GetDirectoryName(source)!,
            Format = format,
            Quality = QualityPreset.Balanced,
            FrameRate = frameRate,
            Scale = ScalePreset.Percent100,
            TrimStart = TimeSpan.Zero,
            TrimEnd = TimeSpan.FromSeconds(5),
            HasAudio = false,
            MuteAudio = false,
            SourceWidth = 1280,
            SourceHeight = 720,
            SourceDuration = TimeSpan.FromSeconds(5)
        };
    }

    private sealed class FakeRunner : IProcessRunner
    {
        public List<IReadOnlyList<string>> Calls { get; } = new();
        public ProcessResult Result { get; init; } = new(0, string.Empty, string.Empty);
        public Action<IReadOnlyList<string>>? OnRun { get; init; }

        public Task<ProcessResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            Action<string>? onStandardOutput,
            Action<string>? onStandardError,
            CancellationToken cancellationToken)
        {
            Calls.Add(arguments);
            OnRun?.Invoke(arguments);
            return Task.FromResult(Result);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```powershell
dotnet test tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj -c Debug
```

Expected: build fails because the FFmpeg types do not exist.

- [ ] **Step 3: Implement process primitives**

Create `src/ViewportCap.Encoding/ProcessResult.cs`:
```csharp
namespace ViewportCap.Encoding;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
```

Create `src/ViewportCap.Encoding/IProcessRunner.cs`:
```csharp
namespace ViewportCap.Encoding;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        Action<string>? onStandardOutput,
        Action<string>? onStandardError,
        CancellationToken cancellationToken);
}
```

Create `src/ViewportCap.Encoding/SystemProcessRunner.cs`:
```csharp
using System.Diagnostics;
using System.Text;

namespace ViewportCap.Encoding;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        Action<string>? onStandardOutput,
        Action<string>? onStandardError,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Could not start {fileName}.");

        var stdoutTask = ReadAllAsync(process.StandardOutput, onStandardOutput);
        var stderrTask = ReadAllAsync(process.StandardError, onStandardError);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private static async Task<string> ReadAllAsync(StreamReader reader, Action<string>? callback)
    {
        var builder = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            callback?.Invoke(line);
            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may have already exited.
        }
    }
}
```

Create `src/ViewportCap.Encoding/ExportException.cs`:
```csharp
namespace ViewportCap.Encoding;

public sealed class ExportException : Exception
{
    public ExportException(string message) : base(message)
    {
    }

    public ExportException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
```

- [ ] **Step 4: Implement capabilities, locator, and progress**

Create `src/ViewportCap.Encoding/FfmpegCapabilities.cs`:
```csharp
namespace ViewportCap.Encoding;

public sealed record FfmpegCapabilities(
    bool HasH264Mf,
    bool HasOpenH264,
    bool HasVp9,
    bool HasOpus)
{
    public bool CanEncodeH264 => HasH264Mf || HasOpenH264;

    public static FfmpegCapabilities Parse(string encoderOutput)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(encoderOutput);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || (trimmed[0] != 'V' && trimmed[0] != 'A'))
                continue;

            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                names.Add(parts[1]);
        }

        return new FfmpegCapabilities(
            names.Contains("h264_mf"),
            names.Contains("libopenh264"),
            names.Contains("libvpx-vp9"),
            names.Contains("libopus"));
    }
}
```

Create `src/ViewportCap.Encoding/FfmpegLocator.cs`:
```csharp
namespace ViewportCap.Encoding;

public static class FfmpegLocator
{
    public static string? Find(string? extraDirectory = null)
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe"),
            Path.Combine(Environment.CurrentDirectory, "tools", "ffmpeg", "ffmpeg.exe")
        };

        if (!string.IsNullOrWhiteSpace(extraDirectory))
            candidates.Add(Path.Combine(extraDirectory, "ffmpeg.exe"));

        return candidates.FirstOrDefault(File.Exists);
    }
}
```

Create `src/ViewportCap.Encoding/FfmpegProgressParser.cs`:
```csharp
using System.Globalization;

namespace ViewportCap.Encoding;

public sealed class FfmpegProgressParser
{
    private readonly double _durationSeconds;

    public FfmpegProgressParser(double durationSeconds)
    {
        _durationSeconds = durationSeconds;
    }

    public double? Percent { get; private set; }

    public void Feed(string line)
    {
        if (line.StartsWith("progress=end", StringComparison.Ordinal))
        {
            Percent = 100;
            return;
        }

        var prefix = line.StartsWith("out_time_us=", StringComparison.Ordinal)
            ? "out_time_us="
            : line.StartsWith("out_time_ms=", StringComparison.Ordinal)
                ? "out_time_ms="
                : null;

        if (prefix is null || _durationSeconds <= 0)
            return;

        if (!long.TryParse(line[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var microseconds))
            return;

        Percent = Math.Clamp(microseconds / 1_000_000.0 / _durationSeconds * 100.0, 0, 100);
    }
}
```

Create `src/ViewportCap.Encoding/FfmpegProbe.cs`:
```csharp
namespace ViewportCap.Encoding;

public static class FfmpegProbe
{
    public static async Task<FfmpegCapabilities> ProbeAsync(
        string ffmpegPath,
        IProcessRunner runner,
        CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(
            ffmpegPath,
            new[] { "-hide_banner", "-encoders" },
            null,
            null,
            cancellationToken);

        return result.ExitCode == 0
            ? FfmpegCapabilities.Parse(result.StandardOutput)
            : new FfmpegCapabilities(false, false, false, false);
    }
}
```

- [ ] **Step 5: Implement argument building**

Create `src/ViewportCap.Encoding/FfmpegArgumentBuilder.cs`:
```csharp
using System.Globalization;
using ViewportCap.Core;

namespace ViewportCap.Encoding;

public static class FfmpegArgumentBuilder
{
    public static IReadOnlyList<string> Build(
        ReviewSettings settings,
        string outputPath,
        FfmpegCapabilities capabilities)
    {
        if (settings.Format == OutputFormat.Mp4 && !capabilities.CanEncodeH264)
            throw new InvalidOperationException("No H.264 encoder is available in FFmpeg.");
        if (settings.Format == OutputFormat.WebM && !capabilities.HasVp9)
            throw new InvalidOperationException("libvpx-vp9 is not available in FFmpeg.");

        var args = new List<string>
        {
            "-y", "-hide_banner", "-nostdin",
            "-i", settings.SourcePath,
            "-ss", FormatTime(settings.TrimStart),
            "-t", FormatTime(settings.TrimDuration)
        };

        var keepAudio = settings.Format != OutputFormat.Gif && settings.HasAudio && !settings.MuteAudio;
        if (keepAudio)
        {
            args.Add("-map");
            args.Add("0:a:0?");
        }
        else
        {
            args.Add("-an");
        }

        var (width, height) = CalculateOutputSize(settings.SourceWidth, settings.SourceHeight, settings.Scale);

        switch (settings.Format)
        {
            case OutputFormat.Mp4:
                AddMp4(args, settings, capabilities, width, height, keepAudio);
                break;
            case OutputFormat.WebM:
                AddWebM(args, settings, width, height, keepAudio);
                break;
            case OutputFormat.Gif:
                AddGif(args, settings, width, height);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(settings));
        }

        args.Add("-progress");
        args.Add("pipe:1");
        args.Add("-nostats");
        args.Add(outputPath);
        return args;
    }

    private static void AddMp4(
        List<string> args,
        ReviewSettings settings,
        FfmpegCapabilities capabilities,
        int width,
        int height,
        bool keepAudio)
    {
        var encoder = capabilities.HasH264Mf ? "h264_mf" : "libopenh264";
        var bpp = settings.Quality switch
        {
            QualityPreset.High => 0.16,
            QualityPreset.Balanced => 0.12,
            QualityPreset.Small => 0.08,
            _ => 0.12
        };
        var bitrate = (int)Math.Clamp(width * (double)height * settings.FrameRate * bpp, 2_000_000, 40_000_000);

        args.Add("-vf");
        args.Add(BuildVideoFilter(settings, width, height));
        args.Add("-c:v");
        args.Add(encoder);
        args.Add("-b:v");
        args.Add(bitrate.ToString(CultureInfo.InvariantCulture));
        args.Add("-pix_fmt");
        args.Add("yuv420p");
        args.Add("-movflags");
        args.Add("+faststart");

        if (keepAudio)
            AddAudio(args, "aac", "128k");
    }

    private static void AddWebM(
        List<string> args,
        ReviewSettings settings,
        int width,
        int height,
        bool keepAudio)
    {
        var crf = settings.Quality switch
        {
            QualityPreset.High => "24",
            QualityPreset.Balanced => "32",
            QualityPreset.Small => "40",
            _ => "32"
        };

        args.Add("-vf");
        args.Add(BuildVideoFilter(settings, width, height));
        args.Add("-c:v");
        args.Add("libvpx-vp9");
        args.Add("-b:v");
        args.Add("0");
        args.Add("-crf");
        args.Add(crf);
        args.Add("-row-mt");
        args.Add("1");
        args.Add("-pix_fmt");
        args.Add("yuv420p");

        if (keepAudio)
            AddAudio(args, "libopus", "96k");
    }

    private static void AddGif(List<string> args, ReviewSettings settings, int width, int height)
    {
        var colors = settings.Quality switch
        {
            QualityPreset.High => 256,
            QualityPreset.Balanced => 128,
            QualityPreset.Small => 64,
            _ => 128
        };
        var scale = width != settings.SourceWidth || height != settings.SourceHeight
            ? $"scale={width}:{height}:flags=lanczos,"
            : string.Empty;

        var filter = $"[0:v]fps={settings.FrameRate},{scale}split[a][b];" +
                     $"[a]palettegen=max_colors={colors}:stats_mode=diff[p];" +
                     $"[b][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle";

        args.Add("-filter_complex");
        args.Add(filter);
        args.Add("-loop");
        args.Add("0");
    }

    private static void AddAudio(List<string> args, string encoder, string bitrate)
    {
        args.Add("-c:a");
        args.Add(encoder);
        args.Add("-b:a");
        args.Add(bitrate);
    }

    private static string BuildVideoFilter(ReviewSettings settings, int width, int height)
    {
        var scale = width != settings.SourceWidth || height != settings.SourceHeight
            ? $",scale={width}:{height}:flags=lanczos"
            : string.Empty;
        return $"fps={settings.FrameRate}{scale}";
    }

    private static (int Width, int Height) CalculateOutputSize(int sourceWidth, int sourceHeight, ScalePreset scale)
    {
        var factor = scale switch
        {
            ScalePreset.Percent100 => 1.0,
            ScalePreset.Percent75 => 0.75,
            ScalePreset.Percent50 => 0.5,
            ScalePreset.Fit960 => Math.Min(1.0, 960.0 / Math.Max(1, sourceWidth)),
            _ => 1.0
        };

        var width = Math.Max(2, (int)Math.Round(sourceWidth * factor));
        var height = Math.Max(2, (int)Math.Round(sourceHeight * factor));
        if (width % 2 != 0) width--;
        if (height % 2 != 0) height--;
        return (width, height);
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
    }
}
```

- [ ] **Step 6: Implement the exporter**

Create `src/ViewportCap.Encoding/FfmpegExporter.cs`:
```csharp
using ViewportCap.Core;

namespace ViewportCap.Encoding;

public sealed class FfmpegExporter : IExporter
{
    private readonly string _ffmpegPath;
    private readonly FfmpegCapabilities _capabilities;
    private readonly IProcessRunner _runner;

    public FfmpegExporter(
        string ffmpegPath,
        FfmpegCapabilities capabilities,
        IProcessRunner runner)
    {
        _ffmpegPath = ffmpegPath;
        _capabilities = capabilities;
        _runner = runner;
    }

    public async Task<ExportResult> ExportAsync(
        ExportPlan plan,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(plan.OutputPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new ExportException("The output path has no directory.");

        Directory.CreateDirectory(directory);

        if (!File.Exists(plan.Settings.SourcePath))
            throw new ExportException("The temporary recording is missing.");

        if (plan.CopySource)
        {
            File.Copy(plan.Settings.SourcePath, plan.OutputPath, true);
            return new ExportResult(plan.OutputPath);
        }

        var extension = Path.GetExtension(plan.OutputPath);
        var partialPath = Path.Combine(
            directory,
            $"{Path.GetFileNameWithoutExtension(plan.OutputPath)}.partial{extension}");

        if (File.Exists(partialPath))
            File.Delete(partialPath);

        var args = FfmpegArgumentBuilder.Build(plan.Settings, partialPath, _capabilities);
        var parser = new FfmpegProgressParser(plan.Settings.TrimDuration.TotalSeconds);

        try
        {
            var result = await _runner.RunAsync(
                _ffmpegPath,
                args,
                line =>
                {
                    parser.Feed(line);
                    if (parser.Percent is double percent)
                        progress?.Report(percent);
                },
                null,
                cancellationToken);

            if (result.ExitCode != 0)
                throw new ExportException($"FFmpeg failed: {Tail(result.StandardError)}");

            if (!File.Exists(partialPath))
                throw new ExportException("FFmpeg did not produce an output file.");

            File.Move(partialPath, plan.OutputPath, true);
            return new ExportResult(plan.OutputPath);
        }
        catch
        {
            if (File.Exists(partialPath))
                File.Delete(partialPath);
            throw;
        }
    }

    private static string Tail(string text)
    {
        const int max = 2000;
        return text.Length <= max ? text : text[^max..];
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run:
```powershell
dotnet test tests/ViewportCap.Encoding.Tests/ViewportCap.Encoding.Tests.csproj -c Debug
```

Expected: `Passed!`.

- [ ] **Step 8: Commit**

Run:
```powershell
git add .
git commit -m "feat: add ffmpeg argument builder and exporter"
```

---

### Task 6: ScreenRecorderLib capture adapter

**Files:**
- Create: `src/ViewportCap.Core/CaptureFailedEventArgs.cs`
- Create: `src/ViewportCap.Core/CaptureException.cs`
- Create: `src/ViewportCap.Core/CaptureRequest.cs`
- Create: `src/ViewportCap.Core/CaptureResult.cs`
- Create: `src/ViewportCap.Core/ICaptureEngine.cs`
- Create: `src/ViewportCap.Core/CaptureBitrateCalculator.cs`
- Create: `src/ViewportCap.Capture/ScreenRecorderOptionsFactory.cs`
- Create: `src/ViewportCap.Capture/ScreenRecorderCaptureEngine.cs`
- Create: `src/ViewportCap.Capture/CapturePreflight.cs`
- Test: `tests/ViewportCap.Core.Tests/CaptureBitrateCalculatorTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/ViewportCap.Core.Tests/CaptureBitrateCalculatorTests.cs`:
```csharp
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class CaptureBitrateCalculatorTests
{
    [Fact]
    public void SmallRegion_UsesMinimumBitrate()
    {
        Assert.Equal(4_000_000, CaptureBitrateCalculator.ForRegion(640, 480, 30));
    }

    [Fact]
    public void FullHd_UsesResolutionAwareBitrate()
    {
        Assert.Equal(7_464_960, CaptureBitrateCalculator.ForRegion(1920, 1080, 30));
    }

    [Fact]
    public void HugeRegion_UsesMaximumBitrate()
    {
        Assert.Equal(40_000_000, CaptureBitrateCalculator.ForRegion(7680, 4320, 30));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
```

Expected: build fails because `CaptureBitrateCalculator` does not exist.

- [ ] **Step 3: Implement capture contracts**

Create `src/ViewportCap.Core/CaptureException.cs`:
```csharp
namespace ViewportCap.Core;

public sealed class CaptureException : Exception
{
    public CaptureException(string message) : base(message)
    {
    }

    public CaptureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
```

Create `src/ViewportCap.Core/CaptureFailedEventArgs.cs`:
```csharp
namespace ViewportCap.Core;

public sealed class CaptureFailedEventArgs : EventArgs
{
    public CaptureFailedEventArgs(string error)
    {
        Error = error;
    }

    public string Error { get; }
}
```

Create `src/ViewportCap.Core/CaptureRequest.cs`:
```csharp
namespace ViewportCap.Core;

public sealed record CaptureRequest(
    CaptureRegion Region,
    string OutputPath,
    AudioMode AudioMode,
    string? MicrophoneDeviceName,
    string? SystemAudioDeviceName);
```

Create `src/ViewportCap.Core/CaptureResult.cs`:
```csharp
namespace ViewportCap.Core;

public sealed record CaptureResult(
    string FilePath,
    bool HasAudio,
    TimeSpan Duration,
    int Width,
    int Height);
```

Create `src/ViewportCap.Core/ICaptureEngine.cs`:
```csharp
namespace ViewportCap.Core;

public interface ICaptureEngine
{
    event EventHandler<CaptureFailedEventArgs>? Failed;

    Task StartAsync(CaptureRequest request, CancellationToken cancellationToken);
    Task<CaptureResult> StopAsync(CancellationToken cancellationToken);
    Task CancelAsync(bool deleteFile = true);
}
```

Create `src/ViewportCap.Core/CaptureBitrateCalculator.cs`:
```csharp
namespace ViewportCap.Core;

public static class CaptureBitrateCalculator
{
    public const int MinimumBitrate = 4_000_000;
    public const int MaximumBitrate = 40_000_000;
    public const double BitsPerPixel = 0.12;

    public static int ForRegion(int width, int height, int framesPerSecond)
    {
        var value = width * (double)height * framesPerSecond * BitsPerPixel;
        return (int)Math.Clamp(value, MinimumBitrate, MaximumBitrate);
    }
}
```

- [ ] **Step 4: Implement the options factory**

Create `src/ViewportCap.Capture/ScreenRecorderOptionsFactory.cs`:
```csharp
using ScreenRecorderLib;
using ViewportCap.Core;

namespace ViewportCap.Capture;

public static class ScreenRecorderOptionsFactory
{
    public const int CaptureFrameRate = 30;

    public static RecorderOptions Create(CaptureRequest request)
    {
        var audioSources = new List<AudioSourceBase>();

        if (request.AudioMode is AudioMode.Microphone or AudioMode.Both)
        {
            audioSources.Add(string.IsNullOrWhiteSpace(request.MicrophoneDeviceName)
                ? CaptureAudioSource.Default
                : new CaptureAudioSource(request.MicrophoneDeviceName));
        }

        if (request.AudioMode is AudioMode.System or AudioMode.Both)
        {
            audioSources.Add(string.IsNullOrWhiteSpace(request.SystemAudioDeviceName)
                ? LoopbackAudioSource.Default
                : new LoopbackAudioSource(request.SystemAudioDeviceName));
        }

        var source = new DisplayRecordingSource
        {
            DeviceName = request.Region.MonitorDeviceName,
            RecorderApi = RecorderApi.WindowsGraphicsCapture,
            IsCursorCaptureEnabled = true,
            IsBorderRequired = false
        };

        var options = new RecorderOptions
        {
            SourceOptions = new SourceOptions
            {
                RecordingSources = new List<RecordingSourceBase> { source }
            },
            OutputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video,
                SourceRect = new ScreenRect(
                    request.Region.SourceX,
                    request.Region.SourceY,
                    request.Region.Width,
                    request.Region.Height),
                OutputFrameSize = new ScreenSize(request.Region.Width, request.Region.Height),
                Stretch = StretchMode.None
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                Encoder = new H264VideoEncoder
                {
                    BitrateMode = H264BitrateControlMode.CBR,
                    EncoderProfile = H264Profile.High
                },
                Framerate = CaptureFrameRate,
                IsFixedFramerate = true,
                Bitrate = CaptureBitrateCalculator.ForRegion(
                    request.Region.Width,
                    request.Region.Height,
                    CaptureFrameRate),
                IsHardwareEncodingEnabled = true,
                IsThrottlingDisabled = false,
                IsLowLatencyEnabled = false,
                IsFragmentedMp4Enabled = true,
                IsMp4FastStartEnabled = true
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = audioSources.Count > 0,
                Bitrate = AudioBitrate.bitrate_128kbps,
                Channels = AudioChannels.Stereo
            },
            MouseOptions = new MouseOptions
            {
                IsMousePointerEnabled = true,
                IsMouseClicksDetected = false
            },
            LogOptions = new LogOptions
            {
                IsLogEnabled = false
            }
        };

        foreach (var audioSource in audioSources)
            options.AudioOptions.AudioSources.Add(audioSource);

        return options;
    }
}
```

- [ ] **Step 5: Implement the capture engine**

Create `src/ViewportCap.Capture/ScreenRecorderCaptureEngine.cs`:
```csharp
using System.Diagnostics;
using ScreenRecorderLib;
using ViewportCap.Core;

namespace ViewportCap.Capture;

public sealed class ScreenRecorderCaptureEngine : ICaptureEngine
{
    private readonly object _gate = new();
    private Recorder? _recorder;
    private TaskCompletionSource<CaptureResult>? _completion;
    private string? _outputPath;
    private bool _hasAudio;
    private int _width;
    private int _height;
    private Stopwatch? _stopwatch;

    public event EventHandler<CaptureFailedEventArgs>? Failed;

    public Task StartAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_recorder is not null)
                throw new InvalidOperationException("A recording is already active.");
            if (!request.Region.IsValid)
                throw new CaptureException("The selected region is too small.");

            var directory = Path.GetDirectoryName(request.OutputPath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new CaptureException("The recording path has no directory.");

            Directory.CreateDirectory(directory);
            if (File.Exists(request.OutputPath))
                File.Delete(request.OutputPath);

            var completion = new TaskCompletionSource<CaptureResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var recorder = Recorder.CreateRecorder(ScreenRecorderOptionsFactory.Create(request));
            recorder.OnRecordingComplete += OnRecordingComplete;
            recorder.OnRecordingFailed += OnRecordingFailed;

            _recorder = recorder;
            _completion = completion;
            _outputPath = request.OutputPath;
            _hasAudio = request.AudioMode != AudioMode.Off;
            _width = request.Region.Width;
            _height = request.Region.Height;
            _stopwatch = Stopwatch.StartNew();

            try
            {
                recorder.Record(request.OutputPath);
            }
            catch
            {
                CleanupAfterStartFailure();
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public async Task<CaptureResult> StopAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<CaptureResult> completion;

        lock (_gate)
        {
            if (_recorder is null || _completion is null)
                throw new InvalidOperationException("No recording is active.");

            completion = _completion;
            _stopwatch?.Stop();

            try
            {
                _recorder.Stop();
            }
            catch (Exception ex)
            {
                completion.TrySetException(new CaptureException("Could not stop the recording.", ex));
            }
        }

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
        finally
        {
            DisposeRecorder();
        }
    }

    public async Task CancelAsync(bool deleteFile = true)
    {
        string? path;
        lock (_gate)
        {
            path = _outputPath;
            try
            {
                _recorder?.Stop();
            }
            catch
            {
                // The recorder may already have stopped.
            }
        }

        var completion = _completion;
        if (completion is not null)
        {
            try
            {
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Cancellation must always win.
            }
        }

        DisposeRecorder();

        if (deleteFile && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // A locked temp file is cleaned up at the next startup.
            }
        }
    }

    private void OnRecordingComplete(object? sender, RecordingCompleteEventArgs e)
    {
        var result = new CaptureResult(
            e.FilePath ?? _outputPath ?? string.Empty,
            _hasAudio,
            _stopwatch?.Elapsed ?? TimeSpan.Zero,
            _width,
            _height);
        _completion?.TrySetResult(result);
    }

    private void OnRecordingFailed(object? sender, RecordingFailedEventArgs e)
    {
        _completion?.TrySetException(new CaptureException(e.Error));
        Failed?.Invoke(this, new CaptureFailedEventArgs(e.Error));
    }

    private void DisposeRecorder()
    {
        lock (_gate)
        {
            if (_recorder is not null)
            {
                _recorder.OnRecordingComplete -= OnRecordingComplete;
                _recorder.OnRecordingFailed -= OnRecordingFailed;
                _recorder.Dispose();
            }

            _recorder = null;
            _completion = null;
            _outputPath = null;
            _width = 0;
            _height = 0;
            _stopwatch = null;
        }
    }

    private void CleanupAfterStartFailure()
    {
        DisposeRecorder();
    }
}
```

- [ ] **Step 6: Implement preflight**

Create `src/ViewportCap.Capture/CapturePreflight.cs`:
```csharp
using ScreenRecorderLib;
using ViewportCap.Core;

namespace ViewportCap.Capture;

public static class CapturePreflight
{
    public static void EnsureNativeLibraryLoads()
    {
        _ = Recorder.GetDisplays();
    }

    public static void EnsureDiskSpace(string path, long minimumBytes = 1_000_000_000)
    {
        var root = Path.GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root))
            throw new CaptureException("Cannot determine the recording drive.");

        var drive = new DriveInfo(root);
        if (drive.AvailableFreeSpace < minimumBytes)
            throw new CaptureException("At least 1 GB of free disk space is required.");
    }
}
```

- [ ] **Step 7: Run tests and build capture**

Run:
```powershell
dotnet test tests/ViewportCap.Core.Tests/ViewportCap.Core.Tests.csproj -c Debug
dotnet build src/ViewportCap.Capture/ViewportCap.Capture.csproj -c Release
```

Expected: tests pass and capture builds with `0 Error(s)`.

- [ ] **Step 8: Commit**

Run:
```powershell
git add .
git commit -m "feat: add ScreenRecorderLib capture adapter"
```

---

### Task 7: Windows platform services

**Files:**
- Create: `src/ViewportCap.App/Services/AppPaths.cs`
- Create: `src/ViewportCap.App/Services/FileLog.cs`
- Create: `src/ViewportCap.App/Services/WindowAffinity.cs`
- Create: `src/ViewportCap.App/Services/DisplayService.cs`
- Create: `src/ViewportCap.App/Services/SingleInstanceService.cs`
- Create: `src/ViewportCap.App/Services/HotkeyService.cs`
- Create: `src/ViewportCap.App/Services/StartupService.cs`
- Create: `src/ViewportCap.App/Services/TrayService.cs`

- [ ] **Step 1: Implement app paths and logging**

Create `src/ViewportCap.App/Services/AppPaths.cs`:
```csharp
namespace ViewportCap.App.Services;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ViewportCap");

    public static string SettingsFile => Path.Combine(Root, "settings.json");
    public static string RecordingsDirectory => Path.Combine(Root, "recordings");
    public static string LogsDirectory => Path.Combine(Root, "logs");
    public static string LogFile => Path.Combine(LogsDirectory, "viewport-cap.log");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(RecordingsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
```

Create `src/ViewportCap.App/Services/FileLog.cs`:
```csharp
namespace ViewportCap.App.Services;

public static class FileLog
{
    private static readonly object Gate = new();
    private const long MaxBytes = 2 * 1024 * 1024;

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message) => Write("WARN", message, null);
    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (Gate)
            {
                AppPaths.EnsureCreated();
                RotateIfNeeded();
                var line = $"{DateTimeOffset.Now:O} [{level}] {message}";
                if (exception is not null)
                    line += Environment.NewLine + exception;
                File.AppendAllText(AppPaths.LogFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never take down the app.
        }
    }

    private static void RotateIfNeeded()
    {
        var file = new FileInfo(AppPaths.LogFile);
        if (!file.Exists || file.Length < MaxBytes)
            return;

        for (var index = 4; index >= 1; index--)
        {
            var from = index == 1
                ? AppPaths.LogFile
                : Path.Combine(AppPaths.LogsDirectory, $"viewport-cap.{index}.log");
            var to = Path.Combine(AppPaths.LogsDirectory, $"viewport-cap.{index + 1}.log");
            if (File.Exists(from))
                File.Move(from, to, true);
        }
    }
}
```

- [ ] **Step 2: Implement capture exclusion and display helpers**

Create `src/ViewportCap.App/Services/WindowAffinity.cs`:
```csharp
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ViewportCap.App.Services;

public static class WindowAffinity
{
    private const uint WdaExcludeFromCapture = 0x00000011;

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    public static void ExcludeFromCapture(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
            SetWindowDisplayAffinity(handle, WdaExcludeFromCapture);
    }
}
```

Create `src/ViewportCap.App/Services/DisplayService.cs`:
```csharp
using System.Runtime.InteropServices;
using System.Windows;
using ViewportCap.Core;

namespace ViewportCap.App.Services;

public static class DisplayService
{
    private const uint MonitorDefaultToNearest = 2;
    private const int MdtEffectiveDpi = 0;

    public static IReadOnlyList<MonitorDescriptor> GetMonitors()
    {
        var monitors = new List<MonitorDescriptor>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr handle, IntPtr hdc, ref Rect rect, IntPtr data) =>
        {
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(handle, ref info))
            {
                monitors.Add(new MonitorDescriptor(
                    info.DeviceName,
                    info.Monitor.Left,
                    info.Monitor.Top,
                    info.Monitor.Right - info.Monitor.Left,
                    info.Monitor.Bottom - info.Monitor.Top,
                    GetDpi(handle),
                    (info.Flags & 1) != 0));
            }

            return true;
        }, IntPtr.Zero);

        if (monitors.Count == 0)
        {
            monitors.Add(new MonitorDescriptor(
                @"\\.\DISPLAY1",
                0,
                0,
                GetSystemMetrics(0),
                GetSystemMetrics(1),
                96,
                true));
        }

        return monitors;
    }

    public static MonitorDescriptor GetMonitorForCursor()
    {
        GetCursorPos(out var point);
        var handle = MonitorFromPoint(point, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };

        if (handle != IntPtr.Zero && GetMonitorInfo(handle, ref info))
        {
            return new MonitorDescriptor(
                info.DeviceName,
                info.Monitor.Left,
                info.Monitor.Top,
                info.Monitor.Right - info.Monitor.Left,
                info.Monitor.Bottom - info.Monitor.Top,
                GetDpi(handle),
                (info.Flags & 1) != 0);
        }

        return GetMonitors()[0];
    }

    public static Rect GetWindowBounds(MonitorDescriptor monitor)
    {
        var scale = monitor.Scale;
        return new Rect(
            monitor.Left / scale,
            monitor.Top / scale,
            monitor.Width / scale,
            monitor.Height / scale);
    }

    public static (double Left, double Top) GetNearbyPosition(CaptureRegion region, double width, double height)
    {
        var scale = region.Scale;
        var left = region.X / scale;
        var top = region.Y / scale;
        var right = region.Right / scale;
        var bottom = region.Bottom / scale;

        var workspaceLeft = SystemParameters.VirtualScreenLeft;
        var workspaceTop = SystemParameters.VirtualScreenTop;
        var workspaceRight = workspaceLeft + SystemParameters.VirtualScreenWidth;
        var workspaceBottom = workspaceTop + SystemParameters.VirtualScreenHeight;

        var candidateLeft = right + 12;
        if (candidateLeft + width > workspaceRight)
            candidateLeft = left - width - 12;

        var candidateTop = top;
        if (candidateTop + height > workspaceBottom)
            candidateTop = bottom - height;

        return (
            Math.Max(workspaceLeft + 8, candidateLeft),
            Math.Max(workspaceTop + 8, candidateTop));
    }

    private static int GetDpi(IntPtr monitor)
    {
        return GetDpiForMonitor(monitor, MdtEffectiveDpi, out var dpiX, out _) == 0 && dpiX > 0
            ? (int)dpiX
            : 96;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect rect, IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point point, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public int Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }
}
```

- [ ] **Step 3: Implement single instance**

Create `src/ViewportCap.App/Services/SingleInstanceService.cs`:
```csharp
namespace ViewportCap.App.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\ViewportCap.SingleInstance";
    private const string EventName = @"Local\ViewportCap.Activate";

    private Mutex? _mutex;
    private EventWaitHandle? _activateEvent;
    private RegisteredWaitHandle? _wait;

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            SignalExistingInstance();
            return false;
        }

        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        return true;
    }

    public void Listen(Action onActivate)
    {
        if (_activateEvent is null)
            return;

        _wait = ThreadPool.RegisterWaitForSingleObject(
            _activateEvent,
            (_, _) => onActivate(),
            null,
            Timeout.Infinite,
            false);
    }

    public void Dispose()
    {
        _wait?.Unregister(null);
        _activateEvent?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(EventName);
            handle.Set();
        }
        catch
        {
            // The other instance may be shutting down.
        }
    }
}
```

- [ ] **Step 4: Implement hotkey and startup**

Create `src/ViewportCap.App/Services/HotkeyService.cs`:
```csharp
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ViewportCap.Core;

namespace ViewportCap.App.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x9000;
    private const int WmHotkey = 0x0312;

    private HwndSource? _source;
    private Action? _callback;
    private HotkeyGesture? _current;

    public bool TryRegister(HotkeyGesture gesture, Action callback)
    {
        EnsureSource();

        if (_current is { } previous)
            UnregisterHotKey(_source!.Handle, HotkeyId);

        if (!RegisterHotKey(_source!.Handle, HotkeyId, (uint)gesture.Modifiers, gesture.VirtualKey))
        {
            if (_current is { } old)
                RegisterHotKey(_source.Handle, HotkeyId, (uint)old.Modifiers, old.VirtualKey);
            return false;
        }

        _current = gesture;
        _callback = callback;
        return true;
    }

    public void Unregister()
    {
        if (_source is not null)
            UnregisterHotKey(_source.Handle, HotkeyId);
        _current = null;
    }

    public void Dispose()
    {
        if (_source is null)
            return;

        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _source = null;
    }

    private void EnsureSource()
    {
        if (_source is not null)
            return;

        var parameters = new HwndSourceParameters("ViewportCapHotkey")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _callback?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
```

Create `src/ViewportCap.App/Services/StartupService.cs`:
```csharp
using Microsoft.Win32;

namespace ViewportCap.App.Services;

public sealed class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "viewport-cap";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (enabled)
        {
            var executable = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine the executable path.");
            key.SetValue(ValueName, $"\"{executable}\" --tray");
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }
}
```

- [ ] **Step 5: Implement the tray icon**

Create `src/ViewportCap.App/Services/TrayService.cs`:
```csharp
using System.Drawing;
using System.Windows.Forms;

namespace ViewportCap.App.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _startupItem;

    public event Action? CaptureRequested;
    public event Action? SettingsRequested;
    public event Action? OpenRecordingsRequested;
    public event Action? ExitRequested;
    public event Action<bool>? StartWithWindowsChanged;

    public TrayService(string hotkeyText, bool startWithWindows)
    {
        var menu = new ContextMenuStrip();

        var captureItem = new ToolStripMenuItem("Capture now");
        captureItem.Click += (_, _) => CaptureRequested?.Invoke();

        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke();

        var recordingsItem = new ToolStripMenuItem("Open recordings folder");
        recordingsItem.Click += (_, _) => OpenRecordingsRequested?.Invoke();

        _startupItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = startWithWindows,
            CheckOnClick = true
        };
        _startupItem.Click += (_, _) => StartWithWindowsChanged?.Invoke(_startupItem.Checked);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(captureItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(recordingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = $"viewport-cap ({hotkeyText})",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
                CaptureRequested?.Invoke();
        };
    }

    public void UpdateStartWithWindows(bool enabled)
    {
        _startupItem.Checked = enabled;
    }

    public void UpdateHotkey(string hotkeyText)
    {
        _icon.Text = $"viewport-cap ({hotkeyText})";
    }

    public void ShowBalloon(string title, string text)
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = text;
        _icon.BalloonTipIcon = ToolTipIcon.Info;
        _icon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static Icon LoadIcon()
    {
        var path = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(path))
        {
            var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is not null)
                return icon;
        }

        return SystemIcons.Application;
    }
}
```

- [ ] **Step 6: Build the app project**

Run:
```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Release
```

Expected: `Build succeeded` with `0 Error(s)`. The stock `MainWindow` is still present at this point; Task 8 replaces it.

- [ ] **Step 7: Commit**

Run:
```powershell
git add .
git commit -m "feat: add Windows tray and platform services"
```

---

### Task 8: App shell, tray wiring, and settings window

**Files:**
- Modify: `src/ViewportCap.App/App.xaml`
- Modify: `src/ViewportCap.App/App.xaml.cs`
- Create: `src/ViewportCap.App/Windows/SettingsWindow.xaml`
- Create: `src/ViewportCap.App/Windows/SettingsWindow.xaml.cs`
- Delete: `src/ViewportCap.App/MainWindow.xaml`
- Delete: `src/ViewportCap.App/MainWindow.xaml.cs`
- Modify: `src/ViewportCap.App/Services/HotkeyService.cs`

- [ ] **Step 1: Replace `App.xaml`**

Modify `src/ViewportCap.App/App.xaml` to:
```xml
<Application x:Class="ViewportCap.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
  <Application.Resources>
    <SolidColorBrush x:Key="PanelBrush" Color="#FF111827" />
    <SolidColorBrush x:Key="BorderBrush" Color="#FF334155" />
    <SolidColorBrush x:Key="MutedBrush" Color="#FF94A3B8" />
    <Style TargetType="Window">
      <Setter Property="FontFamily" Value="Segoe UI" />
      <Setter Property="Foreground" Value="#FFF8FAFC" />
      <Setter Property="Background" Value="#FF0B1220" />
    </Style>
    <Style TargetType="TextBlock">
      <Setter Property="Foreground" Value="#FFF8FAFC" />
    </Style>
    <Style TargetType="Button">
      <Setter Property="Background" Value="#FF1E293B" />
      <Setter Property="Foreground" Value="#FFF8FAFC" />
      <Setter Property="BorderBrush" Value="#FF334155" />
      <Setter Property="Padding" Value="12,6" />
      <Setter Property="Margin" Value="0,0,8,0" />
    </Style>
    <Style TargetType="TextBox">
      <Setter Property="Background" Value="#FF0F172A" />
      <Setter Property="Foreground" Value="#FFF8FAFC" />
      <Setter Property="BorderBrush" Value="#FF334155" />
      <Setter Property="Padding" Value="6,4" />
    </Style>
    <Style TargetType="ComboBox">
      <Setter Property="Background" Value="#FF0F172A" />
      <Setter Property="Foreground" Value="#FFF8FAFC" />
      <Setter Property="BorderBrush" Value="#FF334155" />
      <Setter Property="Padding" Value="4" />
    </Style>
    <Style TargetType="CheckBox">
      <Setter Property="Foreground" Value="#FFF8FAFC" />
    </Style>
  </Application.Resources>
</Application>
```

- [ ] **Step 2: Replace `App.xaml.cs`**

Modify `src/ViewportCap.App/App.xaml.cs` to:
```csharp
using System.Diagnostics;
using System.Windows;
using ViewportCap.App.Services;
using ViewportCap.App.Windows;
using ViewportCap.Core;

namespace ViewportCap.App;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private HotkeyService? _hotkey;
    private TrayService? _tray;
    private StartupService? _startup;
    private ISettingsStore? _settingsStore;
    private AppSettings? _settings;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        AppPaths.EnsureCreated();
        FileLog.Info("viewport-cap starting.");

        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.TryAcquire())
        {
            Shutdown();
            return;
        }

        _singleInstance.Listen(() => Dispatcher.Invoke(ShowSettings));

        _settingsStore = new JsonSettingsStore(AppPaths.SettingsFile);
        _settings = _settingsStore.Load();
        _startup = new StartupService();

        try
        {
            _startup.SetEnabled(_settings.StartWithWindows);
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not update Start with Windows.", ex);
        }

        _hotkey = new HotkeyService();
        if (!HotkeyGesture.TryParse(_settings.Hotkey, out var gesture))
            gesture = HotkeyGesture.Default;

        if (!_hotkey.TryRegister(gesture, OnHotkey))
        {
            var message = $"The hotkey {gesture} is already in use. Choose another hotkey in Settings.";
            FileLog.Warn(message);
            MessageBox.Show(message, "viewport-cap", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _tray = new TrayService(_settings.Hotkey, _settings.StartWithWindows);
        _tray.CaptureRequested += OnCaptureRequested;
        _tray.SettingsRequested += ShowSettings;
        _tray.OpenRecordingsRequested += OpenRecordingsFolder;
        _tray.StartWithWindowsChanged += OnStartWithWindowsChanged;
        _tray.ExitRequested += () => Shutdown();

        if (!_settings.HasShownFirstRunTip)
        {
            _tray.ShowBalloon("viewport-cap", $"Press {_settings.Hotkey} to capture a region.");
            _settings.HasShownFirstRunTip = true;
            _settingsStore.Save(_settings);
        }

        FileLog.Info("viewport-cap ready.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _hotkey?.Dispose();
        _singleInstance?.Dispose();
        FileLog.Info("viewport-cap stopped.");
        base.OnExit(e);
    }

    private void OnHotkey()
    {
        OnCaptureRequested();
    }

    private void OnCaptureRequested()
    {
        // Task 10 replaces this placeholder with CaptureFlowController.
        FileLog.Info("Capture requested.");
    }

    private void ShowSettings()
    {
        if (_settings is null || _settingsStore is null || _hotkey is null)
            return;

        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _settings,
            _settingsStore,
            _hotkey.CanRegister,
            OnSettingsSaved);

        _settingsWindow.ShowDialog();
        _settingsWindow = null;
    }

    private void OnSettingsSaved(AppSettings updated)
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
        }

        if (_hotkey is not null && HotkeyGesture.TryParse(updated.Hotkey, out var gesture))
        {
            if (!_hotkey.TryRegister(gesture, OnHotkey))
            {
                MessageBox.Show(
                    $"The hotkey {gesture} is already in use.",
                    "viewport-cap",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void OnStartWithWindowsChanged(bool enabled)
    {
        if (_settings is null || _settingsStore is null)
            return;

        _settings.StartWithWindows = enabled;
        _settingsStore.Save(_settings);

        try
        {
            _startup?.SetEnabled(enabled);
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
    }

    private void OpenRecordingsFolder()
    {
        if (_settings is null)
            return;

        Directory.CreateDirectory(_settings.OutputFolder);
        Process.Start(new ProcessStartInfo
        {
            FileName = _settings.OutputFolder,
            UseShellExecute = true
        });
    }
}
```

- [ ] **Step 3: Add a non-mutating hotkey test**

Modify `src/ViewportCap.App/Services/HotkeyService.cs` by adding this method after `TryRegister`:
```csharp
    public bool CanRegister(HotkeyGesture gesture)
    {
        EnsureSource();

        if (_current is { } current)
            UnregisterHotKey(_source!.Handle, HotkeyId);

        var registered = RegisterHotKey(
            _source!.Handle,
            HotkeyId,
            (uint)gesture.Modifiers,
            gesture.VirtualKey);

        if (registered)
            UnregisterHotKey(_source.Handle, HotkeyId);

        if (_current is { } previous)
        {
            RegisterHotKey(
                _source!.Handle,
                HotkeyId,
                (uint)previous.Modifiers,
                previous.VirtualKey);
        }

        return registered;
    }
```

- [ ] **Step 4: Create the settings window XAML**

Create `src/ViewportCap.App/Windows/SettingsWindow.xaml`:
```xml
<Window x:Class="ViewportCap.App.Windows.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="viewport-cap settings"
        Width="580"
        Height="620"
        MinWidth="520"
        MinHeight="520"
        WindowStartupLocation="CenterScreen"
        ResizeMode="CanResize"
        Icon="pack://application:,,,/Assets/app.ico">
  <Grid Margin="18">
    <Grid.RowDefinitions>
      <RowDefinition Height="*" />
      <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>

    <ScrollViewer Grid.Row="0" VerticalScrollBarVisibility="Auto">
      <StackPanel>
        <TextBlock Text="viewport-cap" FontSize="24" FontWeight="SemiBold" />
        <TextBlock Text="Capture a region from anywhere."
                   Foreground="{StaticResource MutedBrush}"
                   Margin="0,2,0,18" />

        <TextBlock Text="Global hotkey" FontWeight="SemiBold" Margin="0,0,0,4" />
        <TextBox x:Name="HotkeyBox"
                 IsReadOnly="True"
                 PreviewKeyDown="HotkeyBox_PreviewKeyDown"
                 Margin="0,0,0,14" />
        <TextBlock Text="Click the box, then press a key combination with Ctrl, Alt, Shift, or Win."
                   Foreground="{StaticResource MutedBrush}"
                   FontSize="12"
                   Margin="0,-10,0,14" />

        <TextBlock Text="Output folder" FontWeight="SemiBold" Margin="0,0,0,4" />
        <Grid Margin="0,0,0,14">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
          </Grid.ColumnDefinitions>
          <TextBox x:Name="OutputFolderBox" Grid.Column="0" />
          <Button Grid.Column="1" Content="Browse..." Click="Browse_Click" />
        </Grid>

        <TextBlock Text="Countdown (seconds)" FontWeight="SemiBold" Margin="0,0,0,4" />
        <ComboBox x:Name="CountdownBox" Margin="0,0,0,14" />

        <TextBlock Text="Default export format" FontWeight="SemiBold" Margin="0,0,0,4" />
        <ComboBox x:Name="FormatBox" Margin="0,0,0,14" />

        <TextBlock Text="Audio at capture time" FontWeight="SemiBold" Margin="0,0,0,4" />
        <ComboBox x:Name="AudioModeBox" Margin="0,0,0,14" />

        <TextBlock Text="Microphone" FontWeight="SemiBold" Margin="0,0,0,4" />
        <ComboBox x:Name="MicrophoneBox" Margin="0,0,0,14" />

        <TextBlock Text="System audio output" FontWeight="SemiBold" Margin="0,0,0,4" />
        <ComboBox x:Name="SystemAudioBox" Margin="0,0,0,14" />

        <CheckBox x:Name="StartWithWindowsBox"
                  Content="Start with Windows"
                  Margin="0,4,0,16" />

        <TextBlock x:Name="VersionText"
                   Foreground="{StaticResource MutedBrush}"
                   FontSize="12" />
      </StackPanel>
    </ScrollViewer>

    <Grid Grid.Row="1" Margin="0,12,0,0">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
      </Grid.ColumnDefinitions>
      <Button Grid.Column="0"
              Content="Open logs folder"
              HorizontalAlignment="Left"
              Click="OpenLogs_Click" />
      <StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Content="Cancel" Click="Cancel_Click" />
        <Button Content="Save" Click="Save_Click" />
      </StackPanel>
    </Grid>
  </Grid>
</Window>
```

- [ ] **Step 5: Create the settings code-behind**

Create `src/ViewportCap.App/Windows/SettingsWindow.xaml.cs`:
```csharp
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ScreenRecorderLib;
using ViewportCap.App.Services;
using ViewportCap.Core;

namespace ViewportCap.App.Windows;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _original;
    private readonly ISettingsStore _store;
    private readonly Func<HotkeyGesture, bool> _hotkeyTester;
    private readonly Action<AppSettings> _onSaved;
    private readonly List<AudioDeviceOption> _microphones = new();
    private readonly List<AudioDeviceOption> _systemAudio = new();

    public SettingsWindow(
        AppSettings settings,
        ISettingsStore store,
        Func<HotkeyGesture, bool> hotkeyTester,
        Action<AppSettings> onSaved)
    {
        InitializeComponent();

        _original = settings;
        _store = store;
        _hotkeyTester = hotkeyTester;
        _onSaved = onSaved;

        HotkeyBox.Text = settings.Hotkey;
        OutputFolderBox.Text = settings.OutputFolder;
        CountdownBox.ItemsSource = new[] { 0, 3, 5, 10 };
        CountdownBox.SelectedItem = settings.CountdownSeconds;
        FormatBox.ItemsSource = Enum.GetValues<OutputFormat>();
        FormatBox.SelectedItem = settings.DefaultFormat;
        AudioModeBox.ItemsSource = Enum.GetValues<AudioMode>();
        AudioModeBox.SelectedItem = settings.AudioMode;
        StartWithWindowsBox.IsChecked = settings.StartWithWindows;
        VersionText.Text = $"Version {typeof(SettingsWindow).Assembly.GetName().Version?.ToString(3) ?? "0.1.0"}";

        FillAudioDevices(settings);
    }

    private void FillAudioDevices(AppSettings settings)
    {
        AddDefault(_microphones, settings.MicrophoneDeviceName);
        AddDefault(_systemAudio, settings.SystemAudioDeviceName);

        try
        {
            foreach (var device in Recorder.GetSystemAudioCaptureDevices())
                _microphones.Add(new AudioDeviceOption(device.FriendlyName, device.DeviceName));
        }
        catch
        {
            // The default device is enough if enumeration fails.
        }

        try
        {
            foreach (var device in Recorder.GetSystemAudioLoopbackDevices())
                _systemAudio.Add(new AudioDeviceOption(device.FriendlyName, device.DeviceName));
        }
        catch
        {
            // The default device is enough if enumeration fails.
        }

        MicrophoneBox.ItemsSource = _microphones;
        MicrophoneBox.DisplayMemberPath = nameof(AudioDeviceOption.Name);
        MicrophoneBox.SelectedItem = FindDevice(_microphones, settings.MicrophoneDeviceName);

        SystemAudioBox.ItemsSource = _systemAudio;
        SystemAudioBox.DisplayMemberPath = nameof(AudioDeviceOption.Name);
        SystemAudioBox.SelectedItem = FindDevice(_systemAudio, settings.SystemAudioDeviceName);
    }

    private static void AddDefault(List<AudioDeviceOption> devices, string? selectedDevice)
    {
        devices.Add(new AudioDeviceOption("Default device", null));
        if (!string.IsNullOrWhiteSpace(selectedDevice))
            devices.Add(new AudioDeviceOption($"Saved: {selectedDevice}", selectedDevice));
    }

    private static AudioDeviceOption FindDevice(List<AudioDeviceOption> devices, string? deviceName)
    {
        return devices.FirstOrDefault(device =>
            string.Equals(device.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
            ?? devices[0];
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= HotkeyModifiers.Control;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= HotkeyModifiers.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= HotkeyModifiers.Shift;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) modifiers |= HotkeyModifiers.Win;

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        var gesture = new HotkeyGesture(modifiers, virtualKey);
        if (!HotkeyGesture.TryParse(gesture.ToString(), out _))
            return;

        HotkeyBox.Text = gesture.ToString();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            SelectedPath = OutputFolderBox.Text,
            Description = "Choose the folder for exported recordings"
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            OutputFolderBox.Text = dialog.SelectedPath;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!HotkeyGesture.TryParse(HotkeyBox.Text, out var hotkey))
        {
            MessageBox.Show("Enter a valid hotkey with at least one modifier.", "viewport-cap");
            return;
        }

        if (!_hotkeyTester(hotkey))
        {
            MessageBox.Show($"{hotkey} is already in use. Choose another combination.", "viewport-cap");
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputFolderBox.Text))
        {
            MessageBox.Show("Choose an output folder.", "viewport-cap");
            return;
        }

        var updated = new AppSettings
        {
            SchemaVersion = 1,
            Hotkey = hotkey.ToString(),
            OutputFolder = OutputFolderBox.Text.Trim(),
            CountdownSeconds = CountdownBox.SelectedItem is int count ? count : 3,
            DefaultFormat = FormatBox.SelectedItem is OutputFormat format ? format : OutputFormat.Mp4,
            AudioMode = AudioModeBox.SelectedItem is AudioMode audio ? audio : AudioMode.Off,
            MicrophoneDeviceName = (MicrophoneBox.SelectedItem as AudioDeviceOption)?.DeviceName,
            SystemAudioDeviceName = (SystemAudioBox.SelectedItem as AudioDeviceOption)?.DeviceName,
            StartWithWindows = StartWithWindowsBox.IsChecked == true,
            HasShownFirstRunTip = _original.HasShownFirstRunTip
        };
        updated.Normalize();

        try
        {
            Directory.CreateDirectory(updated.OutputFolder);
            _store.Save(updated);
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not save settings.", ex);
            MessageBox.Show($"Could not save settings:\n\n{ex.Message}", "viewport-cap");
            return;
        }

        _onSaved(updated);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppPaths.LogsDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = AppPaths.LogsDirectory,
            UseShellExecute = true
        });
    }

    private sealed record AudioDeviceOption(string Name, string? DeviceName);
}
```

- [ ] **Step 6: Remove the stock main window**

Run:
```powershell
Remove-Item src/ViewportCap.App/MainWindow.xaml
Remove-Item src/ViewportCap.App/MainWindow.xaml.cs
```

Expected: both files are removed.

- [ ] **Step 7: Build and smoke-test settings**

Run:
```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
dotnet run --project src/ViewportCap.App/ViewportCap.App.csproj
```

Expected: the app starts with a tray icon and no main window. Open the tray menu, choose `Settings...`, and confirm the settings window appears. Change the countdown and save; reopen Settings and confirm the value persisted. Close the app from the tray menu.

- [ ] **Step 8: Commit**

Run:
```powershell
git add .
git commit -m "feat: add tray shell and settings window"
```

---

### Task 9: Region overlay, selector, and countdown

**Files:**
- Create: `src/ViewportCap.App/Windows/RegionOverlayWindow.xaml`
- Create: `src/ViewportCap.App/Windows/RegionOverlayWindow.xaml.cs`
- Create: `src/ViewportCap.App/Windows/RegionSelector.cs`
- Create: `src/ViewportCap.App/Windows/CountdownWindow.xaml`
- Create: `src/ViewportCap.App/Windows/CountdownWindow.xaml.cs`

- [ ] **Step 1: Create the overlay XAML**

Create `src/ViewportCap.App/Windows/RegionOverlayWindow.xaml`:
```xml
<Window x:Class="ViewportCap.App.Windows.RegionOverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Select region"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ShowActivated="False"
        ResizeMode="NoResize"
        WindowStartupLocation="Manual"
        Cursor="Cross"
        MouseLeftButtonDown="Window_MouseLeftButtonDown"
        MouseMove="Window_MouseMove"
        MouseLeftButtonUp="Window_MouseLeftButtonUp"
        MouseRightButtonDown="Window_MouseRightButtonDown"
        KeyDown="Window_KeyDown">
  <Canvas x:Name="RootCanvas" Background="Transparent">
    <Rectangle x:Name="FullShade" Fill="#66000000" />
    <Rectangle x:Name="TopShade" Fill="#66000000" Visibility="Collapsed" />
    <Rectangle x:Name="BottomShade" Fill="#66000000" Visibility="Collapsed" />
    <Rectangle x:Name="LeftShade" Fill="#66000000" Visibility="Collapsed" />
    <Rectangle x:Name="RightShade" Fill="#66000000" Visibility="Collapsed" />

    <Rectangle x:Name="SelectionBorder"
               Stroke="#FF60A5FA"
               StrokeThickness="1.5"
               Visibility="Collapsed" />

    <Border x:Name="SizeBadge"
            Background="#DD0B1220"
            BorderBrush="#FF60A5FA"
            BorderThickness="1"
            CornerRadius="4"
            Padding="6,2"
            Visibility="Collapsed">
      <TextBlock x:Name="SizeText" FontSize="12" Foreground="#FFF8FAFC" />
    </Border>

    <TextBlock x:Name="HintText"
               Text="Drag at least 32 x 32 pixels"
               Background="#DD0B1220"
               Foreground="#FFF8FAFC"
               Padding="8,4"
               Visibility="Collapsed" />
  </Canvas>
</Window>
```

- [ ] **Step 2: Create the overlay code-behind**

Create `src/ViewportCap.App/Windows/RegionOverlayWindow.xaml.cs`:
```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ViewportCap.App.Services;
using ViewportCap.Core;

namespace ViewportCap.App.Windows;

public partial class RegionOverlayWindow : Window
{
    private readonly MonitorDescriptor _monitor;
    private bool _dragging;
    private Point _start;
    private DispatcherTimer? _hintTimer;

    public event Action<CaptureRegion>? SelectionCompleted;
    public event Action? Cancelled;

    public MonitorDescriptor Monitor => _monitor;

    public RegionOverlayWindow(MonitorDescriptor monitor)
    {
        InitializeComponent();
        _monitor = monitor;

        var bounds = DisplayService.GetWindowBounds(monitor);
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        SourceInitialized += (_, _) => WindowAffinity.ExcludeFromCapture(this);
        Loaded += (_, _) =>
        {
            UpdateShades(default, false);
            Focus();
        };
        SizeChanged += (_, _) => UpdateShades(default, false);
    }

    public void EnableInput(bool enabled)
    {
        IsHitTestVisible = enabled;
        Cursor = enabled ? Cursors.Cross : Cursors.Arrow;
        if (enabled)
        {
            Activate();
            Focus();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsHitTestVisible)
            return;

        _dragging = true;
        _start = e.GetPosition(RootCanvas);
        CaptureMouse();
        UpdateSelection(_start, _start);
        e.Handled = true;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;

        UpdateSelection(_start, e.GetPosition(RootCanvas));
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
            return;

        _dragging = false;
        ReleaseMouseCapture();

        var end = e.GetPosition(RootCanvas);
        var rect = RegionMath.Normalize(_start.X, _start.Y, end.X, end.Y);
        var region = RegionMath.ToPhysical(rect, _monitor);

        if (region.IsValid)
        {
            SelectionCompleted?.Invoke(region);
        }
        else
        {
            ShowHint();
            UpdateShades(default, false);
        }

        e.Handled = true;
    }

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Cancelled?.Invoke();
        e.Handled = true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancelled?.Invoke();
            e.Handled = true;
        }
    }

    private void UpdateSelection(Point start, Point end)
    {
        var rect = RegionMath.Normalize(start.X, start.Y, end.X, end.Y);
        UpdateShades(rect, true);

        var physical = RegionMath.ToPhysical(rect, _monitor);
        SizeText.Text = $"{physical.Width} x {physical.Height}";

        var badgeLeft = Math.Min(rect.Left, Math.Max(0, RootCanvas.ActualWidth - 90));
        var badgeTop = rect.Top > 30 ? rect.Top - 28 : rect.Top + 4;
        Canvas.SetLeft(SizeBadge, badgeLeft);
        Canvas.SetTop(SizeBadge, badgeTop);
    }

    private void UpdateShades(DragRect rect, bool hasSelection)
    {
        var width = Math.Max(0, RootCanvas.ActualWidth);
        var height = Math.Max(0, RootCanvas.ActualHeight);

        if (!hasSelection)
        {
            FullShade.Visibility = Visibility.Visible;
            FullShade.Width = width;
            FullShade.Height = height;
            SetVisibility(Visibility.Collapsed, TopShade, BottomShade, LeftShade, RightShade);
            SelectionBorder.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            return;
        }

        FullShade.Visibility = Visibility.Collapsed;
        SetVisibility(Visibility.Visible, TopShade, BottomShade, LeftShade, RightShade);

        var left = Math.Clamp(rect.Left, 0, width);
        var top = Math.Clamp(rect.Top, 0, height);
        var right = Math.Clamp(rect.Right, 0, width);
        var bottom = Math.Clamp(rect.Bottom, 0, height);

        SetRect(TopShade, 0, 0, width, top);
        SetRect(BottomShade, 0, bottom, width, height - bottom);
        SetRect(LeftShade, 0, top, left, bottom - top);
        SetRect(RightShade, right, top, width - right, bottom - top);
        SetRect(SelectionBorder, left, top, right - left, bottom - top);

        SelectionBorder.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
    }

    private void ShowHint()
    {
        _hintTimer?.Stop();
        HintText.Visibility = Visibility.Visible;
        Canvas.SetLeft(HintText, 16);
        Canvas.SetTop(HintText, 16);

        _hintTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _hintTimer.Tick += (_, _) =>
        {
            _hintTimer?.Stop();
            HintText.Visibility = Visibility.Collapsed;
        };
        _hintTimer.Start();
    }

    private static void SetVisibility(Visibility visibility, params FrameworkElement[] elements)
    {
        foreach (var element in elements)
            element.Visibility = visibility;
    }

    private static void SetRect(FrameworkElement element, double left, double top, double width, double height)
    {
        element.Width = Math.Max(0, width);
        element.Height = Math.Max(0, height);
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
    }
}
```

- [ ] **Step 3: Create the selector**

Create `src/ViewportCap.App/Windows/RegionSelector.cs`:
```csharp
using ViewportCap.App.Services;
using ViewportCap.Core;

namespace ViewportCap.App.Windows;

public sealed class RegionSelector
{
    public async Task<CaptureRegion?> SelectAsync(CancellationToken cancellationToken)
    {
        var monitors = DisplayService.GetMonitors();
        var active = DisplayService.GetMonitorForCursor();
        var overlays = new List<RegionOverlayWindow>();
        var completion = new TaskCompletionSource<CaptureRegion?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var registration = cancellationToken.Register(() => completion.TrySetResult(null));

        foreach (var monitor in monitors)
        {
            var overlay = new RegionOverlayWindow(monitor);
            overlay.SelectionCompleted += region => completion.TrySetResult(region);
            overlay.Cancelled += () => completion.TrySetResult(null);
            overlay.Closed += (_, _) => completion.TrySetResult(null);
            overlay.EnableInput(false);
            overlays.Add(overlay);
            overlay.Show();
        }

        var activeOverlay = overlays.FirstOrDefault(overlay =>
            string.Equals(overlay.Monitor.DeviceName, active.DeviceName, StringComparison.OrdinalIgnoreCase));

        activeOverlay?.EnableInput(true);

        var result = await completion.Task;
        foreach (var overlay in overlays)
        {
            try
            {
                overlay.Close();
            }
            catch
            {
                // The overlay may already be closing.
            }
        }

        return result;
    }
}
```

- [ ] **Step 4: Create the countdown XAML**

Create `src/ViewportCap.App/Windows/CountdownWindow.xaml`:
```xml
<Window x:Class="ViewportCap.App.Windows.CountdownWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Starting capture"
        Width="300"
        Height="132"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ShowActivated="False"
        ResizeMode="NoResize"
        WindowStartupLocation="Manual"
        KeyDown="Window_KeyDown">
  <Border Background="#F2111827"
          BorderBrush="#FF334155"
          BorderThickness="1"
          CornerRadius="10"
          Padding="12">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
      </Grid.RowDefinitions>

      <TextBlock x:Name="CountText"
                 Grid.Row="0"
                 FontSize="34"
                 FontWeight="Bold"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center" />

      <TextBlock x:Name="InfoText"
                 Grid.Row="1"
                 Foreground="{StaticResource MutedBrush}"
                 FontSize="12"
                 HorizontalAlignment="Center"
                 Margin="0,0,0,8" />

      <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Center">
        <Button Content="Start now" Click="StartNow_Click" />
        <Button Content="Cancel" Click="Cancel_Click" />
        <Button x:Name="AudioButton" Click="Audio_Click" />
      </StackPanel>
    </Grid>
  </Border>
</Window>
```

- [ ] **Step 5: Create the countdown code-behind**

Create `src/ViewportCap.App/Windows/CountdownWindow.xaml.cs`:
```csharp
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ViewportCap.App.Services;
using ViewportCap.Core;

namespace ViewportCap.App.Windows;

public partial class CountdownWindow : Window
{
    private readonly CaptureRegion _region;
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherTimer _timer;
    private readonly Action<AudioMode>? _onAudioChanged;
    private int _remaining;
    private bool _completed;

    public AudioMode AudioMode { get; private set; }

    public CountdownWindow(
        CaptureRegion region,
        int seconds,
        AudioMode audioMode,
        Action<AudioMode>? onAudioChanged = null)
    {
        InitializeComponent();

        _region = region;
        _remaining = Math.Max(1, seconds);
        AudioMode = audioMode;
        _onAudioChanged = onAudioChanged;

        var (left, top) = DisplayService.GetNearbyPosition(region, Width, Height);
        Left = left;
        Top = top;

        SourceInitialized += (_, _) => WindowAffinity.ExcludeFromCapture(this);
        Closed += (_, _) =>
        {
            if (_completed)
                return;

            _completed = true;
            _timer.Stop();
            _completion.TrySetResult(false);
        };
        UpdateText();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            _remaining--;
            if (_remaining <= 0)
                Complete(true);
            else
                UpdateText();
        };
        _timer.Start();
    }

    public Task<bool> RunAsync()
    {
        Show();
        Activate();
        Focus();
        return _completion.Task;
    }

    private void UpdateText()
    {
        CountText.Text = _remaining.ToString();
        InfoText.Text = $"{_region.Width} x {_region.Height}  ·  {AudioMode}";
        AudioButton.Content = $"Audio: {AudioMode}";
    }

    private void Complete(bool start)
    {
        if (_completed)
            return;

        _completed = true;
        _timer.Stop();
        _completion.TrySetResult(start);
        Close();
    }

    private void StartNow_Click(object sender, RoutedEventArgs e) => Complete(true);

    private void Cancel_Click(object sender, RoutedEventArgs e) => Complete(false);

    private void Audio_Click(object sender, RoutedEventArgs e)
    {
        AudioMode = AudioMode switch
        {
            AudioMode.Off => AudioMode.Microphone,
            AudioMode.Microphone => AudioMode.System,
            AudioMode.System => AudioMode.Both,
            _ => AudioMode.Off
        };
        UpdateText();
        _onAudioChanged?.Invoke(AudioMode);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Complete(false);
            e.Handled = true;
        }
    }
}
```

- [ ] **Step 6: Build**

Run:
```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
```

Expected: `Build succeeded` with `0 Error(s)`.

- [ ] **Step 7: Commit**

Run:
```powershell
git add .
git commit -m "feat: add region overlay and countdown"
```

---

### Task 10: Recording HUD and capture flow

**Files:**
- Create: `src/ViewportCap.App/Windows/RecordingHudWindow.xaml`
- Create: `src/ViewportCap.App/Windows/RecordingHudWindow.xaml.cs`
- Create: `src/ViewportCap.App/Services/CaptureFlowController.cs`
- Modify: `src/ViewportCap.App/App.xaml.cs`

- [ ] **Step 1: Create the HUD XAML**

Create `src/ViewportCap.App/Windows/RecordingHudWindow.xaml`:
```xml
<Window x:Class="ViewportCap.App.Windows.RecordingHudWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Recording"
        Width="300"
        Height="112"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ShowActivated="False"
        ResizeMode="NoResize"
        WindowStartupLocation="Manual">
  <Border Background="#F2111827"
          BorderBrush="#FF334155"
          BorderThickness="1"
          CornerRadius="10"
          Padding="12">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
      </Grid.RowDefinitions>

      <StackPanel Grid.Row="0" Orientation="Horizontal" VerticalAlignment="Center">
        <Ellipse Width="10" Height="10" Fill="#FFEF4444" Margin="0,0,8,0" />
        <TextBlock x:Name="ElapsedText" FontSize="18" FontWeight="SemiBold" />
        <TextBlock x:Name="AudioText"
                   Foreground="{StaticResource MutedBrush}"
                   FontSize="12"
                   Margin="12,0,0,0"
                   VerticalAlignment="Center" />
      </StackPanel>

      <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,8,0,0">
        <Button Content="Stop" Click="Stop_Click" />
        <Button Content="Cancel" Click="Cancel_Click" />
      </StackPanel>

      <TextBlock x:Name="WarningText"
                 Grid.Row="2"
                 Foreground="#FFFBBF24"
                 FontSize="12"
                 TextWrapping="Wrap"
                 Visibility="Collapsed"
                 Margin="0,8,0,0" />
    </Grid>
  </Border>
</Window>
```

- [ ] **Step 2: Create the HUD code-behind**

Create `src/ViewportCap.App/Windows/RecordingHudWindow.xaml.cs`:
```csharp
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ViewportCap.App.Services;
using ViewportCap.Core;

namespace ViewportCap.App.Windows;

public partial class RecordingHudWindow : Window
{
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;
    private bool _completed;

    public event Action? StopRequested;
    public event Action? CancelRequested;

    public RecordingHudWindow(CaptureRegion region, AudioMode audioMode)
    {
        InitializeComponent();

        var (left, top) = DisplayService.GetNearbyPosition(region, Width, Height);
        Left = left;
        Top = top;
        AudioText.Text = audioMode == AudioMode.Off ? "Silent" : $"Audio: {audioMode}";

        SourceInitialized += (_, _) => WindowAffinity.ExcludeFromCapture(this);
        Closed += (_, _) =>
        {
            if (!_completed)
            {
                _completed = true;
                _timer.Stop();
                CancelRequested?.Invoke();
            }
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => UpdateElapsed();
    }

    public void Start()
    {
        Show();
        _stopwatch.Start();
        _timer.Start();
        UpdateElapsed();
    }

    public void ShowWarning(string message)
    {
        WarningText.Text = message;
        WarningText.Visibility = Visibility.Visible;
    }

    private void UpdateElapsed()
    {
        ElapsedText.Text = _stopwatch.Elapsed.ToString(@"mm\:ss");
    }

    private void Finish(bool stop)
    {
        if (_completed)
            return;

        _completed = true;
        _stopwatch.Stop();
        _timer.Stop();

        if (stop)
            StopRequested?.Invoke();
        else
            CancelRequested?.Invoke();

        Close();
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => Finish(true);

    private void Cancel_Click(object sender, RoutedEventArgs e) => Finish(false);
}
```

- [ ] **Step 3: Create the capture flow controller**

Create `src/ViewportCap.App/Services/CaptureFlowController.cs`:
```csharp
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ViewportCap.App.Windows;
using ViewportCap.Capture;
using ViewportCap.Core;

namespace ViewportCap.App.Services;

public sealed class CaptureFlowController
{
    private readonly Func<AppSettings> _getSettings;
    private readonly ISettingsStore _settingsStore;
    private readonly ICaptureEngine _capture;
    private readonly bool _captureAvailable;
    private readonly RecordingStateMachine _state = new();
    private readonly RegionSelector _selector = new();

    private CancellationTokenSource? _sessionCts;
    private TaskCompletionSource<bool>? _stopCompletion;
    private RecordingHudWindow? _hud;

    public CaptureFlowController(
        Func<AppSettings> getSettings,
        ISettingsStore settingsStore,
        ICaptureEngine capture,
        bool captureAvailable)
    {
        _getSettings = getSettings;
        _settingsStore = settingsStore;
        _capture = capture;
        _captureAvailable = captureAvailable;
        _capture.Failed += (_, args) =>
            _stopCompletion?.TrySetException(new CaptureException(args.Error));
    }

    public bool IsIdle => _state.State == RecordingState.Idle;

    public void HandleHotkey()
    {
        if (_state.State == RecordingState.Recording)
        {
            _stopCompletion?.TrySetResult(true);
            return;
        }

        if (_state.State == RecordingState.Idle)
            StartCapture();
    }

    public async void StartCapture()
    {
        if (_state.State != RecordingState.Idle)
            return;

        if (!_captureAvailable)
        {
            MessageBox.Show(
                "Screen capture is unavailable. Check the VC++ runtime and Media Foundation.",
                "viewport-cap",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _state.TransitionTo(RecordingState.Selecting);
        _sessionCts = new CancellationTokenSource();

        try
        {
            var region = await _selector.SelectAsync(_sessionCts.Token);
            if (region is null)
            {
                GoIdle();
                return;
            }

            _state.TransitionTo(RecordingState.Countdown);
            var settings = _getSettings();
            var audioMode = settings.AudioMode;

            if (settings.CountdownSeconds > 0)
            {
                var countdown = new CountdownWindow(
                    region,
                    settings.CountdownSeconds,
                    audioMode,
                    mode =>
                    {
                        try
                        {
                            settings.AudioMode = mode;
                            _settingsStore.Save(settings);
                        }
                        catch (Exception ex)
                        {
                            FileLog.Error("Could not save the audio mode.", ex);
                        }
                    });

                var start = await countdown.RunAsync();
                audioMode = countdown.AudioMode;
                if (!start)
                {
                    GoIdle();
                    return;
                }
            }

            _state.TransitionTo(RecordingState.Recording);

            Directory.CreateDirectory(AppPaths.RecordingsDirectory);
            var tempPath = Path.Combine(AppPaths.RecordingsDirectory, $"{Guid.NewGuid():N}.mp4");
            var request = new CaptureRequest(
                region,
                tempPath,
                audioMode,
                settings.MicrophoneDeviceName,
                settings.SystemAudioDeviceName);

            CapturePreflight.EnsureDiskSpace(tempPath);
            await _capture.StartAsync(request, _sessionCts.Token);

            _stopCompletion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _hud = new RecordingHudWindow(region, audioMode);
            _hud.StopRequested += () => _stopCompletion?.TrySetResult(true);
            _hud.CancelRequested += () => _stopCompletion?.TrySetResult(false);
            _hud.Start();

            var startedAt = DateTime.UtcNow;
            var longRecordingWarned = false;
            var diskTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            diskTimer.Tick += (_, _) =>
            {
                try
                {
                    var root = Path.GetPathRoot(tempPath);
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        var free = new DriveInfo(root).AvailableFreeSpace;
                        if (free < 250L * 1024 * 1024)
                        {
                            _hud?.ShowWarning("Low disk space. Stopping the recording.");
                            _stopCompletion?.TrySetResult(true);
                            diskTimer.Stop();
                        }
                        else if (free < 500L * 1024 * 1024)
                        {
                            _hud?.ShowWarning("Low disk space.");
                        }
                    }
                }
                catch
                {
                    // Disk checks are best effort.
                }

                if (!longRecordingWarned && DateTime.UtcNow - startedAt > TimeSpan.FromMinutes(10))
                {
                    _hud?.ShowWarning("Recording is longer than 10 minutes.");
                    longRecordingWarned = true;
                }
            };
            diskTimer.Start();

            bool stop;
            try
            {
                stop = await _stopCompletion.Task;
            }
            finally
            {
                diskTimer.Stop();
            }

            _hud?.Close();
            _hud = null;
            _state.TransitionTo(RecordingState.Finalizing);

            if (!stop)
            {
                await _capture.CancelAsync();
                GoIdle();
                return;
            }

            var result = await _capture.StopAsync(CancellationToken.None);
            FileLog.Info($"Recording complete: {result.FilePath} ({result.Duration}).");
            MessageBox.Show(
                $"Recording complete.\n\n{result.FilePath}\n\nReview is added in the next task.",
                "viewport-cap",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            GoIdle();
        }
        catch (OperationCanceledException)
        {
            await ResetAfterFailureAsync(keepFile: false);
        }
        catch (Exception ex)
        {
            FileLog.Error("Capture failed.", ex);
            MessageBox.Show(
                $"Capture failed:\n\n{ex.Message}",
                "viewport-cap",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            await ResetAfterFailureAsync(keepFile: true);
        }
    }

    public void Cancel()
    {
        _sessionCts?.Cancel();
        _stopCompletion?.TrySetResult(false);
    }

    private async Task ResetAfterFailureAsync(bool keepFile)
    {
        try
        {
            await _capture.CancelAsync(deleteFile: !keepFile);
        }
        catch
        {
            // Best effort cleanup.
        }

        GoIdle();
    }

    private void GoIdle()
    {
        _hud?.Close();
        _hud = null;
        _stopCompletion = null;
        _sessionCts?.Dispose();
        _sessionCts = null;

        if (_state.State != RecordingState.Idle && _state.CanTransitionTo(RecordingState.Idle))
            _state.TransitionTo(RecordingState.Idle);
    }
}
```

- [ ] **Step 4: Wire the flow into `App.xaml.cs`**

Modify `src/ViewportCap.App/App.xaml.cs`:

1. Add these fields after `_settingsWindow`:
```csharp
    private ScreenRecorderCaptureEngine? _capture;
    private CaptureFlowController? _flow;
    private bool _captureAvailable;
```

2. Add `using ViewportCap.Capture;` after `using ViewportCap.Core;`.

3. After the hotkey registration block and before `_tray = new TrayService(...)`, insert:
```csharp
        _capture = new ScreenRecorderCaptureEngine();
        try
        {
            CapturePreflight.EnsureNativeLibraryLoads();
            _captureAvailable = true;
        }
        catch (Exception ex)
        {
            _captureAvailable = false;
            FileLog.Error("Screen capture prerequisites are missing.", ex);
            MessageBox.Show(
                "Screen capture is unavailable. Install the Microsoft Visual C++ runtime and the Media Feature Pack if this is a Windows N edition.",
                "viewport-cap",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        _flow = new CaptureFlowController(
            () => _settings!,
            _settingsStore!,
            _capture,
            _captureAvailable);
```

4. Replace `_tray.ExitRequested += () => Shutdown();` with:
```csharp
        _tray.ExitRequested += RequestExit;
```

5. Replace the body of `OnHotkey` with:
```csharp
        _flow?.HandleHotkey();
```

6. Replace the body of `OnCaptureRequested` with:
```csharp
        _flow?.StartCapture();
```

7. Add this method before `OnExit`:
```csharp
    private void RequestExit()
    {
        if (_flow is { IsIdle: false })
        {
            var answer = MessageBox.Show(
                "A capture is in progress. Exit anyway?",
                "viewport-cap",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
                return;

            _flow.Cancel();
        }

        Shutdown();
    }
```

8. Add this guard at the start of `ShowSettings`:
```csharp
        if (_flow is { IsIdle: false })
        {
            _tray?.ShowBalloon("viewport-cap", "A capture is already in progress.");
            return;
        }
```

- [ ] **Step 5: Build and manually verify capture**

Run:
```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
dotnet run --project src/ViewportCap.App/ViewportCap.App.csproj
```

Manual check:
1. Press `Ctrl+Alt+Shift+R`.
2. Confirm one dimmed overlay appears and the cursor is a crosshair.
3. Drag a rectangle larger than 32 x 32.
4. Confirm the countdown window shows `3`, `2`, `1`; press `Start now` on a second attempt to skip the wait.
5. Confirm the recording HUD shows elapsed time and a red dot.
6. Press `Stop`.
7. Confirm a temp MP4 was created under `%LOCALAPPDATA%\ViewportCap\recordings` and a completion dialog shows the path.
8. Play the temp MP4 with the default player and confirm the overlay, countdown, and HUD are absent and the pointer is present.

- [ ] **Step 6: Commit**

Run:
```powershell
git add .
git commit -m "feat: add recording HUD and capture flow"
```

---

### Task 11: Review window, export, and completion toast

**Files:**
- Create: `src/ViewportCap.App/Windows/ToastWindow.xaml`
- Create: `src/ViewportCap.App/Windows/ToastWindow.xaml.cs`
- Create: `src/ViewportCap.App/Services/ToastService.cs`
- Create: `src/ViewportCap.App/Windows/ReviewWindow.xaml`
- Create: `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`
- Modify: `src/ViewportCap.App/Services/CaptureFlowController.cs`
- Modify: `src/ViewportCap.App/App.xaml.cs`

- [ ] **Step 1: Create the toast window**

Create `src/ViewportCap.App/Windows/ToastWindow.xaml`:
```xml
<Window x:Class="ViewportCap.App.Windows.ToastWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Export complete"
        Width="400"
        Height="150"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ShowActivated="False"
        ResizeMode="NoResize"
        WindowStartupLocation="Manual">
  <Border Background="#F2111827"
          BorderBrush="#FF334155"
          BorderThickness="1"
          CornerRadius="10"
          Padding="14">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
      </Grid.RowDefinitions>

      <TextBlock Grid.Row="0" Text="Export complete" FontSize="16" FontWeight="SemiBold" />
      <TextBlock x:Name="PathText"
                 Grid.Row="1"
                 Foreground="{StaticResource MutedBrush}"
                 FontSize="12"
                 TextTrimming="CharacterEllipsis"
                 Margin="0,4,0,10" />

      <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Content="Open file" Click="Open_Click" />
        <Button Content="Reveal" Click="Reveal_Click" />
        <Button Content="Copy path" Click="Copy_Click" />
        <Button Content="Close" Click="Close_Click" />
      </StackPanel>
    </Grid>
  </Border>
</Window>
```

Create `src/ViewportCap.App/Windows/ToastWindow.xaml.cs`:
```csharp
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ViewportCap.App.Services;

namespace ViewportCap.App.Windows;

public partial class ToastWindow : Window
{
    private readonly string _path;
    private readonly DispatcherTimer _timer;

    public ToastWindow(string path)
    {
        InitializeComponent();
        _path = path;
        PathText.Text = path;

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;

        SourceInitialized += (_, _) => WindowAffinity.ExcludeFromCapture(this);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _timer.Tick += (_, _) => Close();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _path,
            UseShellExecute = true
        });
    }

    private void Reveal_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{_path}\"",
            UseShellExecute = true
        });
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_path);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
```

Create `src/ViewportCap.App/Services/ToastService.cs`:
```csharp
using ViewportCap.App.Windows;

namespace ViewportCap.App.Services;

public sealed class ToastService
{
    private ToastWindow? _current;

    public void Show(string path)
    {
        _current?.Close();
        var toast = new ToastWindow(path);
        _current = toast;
        toast.Closed += (_, _) =>
        {
            if (ReferenceEquals(_current, toast))
                _current = null;
        };
        toast.Show();
    }
}
```

- [ ] **Step 2: Create the review XAML**

Create `src/ViewportCap.App/Windows/ReviewWindow.xaml`:
```xml
<Window x:Class="ViewportCap.App.Windows.ReviewWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Review recording"
        Width="820"
        Height="720"
        MinWidth="720"
        MinHeight="620"
        WindowStartupLocation="CenterScreen"
        ResizeMode="CanResize"
        Icon="pack://application:,,,/Assets/app.ico">
  <Grid Margin="16">
    <Grid.RowDefinitions>
      <RowDefinition Height="*" />
      <RowDefinition Height="Auto" />
      <RowDefinition Height="Auto" />
      <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>

    <Border Grid.Row="0" Background="#FF0F172A" BorderBrush="{StaticResource BorderBrush}" BorderThickness="1" CornerRadius="8">
      <Grid>
        <MediaElement x:Name="Preview"
                      LoadedBehavior="Manual"
                      UnloadedBehavior="Manual"
                      ScrubbingEnabled="True"
                      MediaOpened="Preview_MediaOpened"
                      MediaFailed="Preview_MediaFailed" />
        <Border x:Name="PreviewErrorPanel"
                Background="#EE0B1220"
                Visibility="Collapsed">
          <TextBlock x:Name="PreviewErrorText"
                     Text="Preview unavailable. Use the timecodes to trim."
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center"
                     Foreground="{StaticResource MutedBrush}"
                     TextWrapping="Wrap"
                     Margin="24" />
        </Border>
      </Grid>
    </Border>

    <StackPanel x:Name="FormPanel" Grid.Row="1" Margin="0,10,0,0">
      <Grid>
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width="Auto" />
          <ColumnDefinition Width="*" />
          <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Column="0" Text="In" Width="24" VerticalAlignment="Center" />
        <Slider x:Name="StartSlider"
                Grid.Column="1"
                Minimum="0"
                ValueChanged="TrimChanged"
                Margin="8,0" />
        <TextBlock x:Name="StartText" Grid.Column="2" Width="70" VerticalAlignment="Center" />
      </Grid>

      <Grid Margin="0,4,0,0">
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width="Auto" />
          <ColumnDefinition Width="*" />
          <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Column="0" Text="Out" Width="24" VerticalAlignment="Center" />
        <Slider x:Name="EndSlider"
                Grid.Column="1"
                Minimum="0"
                ValueChanged="TrimChanged"
                Margin="8,0" />
        <TextBlock x:Name="EndText" Grid.Column="2" Width="70" VerticalAlignment="Center" />
      </Grid>

      <StackPanel Orientation="Horizontal" Margin="0,6,0,0">
        <Button x:Name="PlayButton" Content="Play" Click="Play_Click" />
        <TextBlock x:Name="PositionText"
                   Foreground="{StaticResource MutedBrush}"
                   VerticalAlignment="Center"
                   Margin="8,0,0,0" />
      </StackPanel>
    </StackPanel>

    <WrapPanel Grid.Row="2" Margin="0,12,0,0">
      <StackPanel Width="150" Margin="0,0,12,8">
        <TextBlock Text="Format" Foreground="{StaticResource MutedBrush}" />
        <ComboBox x:Name="FormatBox" SelectionChanged="FormatBox_SelectionChanged" />
      </StackPanel>
      <StackPanel Width="150" Margin="0,0,12,8">
        <TextBlock Text="Quality" Foreground="{StaticResource MutedBrush}" />
        <ComboBox x:Name="QualityBox" />
      </StackPanel>
      <StackPanel Width="130" Margin="0,0,12,8">
        <TextBlock Text="Frame rate" Foreground="{StaticResource MutedBrush}" />
        <ComboBox x:Name="FpsBox" />
      </StackPanel>
      <StackPanel Width="160" Margin="0,0,12,8">
        <TextBlock Text="Scale" Foreground="{StaticResource MutedBrush}" />
        <ComboBox x:Name="ScaleBox" />
      </StackPanel>
      <CheckBox x:Name="KeepAudioBox"
                Content="Keep audio"
                VerticalAlignment="Bottom"
                Margin="0,22,12,8" />
    </WrapPanel>

    <Grid Grid.Row="3" Margin="0,8,0,0">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
      </Grid.ColumnDefinitions>

      <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
        <TextBlock x:Name="StatusText" Foreground="{StaticResource MutedBrush}" VerticalAlignment="Center" />
        <ProgressBar x:Name="ExportProgress"
                     Width="160"
                     Height="8"
                     Minimum="0"
                     Maximum="100"
                     Margin="12,0,0,0" />
      </StackPanel>

      <StackPanel x:Name="ActionsPanel" Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Right">
        <Button x:Name="DiscardButton" Content="Discard" Click="Discard_Click" />
        <Button x:Name="SaveAsButton" Content="Save As..." Click="SaveAs_Click" />
        <Button x:Name="ExportButton" Content="Export" Click="Export_Click" />
      </StackPanel>
    </Grid>
  </Grid>
</Window>
```

- [ ] **Step 3: Create the review code-behind**

Create `src/ViewportCap.App/Windows/ReviewWindow.xaml.cs`:
```csharp
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using ViewportCap.App.Services;
using ViewportCap.Core;
using ViewportCap.Encoding;

namespace ViewportCap.App.Windows;

public partial class ReviewWindow : Window
{
    private readonly CaptureResult _result;
    private readonly AppSettings _settings;
    private readonly IExporter? _exporter;
    private readonly Action<string> _onExported;
    private readonly DispatcherTimer _previewTimer;

    private TimeSpan _duration;
    private bool _initializing = true;
    private bool _adjusting;
    private bool _playing;
    private bool _exporting;
    private bool _exported;
    private CancellationTokenSource? _exportCts;

    public ReviewWindow(
        CaptureResult result,
        AppSettings settings,
        IExporter? exporter,
        Action<string> onExported)
    {
        InitializeComponent();

        _result = result;
        _settings = settings;
        _exporter = exporter;
        _onExported = onExported;
        _duration = result.Duration > TimeSpan.FromMilliseconds(250)
            ? result.Duration
            : TimeSpan.FromSeconds(1);

        FormatBox.ItemsSource = Enum.GetValues<OutputFormat>();
        FormatBox.SelectedItem = settings.DefaultFormat;
        QualityBox.ItemsSource = Enum.GetValues<QualityPreset>();
        QualityBox.SelectedItem = QualityPreset.Balanced;
        ScaleBox.ItemsSource = Enum.GetValues<ScalePreset>();
        ScaleBox.SelectedItem = settings.DefaultFormat == OutputFormat.Gif
            ? ScalePreset.Fit960
            : ScalePreset.Percent100;

        FpsBox.ItemsSource = settings.DefaultFormat == OutputFormat.Gif
            ? new[] { 10, 15, 20 }
            : new[] { 15, 30 };
        FpsBox.SelectedItem = settings.DefaultFormat == OutputFormat.Gif ? 15 : 30;

        KeepAudioBox.IsChecked = result.HasAudio;
        KeepAudioBox.IsEnabled = result.HasAudio;
        if (!result.HasAudio)
            KeepAudioBox.Content = "Silent recording";

        SetTrimRange();
        UpdateTrimLabels();

        if (_exporter is null)
        {
            FormatBox.ItemsSource = new[] { OutputFormat.Mp4 };
            FormatBox.SelectedItem = OutputFormat.Mp4;
            QualityBox.IsEnabled = false;
            FpsBox.IsEnabled = false;
            ScaleBox.IsEnabled = false;
            StartSlider.IsEnabled = false;
            EndSlider.IsEnabled = false;
            KeepAudioBox.IsEnabled = false;
            KeepAudioBox.IsChecked = result.HasAudio;
            StatusText.Text = "FFmpeg not found. Untrimmed MP4 only.";
        }

        _initializing = false;

        Preview.Source = new Uri(result.FilePath);
        SourceInitialized += (_, _) => WindowAffinity.ExcludeFromCapture(this);
        Closing += Window_Closing;

        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _previewTimer.Tick += (_, _) => UpdatePlaybackPosition();
        _previewTimer.Start();
        Closed += (_, _) => _previewTimer.Stop();
    }

    private void SetTrimRange()
    {
        var seconds = Math.Max(1, _duration.TotalSeconds);
        StartSlider.Maximum = seconds;
        EndSlider.Maximum = seconds;
        StartSlider.Value = 0;
        EndSlider.Value = seconds;
    }

    private void Preview_MediaOpened(object sender, RoutedEventArgs e)
    {
        if (Preview.NaturalDuration.HasTimeSpan && Preview.NaturalDuration.TimeSpan > TimeSpan.Zero)
        {
            _duration = Preview.NaturalDuration.TimeSpan;
            SetTrimRange();
            UpdateTrimLabels();
        }

        Preview.Position = TimeSpan.Zero;
        Preview.Pause();
    }

    private void Preview_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        PreviewErrorPanel.Visibility = Visibility.Visible;
        PlayButton.IsEnabled = false;
        StatusText.Text = "Preview unavailable. Trimming by timecode is still available.";
    }

    private void TrimChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_initializing || _adjusting)
            return;

        _adjusting = true;

        if (sender == StartSlider && StartSlider.Value > EndSlider.Value - 0.25)
            EndSlider.Value = Math.Min(EndSlider.Maximum, StartSlider.Value + 0.25);

        if (sender == EndSlider && EndSlider.Value < StartSlider.Value + 0.25)
            StartSlider.Value = Math.Max(0, EndSlider.Value - 0.25);

        _adjusting = false;
        UpdateTrimLabels();

        if (Preview.Source is not null)
        {
            var start = TimeSpan.FromSeconds(StartSlider.Value);
            if (Preview.Position < start)
                Preview.Position = start;
        }
    }

    private void UpdateTrimLabels()
    {
        StartText.Text = FormatTime(TimeSpan.FromSeconds(StartSlider.Value));
        EndText.Text = FormatTime(TimeSpan.FromSeconds(EndSlider.Value));
    }

    private void Play_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewErrorPanel.Visibility == Visibility.Visible)
            return;

        if (_playing)
        {
            Preview.Pause();
            _playing = false;
            PlayButton.Content = "Play";
            return;
        }

        Preview.Position = TimeSpan.FromSeconds(StartSlider.Value);
        Preview.Play();
        _playing = true;
        PlayButton.Content = "Pause";
    }

    private void UpdatePlaybackPosition()
    {
        if (!_playing || Preview.Source is null)
            return;

        var start = TimeSpan.FromSeconds(StartSlider.Value);
        var end = TimeSpan.FromSeconds(EndSlider.Value);

        if (Preview.Position < start || Preview.Position >= end)
            Preview.Position = start;

        PositionText.Text = $"{FormatTime(Preview.Position)} / {FormatTime(end)}";
    }

    private void FormatBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || FormatBox.SelectedItem is not OutputFormat format)
            return;

        if (format == OutputFormat.Gif)
        {
            FpsBox.ItemsSource = new[] { 10, 15, 20 };
            FpsBox.SelectedItem = 15;
            ScaleBox.SelectedItem = ScalePreset.Fit960;
        }
        else
        {
            FpsBox.ItemsSource = new[] { 15, 30 };
            FpsBox.SelectedItem = 30;
            ScaleBox.SelectedItem = ScalePreset.Percent100;
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        await ExportAsync(null);
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (FormatBox.SelectedItem is not OutputFormat format)
            return;

        var extension = ExportNaming.Extension(format).TrimStart('.');
        var dialog = new SaveFileDialog
        {
            Filter = $"{format} files (*.{extension})|*.{extension}",
            DefaultExt = extension,
            FileName = ExportNaming.BuildBaseName(DateTimeOffset.Now) + "." + extension
        };

        if (dialog.ShowDialog() == true)
            await ExportAsync(dialog.FileName);
    }

    private async Task ExportAsync(string? outputPath)
    {
        if (_exporting)
            return;

        var review = BuildReviewSettings();
        var errors = review.Validate();
        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errors), "viewport-cap");
            return;
        }

        if (review.Format == OutputFormat.Gif &&
            (review.TrimDuration > TimeSpan.FromSeconds(30) || review.SourceWidth > 1280))
        {
            var answer = MessageBox.Show(
                "GIFs with a long duration or a large width can be very large and slow to encode. Continue?",
                "viewport-cap",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes)
                return;
        }

        ExportPlan plan;
        try
        {
            plan = outputPath is null
                ? ExportPlanner.Create(review, DateTimeOffset.Now, File.Exists)
                : ExportPlanner.CreateForPath(review, outputPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "viewport-cap");
            return;
        }

        if (_exporter is null)
        {
            if (!plan.CopySource)
            {
                MessageBox.Show(
                    "FFmpeg is required for this export. Install ffmpeg.exe beside the app.",
                    "viewport-cap");
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(plan.OutputPath)!);
                File.Copy(review.SourcePath, plan.OutputPath, true);
                CompleteExport(plan.OutputPath);
            }
            catch (Exception ex)
            {
                FileLog.Error("MP4 copy failed.", ex);
                MessageBox.Show($"Export failed:\n\n{ex.Message}", "viewport-cap");
            }
            return;
        }

        _exporting = true;
        SetBusy(true);
        _exportCts = new CancellationTokenSource();

        try
        {
            var result = await _exporter.ExportAsync(
                plan,
                new Progress<double>(value => ExportProgress.Value = value),
                _exportCts.Token);

            CompleteExport(result.OutputPath);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Export cancelled.";
        }
        catch (Exception ex)
        {
            FileLog.Error("Export failed.", ex);
            MessageBox.Show($"Export failed:\n\n{ex.Message}", "viewport-cap");
        }
        finally
        {
            _exporting = false;
            _exportCts?.Dispose();
            _exportCts = null;
            SetBusy(false);
        }
    }

    private ReviewSettings BuildReviewSettings()
    {
        var start = TimeSpan.FromSeconds(StartSlider.Value);
        var end = TimeSpan.FromSeconds(EndSlider.Value);
        if (end > _duration)
            end = _duration;
        if (start < TimeSpan.Zero)
            start = TimeSpan.Zero;
        if (start > end - TimeSpan.FromMilliseconds(250))
            start = end - TimeSpan.FromMilliseconds(250);

        return new ReviewSettings
        {
            SourcePath = _result.FilePath,
            OutputDirectory = _settings.OutputFolder,
            Format = FormatBox.SelectedItem is OutputFormat format ? format : OutputFormat.Mp4,
            Quality = QualityBox.SelectedItem is QualityPreset quality ? quality : QualityPreset.Balanced,
            FrameRate = FpsBox.SelectedItem is int fps ? fps : 30,
            Scale = ScaleBox.SelectedItem is ScalePreset scale ? scale : ScalePreset.Percent100,
            TrimStart = start,
            TrimEnd = end,
            HasAudio = _result.HasAudio,
            MuteAudio = !(KeepAudioBox.IsChecked == true),
            SourceWidth = _result.Width,
            SourceHeight = _result.Height,
            SourceDuration = _duration
        };
    }

    private void CompleteExport(string path)
    {
        _exported = true;
        _exporting = false;
        _playing = false;
        TryDeleteSource();
        _onExported(path);
        Close();
    }

    private void Discard_Click(object sender, RoutedEventArgs e)
    {
        if (_exporting)
            return;

        if (MessageBox.Show(
                "Discard this recording?",
                "viewport-cap",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _exported = true;
        TryDeleteSource();
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_exporting)
        {
            var answer = MessageBox.Show(
                "Cancel the export and close?",
                "viewport-cap",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
                _exportCts?.Cancel();

            e.Cancel = true;
            return;
        }

        if (_exported)
            return;

        if (MessageBox.Show(
                "Discard this recording?",
                "viewport-cap",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        TryDeleteSource();
    }

    private void SetBusy(bool busy)
    {
        FormPanel.IsEnabled = !busy;
        ActionsPanel.IsEnabled = !busy;
        Preview.IsEnabled = !busy;
        ExportProgress.Value = 0;
        StatusText.Text = busy ? "Exporting..." : "Ready.";
    }

    private void TryDeleteSource()
    {
        try
        {
            if (File.Exists(_result.FilePath))
                File.Delete(_result.FilePath);
        }
        catch (Exception ex)
        {
            FileLog.Warn($"Could not delete temp recording: {ex.Message}");
        }
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString(@"mm\:ss\.ff");
    }
}
```

- [ ] **Step 4: Wire review and toast into the flow**

Modify `src/ViewportCap.App/Services/CaptureFlowController.cs`:

1. Add `using ViewportCap.Encoding;`.

2. Add fields after `_captureAvailable`:
```csharp
    private readonly IExporter? _exporter;
    private readonly Action<string> _showToast;
```

3. Replace the constructor signature with:
```csharp
    public CaptureFlowController(
        Func<AppSettings> getSettings,
        ISettingsStore settingsStore,
        ICaptureEngine capture,
        IExporter? exporter,
        Action<string> showToast,
        bool captureAvailable)
```

4. Add assignments before `_capture.Failed`:
```csharp
        _exporter = exporter;
        _showToast = showToast;
```

5. Replace this block:
```csharp
            var result = await _capture.StopAsync(CancellationToken.None);
            FileLog.Info($"Recording complete: {result.FilePath} ({result.Duration}).");
            MessageBox.Show(
                $"Recording complete.\n\n{result.FilePath}\n\nReview is added in the next task.",
                "viewport-cap",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            GoIdle();
```
with:
```csharp
            var result = await _capture.StopAsync(CancellationToken.None);
            FileLog.Info($"Recording complete: {result.FilePath} ({result.Duration}).");
            _state.TransitionTo(RecordingState.Reviewing);

            var review = new ReviewWindow(result, settings, _exporter, _showToast);
            review.Closed += (_, _) =>
            {
                if (_state.State == RecordingState.Reviewing)
                    GoIdle();
            };
            review.Show();
```

- [ ] **Step 5: Wire FFmpeg and toast into `App.xaml.cs`**

Modify `src/ViewportCap.App/App.xaml.cs`:

1. Add `using ViewportCap.Encoding;`.

2. Add fields after `_flow`:
```csharp
    private IExporter? _exporter;
    private ToastService? _toast;
```

3. After `_settings = _settingsStore.Load();` and before `_startup`, insert:
```csharp
        _toast = new ToastService();
        _exporter = CreateExporter();
```

4. Replace the `_flow = new CaptureFlowController(...)` call with:
```csharp
        _flow = new CaptureFlowController(
            () => _settings!,
            _settingsStore!,
            _capture,
            _exporter,
            _toast.Show,
            _captureAvailable);
```

5. Add this method before `OnExit`:
```csharp
    private static IExporter? CreateExporter()
    {
        var path = FfmpegLocator.Find();
        if (path is null)
        {
            FileLog.Warn("ffmpeg.exe was not found. GIF/WebM export is disabled.");
            return null;
        }

        try
        {
            var runner = new SystemProcessRunner();
            var capabilities = FfmpegProbe
                .ProbeAsync(path, runner, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            return new FfmpegExporter(path, capabilities, runner);
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not initialize FFmpeg.", ex);
            return null;
        }
    }
```

- [ ] **Step 6: Build and manually verify export**

Run:
```powershell
dotnet build src/ViewportCap.App/ViewportCap.App.csproj -c Debug
dotnet run --project src/ViewportCap.App/ViewportCap.App.csproj
```

FFmpeg is fetched in Task 12. For this check, place a working `ffmpeg.exe` in `tools/ffmpeg/` (the app searches that path when run from the repo root), or defer the WebM/GIF checks until Task 12.

Manual check:
1. Record a 4-second clip using the hotkey.
2. Confirm the review window opens and `Preview` plays the clip.
3. Set `Out` to about 2 seconds and export MP4. Confirm the toast appears and the file plays.
4. Repeat for WebM and GIF. Confirm GIF has no audio and loops.
5. Confirm the temp file is deleted after a successful export.
6. Press `Discard` on a new recording and confirm the temp file is deleted.
7. Temporarily rename `ffmpeg.exe`, start the app, record, and confirm only untrimmed MP4 is offered. Restore the file.

- [ ] **Step 7: Commit**

Run:
```powershell
git add .
git commit -m "feat: add review, export, and completion toast"
```

---

### Task 12: Packaging, cleanup, and documentation

**Files:**
- Create: `tools/fetch-ffmpeg.ps1`
- Create: `tools/publish.ps1`
- Create: `README.md`
- Create: `LICENSE`
- Create: `THIRD-PARTY-NOTICES.md`
- Modify: `src/ViewportCap.App/Services/AppPaths.cs`
- Modify: `src/ViewportCap.App/App.xaml.cs`

- [ ] **Step 1: Add temp-file cleanup**

Modify `src/ViewportCap.App/Services/AppPaths.cs` by adding this method:
```csharp
    public static void CleanupOldRecordings(TimeSpan maxAge)
    {
        if (!Directory.Exists(RecordingsDirectory))
            return;

        var cutoff = DateTime.UtcNow - maxAge;
        foreach (var file in Directory.EnumerateFiles(RecordingsDirectory, "*.mp4"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                    File.Delete(file);
            }
            catch
            {
                // A file still in use is cleaned up on the next start.
            }
        }
    }
```

In `src/ViewportCap.App/App.xaml.cs`, immediately after `AppPaths.EnsureCreated();`, add:
```csharp
        AppPaths.CleanupOldRecordings(TimeSpan.FromDays(7));
```

- [ ] **Step 2: Create the FFmpeg fetch script**

Create `tools/fetch-ffmpeg.ps1`:
```powershell
param(
    [string]$Destination = (Join-Path $PSScriptRoot 'ffmpeg')
)

$ErrorActionPreference = 'Stop'

$url = 'https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-10-25-13-00/ffmpeg-n8.1.3-24-g92611900bb-win64-lgpl-8.1.zip'
$expectedHash = 'ed8b8dfe088ff978d657834cf96fe40d7dcbb011b37ff374005b66d5272448c9'

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$zip = Join-Path $Destination 'ffmpeg.zip'
$extract = Join-Path $Destination 'extract'

if (Test-Path (Join-Path $Destination 'ffmpeg.exe')) {
    Write-Host "ffmpeg.exe already present in $Destination"
    exit 0
}

Invoke-WebRequest -Uri $url -OutFile $zip
$actualHash = (Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) {
    throw "FFmpeg checksum mismatch. Expected $expectedHash, got $actualHash."
}

if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
Expand-Archive -Path $zip -DestinationPath $extract -Force
$binary = Get-ChildItem -Path $extract -Filter 'ffmpeg.exe' -Recurse | Select-Object -First 1
if (-not $binary) { throw 'ffmpeg.exe was not found in the archive.' }

Copy-Item $binary.FullName (Join-Path $Destination 'ffmpeg.exe') -Force
Remove-Item $zip -Force
Remove-Item $extract -Recurse -Force
Write-Host "FFmpeg installed at $(Join-Path $Destination 'ffmpeg.exe')"
```

- [ ] **Step 3: Create the publish script**

Create `tools/publish.ps1`:
```powershell
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) { $version = '0.1.0' }

dotnet test ViewportCap.sln -c $Configuration --filter "Category!=Integration"
powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'fetch-ffmpeg.ps1')

$publishDir = Join-Path $root 'dist\publish'
$zipPath = Join-Path $root "dist\viewport-cap-$version-win-x64.zip"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

dotnet publish src/ViewportCap.App/ViewportCap.App.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -o $publishDir

Copy-Item (Join-Path $PSScriptRoot 'ffmpeg\ffmpeg.exe') (Join-Path $publishDir 'ffmpeg.exe') -Force
Copy-Item (Join-Path $root 'LICENSE') $publishDir -Force
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') $publishDir -Force
Copy-Item (Join-Path $root 'README.md') $publishDir -Force

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force
Write-Host "Published $zipPath"
```

- [ ] **Step 4: Write the README**

Create `README.md`:
```markdown
# viewport-cap

A tray-first Windows app for recording a free screen region and exporting it as MP4, WebM, or GIF.

Press `Ctrl+Alt+Shift+R`, drag a rectangle, wait for the countdown, and stop with the hotkey or the floating HUD. Review the clip, trim it, choose a format, and export.

## Requirements

- Windows 10 version 2004 or newer, or Windows 11.
- x64.
- The Microsoft Visual C++ runtime.
- Media Foundation. Windows N/KN users need the Media Feature Pack.

## Build and run

```powershell
dotnet build ViewportCap.sln -c Release
dotnet run --project src/ViewportCap.App/ViewportCap.App.csproj
```

The app starts in the tray. Open the tray menu and choose **Settings...** to change the hotkey, output folder, countdown, audio devices, or Start with Windows.

## Recordings and settings

- Settings: `%LOCALAPPDATA%\ViewportCap\settings.json`
- Logs: `%LOCALAPPDATA%\ViewportCap\logs`
- Temporary recordings: `%LOCALAPPDATA%\ViewportCap\recordings`
- Default exports: `Videos\ViewportCap`

Temporary recordings are deleted on discard, on successful export, and after seven days at startup.

## FFmpeg

MP4 recording works without FFmpeg. GIF/WebM export and MP4 trimming require `ffmpeg.exe` beside the app.

To fetch the pinned FFmpeg build:

```powershell
powershell -ExecutionPolicy Bypass -File tools/fetch-ffmpeg.ps1
```

The script verifies the SHA-256 of the BtbN LGPL archive before extracting.

## Publish

```powershell
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

The result is `dist/viewport-cap-<version>-win-x64.zip`.

## Limitations

- SDR only; HDR content may look washed out.
- A selection cannot span monitors.
- Pause/resume and annotations are not included.
- Protected/DRM content may record black.
- No installer, auto-update, or code signing in v1.

## License

MIT for the app code. See `LICENSE` and `THIRD-PARTY-NOTICES.md`.
```

- [ ] **Step 5: Write the license and notices**

Create `LICENSE`:
```text
MIT License

Copyright (c) 2026 viewport-cap contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

Create `THIRD-PARTY-NOTICES.md`:
```markdown
# Third-party notices

viewport-cap includes or distributes the following third-party components.

## ScreenRecorderLib

- Project: https://github.com/sskodje/ScreenRecorderLib
- License: MIT
- Copyright: Sverre Skodje and contributors

## FFmpeg

- Project: https://ffmpeg.org/
- Source: https://github.com/BtbN/FFmpeg-Builds
- License: LGPL, as distributed by the BtbN FFmpeg-Builds project.
- viewport-cap invokes `ffmpeg.exe` as a separate process.

## .NET

- Project: https://dotnet.microsoft.com/
- License: MIT
- Copyright: .NET Foundation and contributors.
```

- [ ] **Step 6: Run the full test suite**

Run:
```powershell
dotnet test ViewportCap.sln -c Release --filter "Category!=Integration"
```

Expected: all unit tests pass.

- [ ] **Step 7: Build the distribution**

Run:
```powershell
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

Expected: `dist/viewport-cap-0.1.0-win-x64.zip` exists and contains `viewport-cap.exe`, `ffmpeg.exe`, `LICENSE`, `THIRD-PARTY-NOTICES.md`, and `README.md`.

- [ ] **Step 8: Smoke-test the published build**

Run:
```powershell
Expand-Archive dist/viewport-cap-0.1.0-win-x64.zip -DestinationPath dist/smoke -Force
dist/smoke/viewport-cap.exe
```

Manual check:
1. The tray icon appears.
2. Capture a short clip.
3. Export MP4, WebM, and GIF.
4. Confirm the toast buttons open, reveal, and copy the path.
5. Exit from the tray.

- [ ] **Step 9: Commit**

Run:
```powershell
git add .
git commit -m "chore: add packaging and documentation"
```

---

## Plan self-check

Before handing off, confirm these observable outcomes:

1. `dotnet test ViewportCap.sln -c Release --filter "Category!=Integration"` passes.
2. `dotnet build ViewportCap.sln -c Release` succeeds.
3. The hotkey opens the overlay on the monitor under the cursor.
4. A valid drag starts the configured countdown.
5. Stop opens Review; the temp MP4 is playable.
6. MP4, WebM, and GIF exports are created; GIF is silent.
7. The overlay, countdown, HUD, and review window do not appear in the recording; the pointer does.
8. Cancel at every stage returns to Idle.
9. Missing FFmpeg leaves untrimmed MP4 export available and disables trimming and other formats.
10. `tools/publish.ps1` produces a runnable zip.

## Spec coverage

| Spec area | Implemented by |
|---|---|
| 4.1 Platform | Tasks 1, 6, 7, 12 |
| 4.2 Capture and output | Tasks 4, 5, 6, 9, 10, 11 |
| 4.3 Shell behavior | Tasks 7, 8, 10 |
| 5.1 Startup and tray | Task 8 |
| 5.2 Region selection | Task 9 |
| 5.3 Countdown | Tasks 9, 10 |
| 5.4 Recording | Task 10 |
| 5.5 Review and export | Task 11 |
| 5.6 Settings | Task 8 |
| 6 Architecture and state machine | Tasks 1-11 |
| 7 Capture backend | Tasks 6, 10 |
| 8 Audio | Tasks 6, 8, 10, 11 |
| 9 Export | Tasks 5, 11 |
| 10 Windows integration | Tasks 7, 8, 10 |
| 11 Persistence and logging | Tasks 2, 7, 12 |
| 12 Error handling | Tasks 8, 10, 11 |
| 13 Testing | Tasks 2-6, 12 |
| 14 Packaging | Task 12 |
| 16 Acceptance criteria | Task 12 self-check |

Integration export tests are represented by the FFmpeg fake-runner unit tests plus the manual export checks in Task 11 and Task 12. A real FFmpeg fixture test would require the downloaded binary and is intentionally not part of the fast CI filter.

## Handoff note

The implementation session should read the spec and this plan, then execute Task 1 through Task 12 in order. If a compile error is caused by a library API difference, fix the code against the installed ScreenRecorderLib 7.0.1 types rather than changing the task boundaries. Do not stop after scaffolding: the expected outcome is the working zip from Task 12.

