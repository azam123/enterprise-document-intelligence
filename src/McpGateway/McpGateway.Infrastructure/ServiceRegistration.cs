using EnterpriseDocumentIntelligence.McpGateway.Application;
using EnterpriseDocumentIntelligence.McpGateway.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.McpGateway.Infrastructure;

public sealed class DefaultMcpToolRegistry : IMcpToolRegistry
{
    private static readonly IReadOnlyCollection<McpToolDefinition> Tools =
    [
        new(
            "document.search",
            "Search enterprise documents",
            new Dictionary<string, string>
            {
                ["query"] = "string",
                ["topK"] = "integer"
            }),

        new(
            "document.get",
            "Get a document",
            new Dictionary<string, string>
            {
                ["documentId"] = "guid"
            }),

        new(
            "document.audit",
            "Get document audit events",
            new Dictionary<string, string>
            {
                ["documentId"] = "guid"
            })
    ];

    public IReadOnlyCollection<McpToolDefinition> GetTools()
    {
        return Tools;
    }

    public bool IsAllowed(string name)
    {
        return Tools.Any(
            tool => string.Equals(
                tool.Name,
                name,
                StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class DefaultMcpInvocationHandler : IMcpInvocationHandler
{
    public Task<object?> ExecuteAsync(
        McpInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object?>(
            new
            {
                invocation.ToolName,
                invocation.Arguments
            });
    }
}

public static class McpGatewayInfrastructure
{
    public static IServiceCollection AddMcpApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IMcpToolRegistry, DefaultMcpToolRegistry>();
        services.AddSingleton<IMcpInvocationHandler, DefaultMcpInvocationHandler>();
        services.AddScoped<McpApplication>();

        return services;
    }
}
