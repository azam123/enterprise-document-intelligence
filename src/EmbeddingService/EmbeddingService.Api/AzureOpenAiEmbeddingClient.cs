using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.EmbeddingService;

public sealed class AzureOpenAiEmbeddingClient(HttpClient http, IConfiguration configuration, ILogger<AzureOpenAiEmbeddingClient> logger)
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));
        var endpoint = configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/') ?? throw new InvalidOperationException("AzureOpenAI:Endpoint missing.");
        var deployment = configuration["AzureOpenAI:EmbeddingDeployment"] ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment missing.");
        var key = configuration["AzureOpenAI:ApiKey"];

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2024-10-21");
            if (!string.IsNullOrWhiteSpace(key)) request.Headers.Add("api-key", key);
            request.Content = new StringContent(JsonSerializer.Serialize(new { input = text }), Encoding.UTF8, "application/json");

            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                using var json = JsonDocument.Parse(body);
                return json.RootElement.GetProperty("data")[0].GetProperty("embedding")
                    .EnumerateArray().Select(x => x.GetSingle()).ToArray();
            }

            if (attempt == 3 || ((int)response.StatusCode < 429 && (int)response.StatusCode < 500))
                response.EnsureSuccessStatusCode();

            var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 250);
            logger.LogWarning("Embedding request failed attempt={Attempt} Status={Status}", attempt, response.StatusCode);
            await Task.Delay(delay, ct);
        }

        throw new InvalidOperationException("Embedding request failed.");
    }
}