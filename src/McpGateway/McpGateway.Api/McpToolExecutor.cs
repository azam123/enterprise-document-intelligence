using System.Text;
using System.Text.Json;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;

public interface IMcpToolExecutor
{
    Task<object> ExecuteAsync(
        string tool,
        JsonElement arguments,
        CancellationToken cancellationToken);
}

public sealed class McpToolExecutor(
    IHttpClientFactory clients,
    IHttpContextAccessor context,
    ICurrentUser user,
    IConfiguration configuration) : IMcpToolExecutor
{
    public Task<object> ExecuteAsync(
        string tool,
        JsonElement arguments,
        CancellationToken cancellationToken) =>
        tool switch
        {
            "document.search" =>
                SearchAsync(arguments, cancellationToken),

            "document.get" =>
                GetAsync(arguments, cancellationToken),

            "document.audit" =>
                AuditAsync(arguments, cancellationToken),

            _ => throw new InvalidOperationException(
                $"Unsupported MCP tool '{tool}'.")
        };

    private async Task<object> SearchAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var query =
            arguments.TryGetProperty("query", out var queryProperty)
                ? queryProperty.GetString()
                : string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("query is required.");
        }

        var topK =
            arguments.TryGetProperty("topK", out var topKProperty)
                ? Math.Clamp(topKProperty.GetInt32(), 1, 20)
                : 8;

        return await SendAsync(
            "SearchService",
            "/api/v1/search",
            HttpMethod.Post,
            new
            {
                query,
                topK
            },
            cancellationToken);
    }

    private async Task<object> GetAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty(
                "documentId",
                out var idProperty) ||
            !Guid.TryParse(
                idProperty.GetString(),
                out var documentId))
        {
            throw new ArgumentException(
                "documentId must be a GUID.");
        }

        return await SendAsync(
            "DocumentService",
            $"/api/v1/documents/{documentId}",
            HttpMethod.Get,
            null,
            cancellationToken);
    }

    private Task<object> AuditAsync(
        JsonElement arguments,
        CancellationToken cancellationToken) =>
        SendAsync(
            "AuditService",
            "/api/v1/audit",
            HttpMethod.Get,
            null,
            cancellationToken);

    private async Task<object> SendAsync(
        string service,
        string path,
        HttpMethod method,
        object? body,
        CancellationToken cancellationToken)
    {
        var baseUrl =
            configuration[$"Services:{service}Url"]?.TrimEnd('/')
            ?? throw new InvalidOperationException(
                $"Services:{service}Url missing.");

        using var request = new HttpRequestMessage(
            method,
            baseUrl + path);

        var authorization =
            context.HttpContext?.Request.Headers.Authorization
                .ToString();

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                authorization);
        }

        request.Headers.TryAddWithoutValidation(
            "X-Tenant-Id",
            user.TenantId.ToString());

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");
        }

        using var response = await clients
            .CreateClient(service)
            .SendAsync(request, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<JsonElement>(content);
    }
}