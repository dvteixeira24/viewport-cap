namespace ViewportCap.Encoding;

public static class ExportFiles
{
    public static string PartialPath(string output) =>
        Path.Combine(Path.GetDirectoryName(output) ?? "", $"{Path.GetFileNameWithoutExtension(output)}.{Guid.NewGuid():N}.partial{Path.GetExtension(output)}");

    public static Task CopyIntoPlaceAsync(string source, string output, CancellationToken cancellationToken) => Task.Run(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(output) ?? throw new ArgumentException("The output path has no directory.", nameof(output)));
        var partial = PartialPath(output);
        var created = false;
        try
        {
            await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var destination = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                created = true;
                await input.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partial, output, true);
        }
        catch
        {
            try { if (created) File.Delete(partial); } catch { }
            throw;
        }
    }, cancellationToken);
}
