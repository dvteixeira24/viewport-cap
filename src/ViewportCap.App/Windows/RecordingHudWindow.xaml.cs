using System.Diagnostics;using System.Windows;using System.Windows.Threading;using ViewportCap.App.Services;using ViewportCap.Core;
namespace ViewportCap.App.Windows;
public partial class RecordingHudWindow:Window
{
 private readonly Stopwatch _watch=new();private readonly DispatcherTimer _timer;private bool _completed;public event Action? StopRequested;public event Action? CancelRequested;
 public RecordingHudWindow(CaptureRegion region,AudioMode mode){InitializeComponent();var p=DisplayService.GetNearbyPosition(region,Width,Height);Left=p.Left;Top=p.Top;AudioText.Text=mode==AudioMode.Off?"Silent":$"Audio: {mode}";SourceInitialized+=(_,_)=>WindowAffinity.ExcludeFromCapture(this);Closed+=(_,_)=>{if(!_completed){_completed=true;_timer.Stop();CancelRequested?.Invoke();}};_timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};_timer.Tick+=(_,_)=>ElapsedText.Text=_watch.Elapsed.ToString(@"mm\:ss");}
 public void Start(){Show();_watch.Start();_timer.Start();}public void ShowWarning(string message){WarningText.Text=message;WarningText.Visibility=Visibility.Visible;}
 private void Finish(bool stop){if(_completed)return;_completed=true;_watch.Stop();_timer.Stop();if(stop)StopRequested?.Invoke();else CancelRequested?.Invoke();Close();}
 private void Stop_Click(object s,RoutedEventArgs e)=>Finish(true);private void Cancel_Click(object s,RoutedEventArgs e)=>Finish(false);
}
