using Microsoft.AspNetCore.Http;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Diagnostics;
using ClosedXML.Excel;

namespace Docs2Pdf.Api.Services;

public sealed class PdfConversionService
{
    private readonly TempFileService _tempFileService;

    public PdfConversionService(TempFileService tempFileService)
    {
        _tempFileService = tempFileService;
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
        var outputPath = Path.ChangeExtension(inputPath, ".pdf");

            var exe = FindLibreOfficeExecutable();
            if (exe == null)
            {
                throw new InvalidOperationException("LibreOffice não foi encontrado no PATH nem nos caminhos padrão do sistema.");
            }

        try
        {
            await using var fileStream = File.Create(inputPath);
            await file.CopyToAsync(fileStream);
            fileStream.Close();

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension is ".xlsx" or ".xls")
            {
                await PrepareSpreadsheetAsync(inputPath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--headless --convert-to pdf --outdir \"{Path.GetDirectoryName(outputPath)}\" \"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                    throw new InvalidOperationException($"Não foi possível iniciar o LibreOffice a partir de '{exe}'.");
            }

            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                var error = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"Falha na conversão do LibreOffice (executável: {exe}): {error}");
            }

            return await File.ReadAllBytesAsync(outputPath);
        }
        finally
        {
            _tempFileService.DeleteQuietly(inputPath);
            _tempFileService.DeleteQuietly(outputPath);
        }
    }

    private static string? FindLibreOfficeExecutable()
    {
        // Common candidates: explicit full paths first, then common executable names
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LibreOffice", "program", "soffice.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "LibreOffice", "program", "soffice.exe"),
            "soffice",
            "libreoffice"
        };

        foreach (var c in candidates)
        {
            try
            {
                if (c.IndexOf(Path.DirectorySeparatorChar) >= 0)
                {
                    if (File.Exists(c)) return c;
                }
                else
                {
                    // Try starting with the name to see if it's resolvable in PATH
                    var psi = new ProcessStartInfo { FileName = c, UseShellExecute = false };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.Kill();
                        return c;
                    }
                }
            }
            catch
            {
                // ignore and try next
            }
        }

        return null;
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

    private async Task PrepareSpreadsheetAsync(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);

        foreach (var ws in workbook.Worksheets)
        {
            var usedRange = ws.RangeUsed();

            if (usedRange == null)
                continue;

            var columnCount = usedRange.ColumnCount();
            var rowCount = usedRange.RowCount();

            var pageSetup = ws.PageSetup;

            // Heurística simples:
            // muitas colunas => paisagem
            pageSetup.PageOrientation =
                columnCount > 8
                    ? XLPageOrientation.Landscape
                    : XLPageOrientation.Portrait;

            // Ajusta para caber na largura
            pageSetup.FitToPages(1, 0);

            // Centraliza
            pageSetup.CenterHorizontally = true;

            // Opcional:
            pageSetup.Margins.Left = 0.3;
            pageSetup.Margins.Right = 0.3;
            pageSetup.Margins.Top = 0.5;
            pageSetup.Margins.Bottom = 0.5;
        }

        workbook.Save();
    }
}
