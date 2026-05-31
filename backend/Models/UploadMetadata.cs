namespace Docs2Pdf.Api.Models;

public sealed class UploadMetadata
{
    public string OriginalName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
}
