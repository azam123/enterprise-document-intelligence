using Microsoft.AspNetCore.Http;

namespace EnterpriseDocumentIntelligence.DocumentService.Api;

public static class FileSignatureValidator
{
    public static bool IsAllowed(
        IFormFile file,
        string fileName)
    {
        if (file is null || string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);

        if (file.ContentType.Equals(
                "text/plain",
                StringComparison.OrdinalIgnoreCase))
        {
            return extension.Equals(
                ".txt",
                StringComparison.OrdinalIgnoreCase);
        }

        Span<byte> header = stackalloc byte[8];

        using var stream = file.OpenReadStream();
        var read = stream.Read(header);

        if (file.ContentType.Equals(
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return extension.Equals(
                       ".pdf",
                       StringComparison.OrdinalIgnoreCase) &&
                   read >= 4 &&
                   header[..4].SequenceEqual("%PDF"u8);
        }

        if (file.ContentType.Equals(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase))
        {
            return extension.Equals(
                       ".docx",
                       StringComparison.OrdinalIgnoreCase) &&
                   read >= 2 &&
                   header[0] == 0x50 &&
                   header[1] == 0x4B;
        }

        if (file.ContentType.Equals(
                "application/msword",
                StringComparison.OrdinalIgnoreCase))
        {
            return extension.Equals(
                       ".doc",
                       StringComparison.OrdinalIgnoreCase) &&
                   read >= 8 &&
                   header[..8].SequenceEqual(
                       new byte[]
                       {
                           0xD0, 0xCF, 0x11, 0xE0,
                           0xA1, 0xB1, 0x1A, 0xE1
                       });
        }

        return false;
    }
}
