namespace ViewportCap.Encoding;

public static class ExportFiles
{
    public static string PartialPath(string output) =>
        Path.Combine(Path.GetDirectoryName(output) ?? "", $"{Path.GetFileNameWithoutExtension(output)}.partial{Path.GetExtension(output)}");

    public static void CopyIntoPlace(string source, string output)
    {
        var partial = PartialPath(output);
        try
        {
            File.Copy(source, partial, true);
            File.Move(partial, output, true);
        }
        catch
        {
            try { if (File.Exists(partial)) File.Delete(partial); } catch { }
            throw;
        }
    }
}
