using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using System.Text;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.McpGateway;

public interface IMcpToolExecutor { Task<object> ExecuteAsync(string tool, JsonElement arguments, CancellationToken ct); }

public sealed class McpToolExecutor(IHttpClientFactory clients, IHttpContextAccessor context, ICurrentUser user) : IMcpToolExecutor
{
    public async Task<object> ExecuteAsync(string tool, JsonElement arguments, CancellationToken ct)
    {
        var configuration = context.HttpContext?.RequestServices.GetRequiredService<IConfiguration>()
            ?? throw new InvalidOperationException("Configuration unavailable.");

        string serviceKey, path;
        HttpMethod method;
        switch (tool)
        {
            case "document.search":
                serviceKey = "SearchServiceUrl"; path = "api/v1/search"; method = HttpMethod.Post; break;
            case "document.get":
                serviceKey = "DocumentServiceUrl"; path = $"api/v1/documents/{RequiredGuid(arguments, "documentId")}"; method = HttpMethod.Get; break;
            case "document.audit":
                serviceKey = "AuditServiceUrl"; path = BuildAuditPath(arguments); method = HttpMethod.Get; break;
            default:
                throw new KeyNotFoundException($"Tool '{tool}' is not registered.");
        }

        var client = clients.CreateClient(tool);
        client.BaseAddress ??= new Uri(configuration[$"Services:{serviceKey}"] ?? throw new InvalidOperationException($"Services:{serviceKey} missing."));
        using var request = new HttpRequestMessage(method, path);
        var authorization = context.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)) request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Headers.Add("X-Tenant-Id", user.TenantId.ToString());
        if (method == HttpMethod.Post) request.Content = new StringContent(arguments.GetRawText(), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Tool '{tool}' failed with {(int)response.StatusCode}.");
        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    private static string BuildAuditPath(JsonElement arguments)
    {
        if (!arguments.TryGetProperty("from", out var value) || value.ValueKind != JsonValueKind.String) return "api/v1/audit";
        return $"api/v1/audit?from={Uri.EscapeDataString(value.GetString()!)}";
    }

    private static Guid RequiredGuid(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var value) || !Guid.TryParse(value.GetString(), out var id))
            throw new ArgumentException($"{name} must be a valid GUID.");
        return id;
    }
}