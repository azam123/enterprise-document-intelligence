namespace EnterpriseDocumentIntelligence.DocumentService.Domain;

public static class DocumentTypePolicy
{
    private static readonly IReadOnlyDictionary<string, string[]> Allowed =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = [".pdf"],
            ["text/plain"] = [".txt"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"],
            ["application/msword"] = [".doc"]
        };

    public static bool IsAllowed(string contentType, string fileName)
    {
        if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(fileName))
            return false;

        var extension = Path.GetExtension(fileName);
        return Allowed.TryGetValue(contentType, out var extensions) &&
               extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public static void EnsureAllowed(string contentType, string fileName)
    {
        if (!IsAllowed(contentType, fileName))
            throw new ArgumentException("The content type and file extension are not supported.");
    }
}
