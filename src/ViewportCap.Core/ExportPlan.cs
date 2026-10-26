namespace ViewportCap.Core;

public sealed record ExportPlan(ReviewSettings Settings, string OutputPath, bool CopySource);
