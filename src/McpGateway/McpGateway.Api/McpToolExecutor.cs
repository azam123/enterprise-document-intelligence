using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.McpGateway;

public interface IMcpToolExecutor
{
    Task<object> ExecuteAsync(string tool, JsonElement arguments, CancellationToken ct);
}

public sealed class McpToolExecutor(IHttpClientFactory clients, IHttpContextAccessor context, ICurrentUser user)
    : IMcpToolExecutor
{
    public async Task<object> ExecuteAsync(string tool, JsonElement arguments, CancellationToken ct)
    {
        var (baseUrl, path, method) = tool switch
        {
            "document.search" => ("SearchServiceUrl", "api/v1/search", HttpMethod.Post),
            "document.get" => ("DocumentServiceUrl", "api/v1/documents/" + RequiredGuid(arguments, "documentId"), HttpMethod.Get),
            "document.audit" => ("AuditServiceUrl", "api/v1/audit", HttpMethod.Get),
            _ => throw new KeyNotFoundException($"Tool '{tool}' is not registered.")
        };

        var config = context.HttpContext?.RequestServices.GetRequiredService<IConfiguration>()
            ?? throw new InvalidOperationException("Configuration unavailable.");
        var client = clients.CreateClient(tool);
        client.BaseAddress ??= new Uri(config[$"Services:{baseUrl}"] ?? throw new InvalidOperationException($"Services:{baseUrl} missing."));

        using var request = new HttpRequestMessage(method, path);
        var authorization = context.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.Add("X-Tenant-Id", user.TenantId.ToString());

        if (method == HttpMethod.Post)
            request.Content = new StringContent(arguments.GetRawText(), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Tool '{tool}' failed with {(int)response.StatusCode}.");
        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    private static Guid RequiredGuid(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var value) || !Guid.TryParse(value.GetString(), out var id))
            throw new ArgumentException($"{name} must be a valid GUID.");
        return id;
    }
}