namespace EnterpriseDocumentIntelligence.DocumentService.Domain;

public readonly record struct DocumentName
{
    public string Value { get; }

    public DocumentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Document name is required.", nameof(value));

        var normalized = Path.GetFileName(value.Trim());
        if (string.IsNullOrWhiteSpace(normalized) || normalized != value.Trim())
            throw new ArgumentException("Document name must be a file name.", nameof(value));

        if (normalized.Length > 255)
            throw new ArgumentException("Document name cannot exceed 255 characters.", nameof(value));

        Value = normalized;
    }

    public override string ToString() => Value;
}
