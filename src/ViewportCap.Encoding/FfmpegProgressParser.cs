using System.Globalization;
namespace ViewportCap.Encoding;
public sealed class FfmpegProgressParser
{
 private readonly double _durationSeconds;public FfmpegProgressParser(double durationSeconds)=>_durationSeconds=durationSeconds;public double? Percent{get;private set;}
 public void Feed(string line){if(line.StartsWith("progress=end",StringComparison.Ordinal)){Percent=100;return;}var prefix=line.StartsWith("out_time_us=",StringComparison.Ordinal)?"out_time_us=":line.StartsWith("out_time_ms=",StringComparison.Ordinal)?"out_time_ms=":null;if(prefix is null||_durationSeconds<=0)return;if(!long.TryParse(line[prefix.Length..],NumberStyles.Integer,CultureInfo.InvariantCulture,out var microseconds))return;Percent=Math.Clamp(microseconds/1_000_000.0/_durationSeconds*100.0,0,100);}
}
