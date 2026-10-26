using ViewportCap.App.Windows;
namespace ViewportCap.App.Services;
public sealed class ToastService
{
 private ToastWindow? _current;public void Show(string path){_current?.Close();var toast=new ToastWindow(path);_current=toast;toast.Closed+=(_,_)=>{if(ReferenceEquals(_current,toast))_current=null;};toast.Show();}
}
