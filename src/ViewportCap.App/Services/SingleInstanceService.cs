namespace ViewportCap.App.Services;
public sealed class SingleInstanceService:IDisposable
{
 private const string MutexName=@"Local\ViewportCap.SingleInstance";private const string EventName=@"Local\ViewportCap.Activate";private Mutex? _mutex;private EventWaitHandle? _event;private RegisteredWaitHandle? _wait;
 public bool TryAcquire(){_mutex=new Mutex(true,MutexName,out var created);if(!created){_mutex.Dispose();_mutex=null;SignalExistingInstance();return false;}_event=new EventWaitHandle(false,EventResetMode.AutoReset,EventName);return true;}
 public void Listen(Action onActivate){if(_event!=null)_wait=ThreadPool.RegisterWaitForSingleObject(_event,(_,_)=>onActivate(),null,Timeout.Infinite,false);}
 public void Dispose(){_wait?.Unregister(null);_event?.Dispose();_mutex?.ReleaseMutex();_mutex?.Dispose();}
 private static void SignalExistingInstance(){try{using var e=EventWaitHandle.OpenExisting(EventName);e.Set();}catch{}}
}
