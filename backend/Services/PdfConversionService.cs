using Microsoft.AspNetCore.Http;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace Docs2Pdf.Api.Services;

/// <summary>
/// Serviço de conversão de PDF que usa Gotenberg como backend.
/// Gotenberg é um micro-serviço de conversão de documentos muito mais rápido
/// e confiável que chamar LibreOffice para cada arquivo.
/// </summary>
public sealed class PdfConversionService
{
    private readonly GotenbergConversionService _gotenbergService;
    private readonly ILogger<PdfConversionService> _logger;

    public PdfConversionService(GotenbergConversionService gotenbergService, ILogger<PdfConversionService> logger)
    {
        _gotenbergService = gotenbergService;
        _logger = logger;
    }

    public async Task<byte[]> ConvertAsync(IFormFile file)
    {
        return await _gotenbergService.ConvertAsync(file);
    }

    private async Task<byte[]> ConvertImageToPdfAsync(IFormFile file)
    {
        return await _gotenbergService.ConvertAsync(file);
    }

    public byte[] MergePdfs(IEnumerable<byte[]> pdfFiles)
    {
        using var outputDocument = new PdfDocument();

        foreach (var bytes in pdfFiles)
        {
            using var inputStream = new MemoryStream(bytes);
            using var inputDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

            for (var index = 0; index < inputDocument.PageCount; index++)
            {
                outputDocument.AddPage(inputDocument.Pages[index]);
            }
        }

        using var mergedStream = new MemoryStream();
        outputDocument.Save(mergedStream);
        return mergedStream.ToArray();
    }

}
