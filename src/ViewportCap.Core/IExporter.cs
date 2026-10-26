namespace ViewportCap.Core;

public sealed record ExportResult(string OutputPath);
public interface IExporter
{
    Task<ExportResult> ExportAsync(ExportPlan plan, IProgress<double>? progress, CancellationToken cancellationToken);
}
