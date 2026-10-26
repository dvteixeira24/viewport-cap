using ViewportCap.Core;
using Xunit;
namespace ViewportCap.Core.Tests;
public sealed class ExportNamingTests
{
    [Theory] [InlineData(OutputFormat.Mp4,".mp4")] [InlineData(OutputFormat.WebM,".webm")] [InlineData(OutputFormat.Gif,".gif")]
    public void Extension_ReturnsExpectedValue(OutputFormat f,string e)=>Assert.Equal(e,ExportNaming.Extension(f));
    [Fact] public void BuildBaseName_UsesSortableTimestamp()=>Assert.Equal("viewport-cap_2026-10-26_14-32-05",ExportNaming.BuildBaseName(new DateTimeOffset(2026,10,26,14,32,5,TimeSpan.Zero)));
    [Fact] public void EnsureUnique_AppendsCounter(){var existing=new HashSet<string>(StringComparer.OrdinalIgnoreCase){@"C:\out\viewport-cap_2026-10-26_14-32-05.mp4",@"C:\out\viewport-cap_2026-10-26_14-32-05-2.mp4"}; Assert.Equal(@"C:\out\viewport-cap_2026-10-26_14-32-05-3.mp4",ExportNaming.EnsureUnique(@"C:\out","viewport-cap_2026-10-26_14-32-05",".mp4",existing.Contains));}
}
