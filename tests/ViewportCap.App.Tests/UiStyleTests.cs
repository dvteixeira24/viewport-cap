using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml.Linq;
using ViewportCap.App.Windows;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.App.Tests;

// WPF theme initialization must not race the other STA window tests.
[CollectionDefinition("UI styles", DisableParallelization = true)]
public sealed class UiStyleCollection;

[Collection("UI styles")]
public sealed class UiStyleTests
{
    [Fact]
    public Task WindowTheme_PreservesDerivedWindowStyleAndDropdownKeyboard() => StaTest.Run(async () =>
    {
        var ns = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        var app = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Ui", "App.xaml"));
        var dictionary = new XElement(ns + "ResourceDictionary",
            new XAttribute(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"),
            app.Root!.Element(ns + "Application.Resources")!.Elements());
        var resources = (ResourceDictionary)XamlReader.Parse(dictionary.ToString());
        var combo = new ComboBox { ItemsSource = new[] { "Default device", "USB microphone" }, SelectedIndex = 0 };
        var window = new Window { Resources = resources, Content = combo, Width = 320, Height = 120, ShowInTaskbar = false };
        try
        {
            window.Show();
            window.UpdateLayout();
            Assert.NotNull(combo.Template.FindName("PART_Popup", combo));
            var key = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(combo), 0, Key.F4)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };
            combo.RaiseEvent(key);
            Assert.True(combo.IsDropDownOpen);
            var popup = (Popup)combo.Template.FindName("PART_Popup", combo);
            Assert.True(popup.IsOpen);
            combo.SelectedIndex = 1;
            combo.IsDropDownOpen = false;
            Assert.Equal("USB microphone", combo.SelectionBoxItem);
            combo.IsEnabled = false;
            Assert.Equal(0.5, combo.Opacity);
        }
        finally
        {
            window.Close();
        }
        var recording = new CaptureResult(Path.Combine(Path.GetTempPath(), "viewport-cap-missing.mp4"), false, TimeSpan.FromSeconds(5), 640, 480);
        var review = new ReviewWindow(recording, new AppSettings(), null, _ => { }) { Resources = resources };
        try
        {
            Assert.Same(resources[typeof(Window)], review.Style);
            Assert.Equal(14, review.FontSize);
        }
        finally
        {
            await review.CloseForShutdownAsync();
        }
    });
}
