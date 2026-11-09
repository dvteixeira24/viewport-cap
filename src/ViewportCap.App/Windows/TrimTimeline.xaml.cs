using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ViewportCap.Core;

namespace ViewportCap.App.Windows;

// Single-track start/end selector. The track shows cut regions, Kept shows the
// Highlight-filled kept range. Keyboard entry lives in the ReviewWindow text boxes.
public partial class TrimTimeline : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty StartProperty =
        DependencyProperty.Register("Start", typeof(double), typeof(TrimTimeline), new PropertyMetadata(0.0, OnLayout));
    public static readonly DependencyProperty EndProperty =
        DependencyProperty.Register("End", typeof(double), typeof(TrimTimeline), new PropertyMetadata(1.0, OnLayout));
    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register("Duration", typeof(double), typeof(TrimTimeline), new PropertyMetadata(1.0, OnLayout));
    public static readonly DependencyProperty PlayheadProperty =
        DependencyProperty.Register("Playhead", typeof(double), typeof(TrimTimeline), new PropertyMetadata(0.0, OnLayout));

    public double Start { get => (double)GetValue(StartProperty); set => SetValue(StartProperty, value); }
    public double End { get => (double)GetValue(EndProperty); set => SetValue(EndProperty, value); }
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public double Playhead { get => (double)GetValue(PlayheadProperty); set => SetValue(PlayheadProperty, value); }

    public event EventHandler? Changed;

    private bool _moving;
    private double _grabX, _grabStart, _grabEnd;

    public TrimTimeline()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Layout();
        StartThumb.DragDelta += (_, e) =>
        {
            var w = Math.Max(1, Track.ActualWidth);
            Start = TrimMath.ClampStart(Start + e.HorizontalChange / w * Duration, End, Duration);
            Changed?.Invoke(this, EventArgs.Empty);
            Layout();
        };
        EndThumb.DragDelta += (_, e) =>
        {
            var w = Math.Max(1, Track.ActualWidth);
            End = TrimMath.ClampEnd(Start, End + e.HorizontalChange / w * Duration, Duration);
            Changed?.Invoke(this, EventArgs.Empty);
            Layout();
        };
    }

    private static void OnLayout(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TrimTimeline)d).Layout();

    private void Layout()
    {
        if (Track.ActualWidth <= 0 || Duration <= 0) return;
        var w = Track.ActualWidth;
        var a = Math.Clamp(Start / Duration, 0, 1) * w;
        var b = Math.Clamp(End / Duration, 0, 1) * w;
        var p = Math.Clamp(Playhead / Duration, 0, 1) * w;
        Kept.Margin = new Thickness(a, 0, Math.Max(0, w - b), 0);
        StartThumb.Margin = new Thickness(a - 8, 0, 0, 0);
        EndThumb.Margin = new Thickness(b - 8, 0, 0, 0);
        PlayheadLine.Margin = new Thickness(p - 1, 0, 0, 0);
    }

    // Drag the kept middle to move the whole range.
    public static bool ShouldIgnoreTrackPress(object? source)
    {
        var d = source as DependencyObject;
        while (d != null)
        {
            if (d is Thumb) return true;
            d = VisualTreeHelper.GetParent(d);
        }
        return false;
    }
    private void Track_Press(object sender, MouseButtonEventArgs e)
    {
        if (ShouldIgnoreTrackPress(e.OriginalSource)) return;
        _moving = true;
        _grabX = e.GetPosition(Track).X;
        _grabStart = Start;
        _grabEnd = End;
        Track.CaptureMouse();
    }

    private void Track_Move(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_moving || e.LeftButton != MouseButtonState.Pressed) return;
        var w = Math.Max(1, Track.ActualWidth);
        var shift = (e.GetPosition(Track).X - _grabX) / w * Duration;
        var len = _grabEnd - _grabStart;
        var s = Math.Clamp(_grabStart + shift, 0, Math.Max(0, Duration - len));
        Start = s;
        End = Math.Min(Duration, s + len);
        Changed?.Invoke(this, EventArgs.Empty);
        Layout();
    }

    private void Track_Release(object sender, MouseButtonEventArgs e)
    {
        _moving = false;
        Track.ReleaseMouseCapture();
    }
}
