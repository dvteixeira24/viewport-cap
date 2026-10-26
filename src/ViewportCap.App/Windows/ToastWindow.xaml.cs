using System.Diagnostics;using System.Windows;using System.Windows.Threading;using ViewportCap.App.Services;
namespace ViewportCap.App.Windows;
public partial class ToastWindow:Window
{
 private readonly string _path;private readonly DispatcherTimer _timer;
 public ToastWindow(string path){InitializeComponent();_path=path;PathText.Text=path;var a=SystemParameters.WorkArea;Left=a.Right-Width-16;Top=a.Bottom-Height-16;SourceInitialized+=(_,_)=>WindowAffinity.ExcludeFromCapture(this);_timer=new(){Interval=TimeSpan.FromSeconds(8)};_timer.Tick+=(_,_)=>Close();_timer.Start();Closed+=(_,_)=>_timer.Stop();}
  private void Open_Click(object s,RoutedEventArgs e)=>Process.Start(new ProcessStartInfo{FileName=_path,UseShellExecute=true});private void Reveal_Click(object s,RoutedEventArgs e)=>Process.Start(new ProcessStartInfo{FileName="explorer.exe",Arguments=$"/select,\"{_path}\"",UseShellExecute=true});private void Copy_Click(object s,RoutedEventArgs e){try{System.Windows.Clipboard.SetText(_path);}catch(Exception ex){FileLog.Warn($"Could not copy path: {ex.Message}");}}private void Close_Click(object s,RoutedEventArgs e)=>Close();
}
