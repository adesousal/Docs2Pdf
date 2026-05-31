using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Docs2Pdf.Api.Models;

namespace Docs2Pdf.Api.Services;

public sealed class FileValidationService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".doc",
        ".xls",
        ".ppt",
        ".docx",
        ".xlsx",
        ".pptx",
        ".odt",
        ".ods",
        ".odp",
        ".png",
        ".jpg",
        ".jpeg",
        ".pdf"
    };

    private static readonly Dictionary<string, string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Textos / Documentos
        ["text/plain"] = ".txt",
        ["application/pdf"] = ".pdf",
        
        // Microsoft Office (Formatos Novos XML)
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx",
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = ".pptx",
        
        // Microsoft Office (Formatos Antigos/Binários)
        ["application/msword"] = ".doc",
        ["application/vnd.ms-excel"] = ".xls",
        ["application/vnd.ms-powerpoint"] = ".ppt",
        
        // --- NOVO: LibreOffice / OpenDocument ---
        ["application/vnd.oasis.opendocument.text"] = ".odt",
        ["application/vnd.oasis.opendocument.spreadsheet"] = ".ods",
        ["application/vnd.oasis.opendocument.presentation"] = ".odp",
        // ----------------------------------------

        // Imagens
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/pjpeg"] = ".jpeg" 
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public bool TryValidate(IFormFile file, out UploadMetadata metadata, out string error)
    {
        metadata = new UploadMetadata();
        error = string.Empty;

        if (file.Length <= 0)
        {
            error = "O arquivo está vazio.";
            return false;
        }

        // if (file.Length > MaxFileSizeBytes)
        // {
        //     error = "O arquivo excede o limite de 10MB.";
        //     return false;
        // }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            error = "Extensão de arquivo não permitida.";
            return false;
        }

        // If the browser provided a known MIME type, prefer to validate it against expected mapping.
        // However, many browsers or proxies may send generic MIME types; in that case accept based on extension only.
        if (AllowedMimeTypes.TryGetValue(file.ContentType, out var expectedExtension))
        {
            if (!string.Equals(expectedExtension, extension, StringComparison.OrdinalIgnoreCase) && !IsImageVariant(extension, expectedExtension))
            {
                error = "Tipo de arquivo e extensão não correspondem.";
                return false;
            }
        }

        metadata = new UploadMetadata
        {
            OriginalName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            Extension = extension.ToLowerInvariant(),
            SizeBytes = file.Length
        };

        return true;
    }

    private static bool IsImageVariant(string extension, string expectedExtension)
    {
        if (expectedExtension == ".jpg" && extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (expectedExtension == ".jpeg" && extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
