using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenRecorderLib;
using ViewportCap.App.Services;
using ViewportCap.App.Windows;
using ViewportCap.Capture;
using ViewportCap.Core;
using Xunit;

namespace ViewportCap.App.Tests;

public sealed class NativeRecorderIntegrationTests
{
    [Theory]
    [Trait("Category", "Integration")]
    [InlineData(false,640,480,1000)]
    [InlineData(false,1016,562,7000)]
    [InlineData(true,1016,562,7000)]
    public Task SyntheticContent_RecordsAndStopsWithCaptureDefaults(bool useCaptureEngine,int width,int height,int durationMillis) => StaTest.Run(async () =>
    {
        var directory=Path.Combine(Path.GetTempPath(),"opencode","viewport-cap-native-tests",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var output=Path.Combine(directory,"recording.mp4");
        var region=new CaptureRegion("synthetic image",0,0,0,0,width,height,96);
        if(useCaptureEngine)
        {
            var monitor=DisplayService.GetMonitors().First(m=>m.IsPrimary);
            region=new CaptureRegion(monitor.DeviceName,monitor.Left,monitor.Top,monitor.Left+100,monitor.Top+100,width,height,monitor.Dpi);
            var window=new Window{Title="viewport-cap recorder test",Width=width,Height=height,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Background=Brushes.SteelBlue,Topmost=true,ShowInTaskbar=false};
            window.Show();
            DisplayService.SetPhysicalBounds(window,region.X,region.Y,region.Width,region.Height);
            try
            {
                await Task.Delay(200);
                var engine=new ScreenRecorderCaptureEngine();
                await engine.StartAsync(new CaptureRequest(region,output,AudioMode.Off,null,null),CancellationToken.None);
                await Task.Delay(durationMillis);
                var result=await engine.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(8));
                Assert.True(new FileInfo(output).Length>1024);
                var review=new ReviewWindow(result,new AppSettings{OutputFolder=directory},null,_=>{});
                try
                {
                    var player=StaTest.Field<MediaPlayer>(review,"_player");
                    var opened=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    player.MediaOpened+=(_,_)=>opened.TrySetResult();
                    player.MediaFailed+=(_,e)=>opened.TrySetException(e.ErrorException);
                    review.Show();
                    await opened.Task.WaitAsync(TimeSpan.FromSeconds(8));
                    Assert.True(review.IsVisible);
                    Assert.True(player.NaturalDuration.HasTimeSpan);
                }
                finally { await review.CloseForShutdownAsync(); }
            }
            finally { window.Close(); }
            return;
        }

        // Generated pixels reproduce the encoder failure without recording the desktop.
        var image=Path.Combine(directory,"source.png");
        var pixels=new byte[width*height*4];
        for(var i=0;i<pixels.Length;i+=4){pixels[i]=200;pixels[i+3]=255;}
        var png=new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,pixels,width*4)));
        using(var stream=File.Create(image))png.Save(stream);
        var options=ScreenRecorderOptionsFactory.Create(new CaptureRequest(region,output,AudioMode.Off,null,null));
        options.SourceOptions.RecordingSources=new List<RecordingSourceBase>{new ImageRecordingSource(image)};
        options.LogOptions=new LogOptions{IsLogEnabled=true,LogFilePath=Path.Combine(directory,"native.log"),LogSeverityLevel=LogLevel.Debug};
        var complete=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder=Recorder.CreateRecorder(options);
        recorder.OnRecordingComplete+=(_,_)=>complete.TrySetResult();
        recorder.OnRecordingFailed+=(_,e)=>{var error=new Exception(e.Error);complete.TrySetException(error);started.TrySetException(error);};
        recorder.OnStatusChanged+=(_,e)=>{if(e.Status==RecorderStatus.Recording)started.TrySetResult();};
        recorder.Record(output);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(8));
            await Task.Delay(durationMillis);
            recorder.Stop();
            await complete.Task.WaitAsync(TimeSpan.FromSeconds(8));
            Assert.True(new FileInfo(output).Length>1024,$"Empty recording; diagnostics: {directory}");
        }
        catch(Exception ex)
        {
            throw new Exception($"Native recorder status={recorder.Status}, frames={recorder.CurrentFrameNumber}, diagnostics={directory}",ex);
        }
        finally
        {
            // Disposing a native recorder still waiting on its encoder would hang the test runner too.
            if(complete.Task.IsCompleted)recorder.Dispose();
        }
    });
}
