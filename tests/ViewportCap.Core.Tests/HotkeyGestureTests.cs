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
    public void TryParse_RejectsInvalidInput(string input) => Assert.False(HotkeyGesture.TryParse(input, out _));
}
