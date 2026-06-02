using Microsoft.AspNetCore.Http;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace Docs2Pdf.Api.Services;

/// <summary>
/// Serviço de conversão usando Gotenberg, um micro-serviço de conversão de documentos
/// muito mais rápido e confiável que LibreOffice.
/// Gotenberg roda em um container Docker de forma persistente.
/// </summary>
public sealed class GotenbergConversionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GotenbergConversionService> _logger;
    private readonly TempFileService _tempFileService;
    private readonly string _gotenbergUrl;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000; // Aumentado para produção com HTTPS

    public GotenbergConversionService(
        HttpClient httpClient,
        ILogger<GotenbergConversionService> logger,
        TempFileService tempFileService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tempFileService = tempFileService;
        
        // Estratégia de fallback: tenta URL pública primeiro, depois localhost
        _gotenbergUrl = configuration["GotenbergUrl"] ?? DetectGotenbergUrl();
        
        // Timeout maior para HTTPS e produção
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    private string DetectGotenbergUrl()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        return env switch
        {
            "Production" => "https://gotenberg.onrender.com",
            "Staging" => "https://gotenberg-staging.onrender.com",
            _ => "http://localhost:3000"
        };
    }

    public async Task<byte[]> ConvertAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension is ".png" or ".jpg" or ".jpeg")
        {
            return await ConvertImageToPdfAsync(file);
        }

        return await ConvertOfficeDocumentAsync(file);
    }

    private async Task<byte[]> ConvertOfficeDocumentAsync(IFormFile file)
    {
        var inputPath = _tempFileService.CreatePath(Path.GetExtension(file.FileName));
        
        try
        {
            await using var fileStream = File.Create(inputPath);
            await file.CopyToAsync(fileStream);
            fileStream.Close();

            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(inputPath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            using var content = new MultipartFormDataContent();
            content.Add(fileContent, "files", file.FileName);

            var endpoint = DetermineEndpoint(file.FileName);
            var retryCount = 0;

            while (retryCount < MaxRetries)
            {
                try
                {
                    var url = $"{_gotenbergUrl}{endpoint}";
                    _logger.LogInformation($"Enviando arquivo {file.FileName} para Gotenberg em {url} (tentativa {retryCount + 1}/{MaxRetries})");
                    var response = await _httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Conversão bem-sucedida: {file.FileName}");
                        return await response.Content.ReadAsByteArrayAsync();
                    }

                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Erro na conversão (status {response.StatusCode}): {error}");

                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        retryCount++;
                        if (retryCount < MaxRetries)
                        {
                            _logger.LogWarning($"Gotenberg indisponível, tentativa {retryCount} de {MaxRetries}");
                            await Task.Delay(RetryDelayMs * retryCount);
                            continue;
                        }
                    }

                    throw new InvalidOperationException(
                        $"Falha na conversão com Gotenberg: {response.StatusCode} - {error}");
                }
                catch (HttpRequestException ex) when (retryCount < MaxRetries - 1)
                {
                    retryCount++;
                    _logger.LogWarning($"Erro de conexão com Gotenberg, tentativa {retryCount} de {MaxRetries}: {ex.Message}");
                    await Task.Delay(RetryDelayMs * retryCount);
                }
                catch (TaskCanceledException ex)
                {
                    retryCount++;
                    _logger.LogWarning($"Timeout na conexão com Gotenberg, tentativa {retryCount} de {MaxRetries}: {ex.Message}");
                    if (retryCount < MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs * retryCount);
                    }
                }
            }

            throw new InvalidOperationException(
                $"Falha na conversão com Gotenberg após {MaxRetries} tentativas.");
        }
        finally
        {
            _tempFileService.DeleteQuietly(inputPath);
        }
    }

    private string DetermineEndpoint(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".docx" or ".doc" or ".odt" or ".rtf" => "/forms/libreoffice/convert",
            ".xlsx" or ".xls" or ".ods" => "/forms/libreoffice/convert",
            ".pptx" or ".ppt" or ".odp" => "/forms/libreoffice/convert",
            ".html" or ".htm" => "/forms/chromium/convert/html",
            _ => "/forms/libreoffice/convert"
        };
    }

    private async Task<byte[]> ConvertImageToPdfAsync(IFormFile file)
    {
        using var pdf = new PdfDocument();
        var page = pdf.AddPage();

        using var image = XImage.FromStream(() => file.OpenReadStream());

        var imageWidth = image.PointWidth;
        var imageHeight = image.PointHeight;

        // Detecta orientação automaticamente
        page.Orientation = imageWidth > imageHeight
            ? PdfSharpCore.PageOrientation.Landscape
            : PdfSharpCore.PageOrientation.Portrait;

        page.Width = imageWidth;
        page.Height = imageHeight;

        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(image, 0, 0, imageWidth, imageHeight);

        using var ms = new MemoryStream();
        pdf.Save(ms);

        return ms.ToArray();
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

    /// <summary>
    /// Verifica a saúde do serviço Gotenberg
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_gotenbergUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }    
}
