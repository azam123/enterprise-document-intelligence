using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.SearchService;

public sealed class AzureOpenAiEmbeddingClient(HttpClient http, IConfiguration configuration)
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/') ?? throw new InvalidOperationException("AzureOpenAI:Endpoint missing.");
        var deployment = configuration["AzureOpenAI:EmbeddingDeployment"] ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment missing.");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2024-10-21");
        var key = configuration["AzureOpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(key)) request.Headers.Add("api-key", key);
        request.Content = new StringContent(JsonSerializer.Serialize(new { input = text }), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return json.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray().Select(x => x.GetSingle()).ToArray();
    }
}