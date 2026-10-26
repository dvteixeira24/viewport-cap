namespace ViewportCap.Encoding;
public interface IProcessRunner{Task<ProcessResult> RunAsync(string fileName,IReadOnlyList<string> arguments,Action<string>? onStandardOutput,Action<string>? onStandardError,CancellationToken cancellationToken);}
