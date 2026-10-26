namespace ViewportCap.Encoding;
public sealed record FfmpegCapabilities(bool HasH264Mf,bool HasOpenH264,bool HasVp9,bool HasOpus)
{
 public bool CanEncodeH264=>HasH264Mf||HasOpenH264;
 public static FfmpegCapabilities Parse(string output){var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);using var r=new StringReader(output);string? line;while((line=r.ReadLine())!=null){var t=line.Trim();if(t.Length==0||(t[0]!='V'&&t[0]!='A'))continue;var p=t.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(p.Length>=2)names.Add(p[1]);}return new(names.Contains("h264_mf"),names.Contains("libopenh264"),names.Contains("libvpx-vp9"),names.Contains("libopus"));}
}
