using System.Drawing;using System.Windows.Forms;
namespace ViewportCap.App.Services;
public sealed class TrayService:IDisposable
{
 private readonly NotifyIcon _icon;private readonly ToolStripMenuItem _startupItem;
 public event Action? CaptureRequested;public event Action? SettingsRequested;public event Action? OpenRecordingsRequested;public event Action? ExitRequested;public event Action<bool>? StartWithWindowsChanged;
 public TrayService(string hotkeyText,bool startWithWindows){var menu=new ContextMenuStrip();var cap=new ToolStripMenuItem("Capture now");cap.Click+=(_,_)=>CaptureRequested?.Invoke();var settings=new ToolStripMenuItem("Settings...");settings.Click+=(_,_)=>SettingsRequested?.Invoke();var folder=new ToolStripMenuItem("Open recordings folder");folder.Click+=(_,_)=>OpenRecordingsRequested?.Invoke();_startupItem=new ToolStripMenuItem("Start with Windows"){Checked=startWithWindows,CheckOnClick=true};_startupItem.Click+=(_,_)=>StartWithWindowsChanged?.Invoke(_startupItem.Checked);var exit=new ToolStripMenuItem("Exit");exit.Click+=(_,_)=>ExitRequested?.Invoke();menu.Items.Add(cap);menu.Items.Add(settings);menu.Items.Add(folder);menu.Items.Add(new ToolStripSeparator());menu.Items.Add(_startupItem);menu.Items.Add(new ToolStripSeparator());menu.Items.Add(exit);_icon=new NotifyIcon{Icon=LoadIcon(),Text=$"viewport-cap ({hotkeyText})",ContextMenuStrip=menu,Visible=true};_icon.MouseClick+=(_,e)=>{if(e.Button==MouseButtons.Left)CaptureRequested?.Invoke();};}
 public void UpdateStartWithWindows(bool enabled)=>_startupItem.Checked=enabled;public void UpdateHotkey(string text)=>_icon.Text=$"viewport-cap ({text})";
 public void ShowBalloon(string title,string text){_icon.BalloonTipTitle=title;_icon.BalloonTipText=text;_icon.BalloonTipIcon=ToolTipIcon.Info;_icon.ShowBalloonTip(5000);}
 public void Dispose(){_icon.Visible=false;_icon.Dispose();}
 private static Icon LoadIcon(){var path=Environment.ProcessPath;if(!string.IsNullOrWhiteSpace(path)){var icon=Icon.ExtractAssociatedIcon(path);if(icon!=null)return icon;}return SystemIcons.Application;}
}
