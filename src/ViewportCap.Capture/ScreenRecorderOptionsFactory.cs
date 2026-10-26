using ScreenRecorderLib;
using ViewportCap.Core;
namespace ViewportCap.Capture;
public static class ScreenRecorderOptionsFactory
{
 public const int CaptureFrameRate=30;
 public static RecorderOptions Create(CaptureRequest request)
 {
  var audioSources=new List<AudioSourceBase>();
  if(request.AudioMode is AudioMode.Microphone or AudioMode.Both)audioSources.Add(string.IsNullOrWhiteSpace(request.MicrophoneDeviceName)?CaptureAudioSource.Default:new CaptureAudioSource(request.MicrophoneDeviceName));
  if(request.AudioMode is AudioMode.System or AudioMode.Both)audioSources.Add(string.IsNullOrWhiteSpace(request.SystemAudioDeviceName)?LoopbackAudioSource.Default:new LoopbackAudioSource(request.SystemAudioDeviceName));
  var source=new DisplayRecordingSource{DeviceName=request.Region.MonitorDeviceName,RecorderApi=RecorderApi.WindowsGraphicsCapture,IsCursorCaptureEnabled=true,IsBorderRequired=false};
  var options=new RecorderOptions
  {
   SourceOptions=new SourceOptions{RecordingSources=new List<RecordingSourceBase>{source}},
   // Do not set OutputFrameSize: with one source it becomes the whole canvas and the display is scaled into it, while SourceRect is ignored.
   OutputOptions=new OutputOptions{RecorderMode=RecorderMode.Video,SourceRect=new ScreenRect(request.Region.SourceX,request.Region.SourceY,request.Region.Width,request.Region.Height),Stretch=StretchMode.None},
   VideoEncoderOptions=new VideoEncoderOptions{Encoder=new H264VideoEncoder{BitrateMode=H264BitrateControlMode.CBR,EncoderProfile=H264Profile.High},Framerate=CaptureFrameRate,IsFixedFramerate=true,Bitrate=CaptureBitrateCalculator.ForRegion(request.Region.Width,request.Region.Height,CaptureFrameRate),IsHardwareEncodingEnabled=true,IsThrottlingDisabled=false,IsLowLatencyEnabled=false,IsFragmentedMp4Enabled=true,IsMp4FastStartEnabled=true},
   AudioOptions=new AudioOptions{IsAudioEnabled=audioSources.Count>0,Bitrate=AudioBitrate.bitrate_128kbps,Channels=AudioChannels.Stereo},
   MouseOptions=new MouseOptions{IsMousePointerEnabled=true,IsMouseClicksDetected=false},LogOptions=new LogOptions{IsLogEnabled=false}
  };
  foreach(var audioSource in audioSources)options.AudioOptions.AudioSources.Add(audioSource);return options;
 }
}
