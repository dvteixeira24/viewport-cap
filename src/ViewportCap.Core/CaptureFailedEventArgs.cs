namespace ViewportCap.Core;
public sealed class CaptureFailedEventArgs:EventArgs{public CaptureFailedEventArgs(string error)=>Error=error;public string Error{get;}}
