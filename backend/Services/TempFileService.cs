namespace Docs2Pdf.Api.Services;

public sealed class TempFileService
{
    private readonly string _tempDirectory;

    public TempFileService()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "docs2pdf");
        Directory.CreateDirectory(_tempDirectory);
    }

    public string CreatePath(string extension)
    {
        var fileName = $"{Guid.NewGuid()}{extension}";
        return Path.Combine(_tempDirectory, fileName);
    }

    public void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignore cleanup failures
        }
    }
}
