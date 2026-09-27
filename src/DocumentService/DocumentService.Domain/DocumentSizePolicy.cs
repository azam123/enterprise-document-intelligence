namespace EnterpriseDocumentIntelligence.DocumentService.Domain;

public static class DocumentSizePolicy
{
    public const long MaximumBytes = 500L * 1024 * 1024;

    public static void EnsureValid(long sizeBytes)
    {
        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Document size must be greater than zero.");

        if (sizeBytes > MaximumBytes)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), $"Document size cannot exceed {MaximumBytes} bytes.");
    }
}
