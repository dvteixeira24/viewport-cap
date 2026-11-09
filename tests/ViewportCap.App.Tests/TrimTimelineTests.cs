using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ViewportCap.App.Windows;
using Xunit;

namespace ViewportCap.App.Tests;

public sealed class TrimTimelineTests
{
    [Fact]
    public Task ThumbPress_IsNotMistakenForMiddleDrag() => StaTest.Run(() =>
    {
        var timeline = new TrimTimeline { Duration = 10, Start = 2, End = 8 };
        var window = new Window { Content = timeline, Width = 400, Height = 100 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var thumb = (Thumb)timeline.FindName("StartThumb");
            Assert.True(thumb.RenderSize.Width > 0);
            var leaf = DeepestChild(thumb) ?? (DependencyObject)thumb;
            // A press landing on the handle (or anything it renders) must count as a thumb press.
            Assert.True(TrimTimeline.ShouldIgnoreTrackPress(leaf));
            // A press on the kept fill must still arm middle-drag.
            var kept = (DependencyObject)timeline.FindName("Kept");
            Assert.False(TrimTimeline.ShouldIgnoreTrackPress(kept));
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });

    [Fact]
    public Task ThumbDragDelta_MovesStartEdge() => StaTest.Run(() =>
    {
        var timeline = new TrimTimeline { Duration = 10, Start = 2, End = 8 };
        var window = new Window { Content = timeline, Width = 400, Height = 100 };
        try
        {
            window.Show();
            window.UpdateLayout();
            var thumb = (Thumb)timeline.FindName("StartThumb");
            thumb.RaiseEvent(new DragDeltaEventArgs(20, 0));
            Assert.True(timeline.Start > 2);
            Assert.Equal(8, timeline.End);
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });

    private static DependencyObject? DeepestChild(DependencyObject root)
    {
        var current = root;
        while (VisualTreeHelper.GetChildrenCount(current) > 0)
            current = VisualTreeHelper.GetChild(current, 0);
        return current == root ? null : current;
    }
}
