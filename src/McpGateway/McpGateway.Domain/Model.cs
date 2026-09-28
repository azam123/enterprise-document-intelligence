namespace EnterpriseDocumentIntelligence.McpGateway.Domain;

public sealed record McpToolDefinition(
    string Name,
    string Description,
    IReadOnlyDictionary<string, string> Arguments);

public sealed record McpInvocation(
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments);
