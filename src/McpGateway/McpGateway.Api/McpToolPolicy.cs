using System.Text.Json;
public static class McpToolPolicy
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "document.search", "document.get", "document.audit"
    };
    public static IReadOnlyCollection<string> Tools => Allowed;
    public static void EnsureAllowed(string tool)
    {
        if (string.IsNullOrWhiteSpace(tool) || !Allowed.Contains(tool)) throw new KeyNotFoundException($"Unsupported MCP tool '{tool}'.");
    }
    public static int NormalizeTopK(int topK) => Math.Clamp(topK <= 0 ? 8 : topK, 1, 20);
    public static void ValidateArguments(JsonElement arguments)
    {
        if (arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) throw new ArgumentException("Arguments are required.", nameof(arguments));
        if (arguments.GetRawText().Length > 32768) throw new ArgumentException("Arguments exceed the maximum size.", nameof(arguments));
    }
    public static Guid ParseDocumentId(JsonElement arguments)
    {
        if (!arguments.TryGetProperty("documentId", out var id) || !Guid.TryParse(id.GetString(), out var value))
            throw new ArgumentException("documentId must be a GUID.", nameof(arguments));
        return value;
    }
}