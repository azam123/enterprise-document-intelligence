using System.Net;
using System.Net.Http.Json;
using EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;

namespace EnterpriseDocumentIntelligence.SearchService.Infrastructure;

public sealed class AzureOpenAiQueryEmbeddingService(
    HttpClient httpClient,
    IConfiguration configuration) : IQueryEmbeddingService
{
    public async Task<float[]> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Query is required.", nameof(text));
        }

        var endpoint = configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint missing.");

        var deployment = configuration["AzureOpenAI:EmbeddingDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment missing.");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2024-10-21");

        var apiKey = configuration["AzureOpenAI:ApiKey"];

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add("api-key", apiKey);
        }

        request.Content = JsonContent.Create(new { input = text });

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
            (int)response.StatusCode >= 500)
        {
            throw new HttpRequestException(
                $"Azure OpenAI returned {(int)response.StatusCode}.");
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
            cancellationToken);

        return payload?.Data?.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("Embedding response was empty.");
    }

    private sealed record EmbeddingResponse(
        IReadOnlyList<EmbeddingItem>? Data);

    private sealed record EmbeddingItem(
        float[] Embedding);
}