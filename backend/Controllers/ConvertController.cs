using Docs2Pdf.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Docs2Pdf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConvertController : ControllerBase
{
    private readonly FileValidationService _validationService;
    private readonly PdfConversionService _conversionService;
    private readonly ILogger<ConvertController> _logger;

    public ConvertController(FileValidationService validationService, PdfConversionService conversionService, ILogger<ConvertController> logger)
    {
        _validationService = validationService;
        _conversionService = conversionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromQuery] bool combine = false)
    {
        if (!Request.Form.Files.Any())
        {
            _logger.LogWarning("Request with no files received from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            return BadRequest(new { error = "Nenhum arquivo enviado." });
        }

        var files = Request.Form.Files;
        var validatedFiles = new List<IFormFile>();

        foreach (var formFile in files)
        {
            if (!_validationService.TryValidate(formFile, out _, out var validationError))
            {
                _logger.LogWarning("Validation failed for file {FileName} (size={Size}) from {RemoteIp}: {Error}", formFile.FileName, formFile.Length, HttpContext.Connection.RemoteIpAddress, validationError);
                return BadRequest(new { error = validationError });
            }
            validatedFiles.Add(formFile);
        }

        if (!combine && validatedFiles.Count > 1)
        {
            _logger.LogWarning("Client attempted multiple files with combine=false from {RemoteIp}. Count={Count}", HttpContext.Connection.RemoteIpAddress, validatedFiles.Count);
            return BadRequest(new { error = "Envie apenas um arquivo por solicitação ou use combine=true." });
        }

        try
        {
            if (combine)
            {
                var pdfList = new List<byte[]>();
                foreach (var file in validatedFiles)
                {
                    pdfList.Add(await _conversionService.ConvertAsync(file));
                }

                var mergedPdf = _conversionService.MergePdfs(pdfList);
                return File(mergedPdf, "application/pdf", "pdfscombinados.pdf");
            }

            var targetFile = validatedFiles[0];
            var pdfData = await _conversionService.ConvertAsync(targetFile);
            var resultName = Path.GetFileNameWithoutExtension(targetFile.FileName) + ".pdf";
            return File(pdfData, "application/pdf", resultName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion error for request from {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });
}
