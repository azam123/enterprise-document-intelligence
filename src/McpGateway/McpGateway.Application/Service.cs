using EnterpriseDocumentIntelligence.McpGateway.Domain;

namespace EnterpriseDocumentIntelligence.McpGateway.Application;

public interface IMcpToolRegistry
{
    IReadOnlyCollection<McpToolDefinition> GetTools();

    bool IsAllowed(string name);
}

public interface IMcpInvocationHandler
{
    Task<object?> ExecuteAsync(
        McpInvocation invocation,
        CancellationToken cancellationToken = default);
}

public sealed class McpApplication(
    IMcpToolRegistry registry,
    IMcpInvocationHandler handler)
{
    public IReadOnlyCollection<McpToolDefinition> GetTools()
    {
        return registry.GetTools();
    }

    public Task<object?> ExecuteAsync(
        McpInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        if (!registry.IsAllowed(invocation.ToolName))
        {
            throw new KeyNotFoundException(
                $"MCP tool '{invocation.ToolName}' is not registered.");
        }

        return handler.ExecuteAsync(invocation, cancellationToken);
    }
}
