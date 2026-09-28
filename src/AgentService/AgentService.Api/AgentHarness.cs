using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

public sealed record AgentResult(
    string Answer,
    IReadOnlyList<string> Citations,
    int ToolCalls,
    int Tokens);

public sealed class AgentHarness(
    HttpClient http,
    IHttpContextAccessor context,
    IConfiguration configuration,
    ILogger<AgentHarness> log,
    AgentRequestPolicy policy)
{
    public async Task<AgentResult> RunAsync(
        string prompt,
        int maxTools,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        prompt = policy.ValidatePrompt(prompt);
        (maxTools, maxTokens) = policy.Normalize(maxTools, maxTokens);

        var messages = new List<object>
        {
            new
            {
                role = "system",
                content =
                    policy.BuildSystemPrompt()
            },
            new
            {
                role = "user",
                content = prompt
            }
        };

        var citations =
            new HashSet<string>(StringComparer.Ordinal);

        var toolCalls = 0;
        var totalTokens = 0;

        while (toolCalls < maxTools)
        {
            using var response = await ChatAsync(
                messages,
                maxTokens,
                cancellationToken);

            using var json = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(
                    cancellationToken));

            totalTokens += GetTokenUsage(json);

            var message = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");

            if (!message.TryGetProperty(
                    "tool_calls",
                    out var calls) ||
                calls.GetArrayLength() == 0)
            {
                return new AgentResult(
                    message.GetProperty("content").GetString()
                        ?? "No answer.",
                    citations.ToArray(),
                    toolCalls,
                    totalTokens);
            }

            var assistant =
                JsonSerializer.Deserialize<JsonElement>(
                    message.GetRawText());

            messages.Add(assistant);

            foreach (var call in calls.EnumerateArray())
            {
                if (toolCalls >= maxTools)
                {
                    break;
                }

                var name = call
                    .GetProperty("function")
                    .GetProperty("name")
                    .GetString();

                if (!string.Equals(
                        name,
                        "document_search",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var arguments =
                    JsonSerializer.Deserialize<SearchArguments>(
                        call.GetProperty("function")
                            .GetProperty("arguments")
                            .GetString() ?? "{}")
                    ?? new SearchArguments();

                var result = await SearchAsync(
                    arguments,
                    cancellationToken);

                foreach (var citation in result.Citations)
                {
                    citations.Add(citation);
                }

                toolCalls++;

                messages.Add(
                    new
                    {
                        role = "tool",
                        tool_call_id = call.GetProperty("id")
                            .GetString(),
                        content = JsonSerializer.Serialize(result)
                    });
            }
        }

        log.LogWarning(
            "Agent reached tool-call limit of {MaxTools}.",
            maxTools);

        return new AgentResult(
            "The agent reached the configured tool-call limit " +
            "before producing a final answer.",
            citations.ToArray(),
            toolCalls,
            totalTokens);
    }

    private async Task<HttpResponseMessage> ChatAsync(
        List<object> messages,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var endpoint =
            configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/')
            ?? throw new InvalidOperationException(
                "AzureOpenAI:Endpoint missing.");

        var deployment =
            configuration["AzureOpenAI:ChatDeployment"]
            ?? throw new InvalidOperationException(
                "AzureOpenAI:ChatDeployment missing.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint}/openai/deployments/{deployment}" +
            "/chat/completions?api-version=2024-10-21");

        var key = configuration["AzureOpenAI:ApiKey"];

        if (!string.IsNullOrWhiteSpace(key))
        {
            request.Headers.Add("api-key", key);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    messages,
                    temperature = 0,
                    max_tokens = maxTokens,
                    tools = new[]
                    {
                        new
                        {
                            type = "function",
                            function = new
                            {
                                name = "document_search",
                                description =
                                    "Search authorized enterprise " +
                                    "documents.",
                                parameters = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        query = new
                                        {
                                            type = "string"
                                        },
                                        topK = new
                                        {
                                            type = "integer",
                                            minimum = 1,
                                            maximum = 20
                                        }
                                    },
                                    required = new[] { "query" }
                                }
                            }
                        }
                    }
                }),
            Encoding.UTF8,
            "application/json");

        return await http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private async Task<SearchToolResult> SearchAsync(
        SearchArguments arguments,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(arguments.Query))
        {
            return new SearchToolResult([], []);
        }

        var baseUrl =
            configuration["Services:SearchServiceUrl"]?.TrimEnd('/')
            ?? throw new InvalidOperationException(
                "Services:SearchServiceUrl missing.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/api/v1/search");

        var authorization =
            context.HttpContext?.Request.Headers.Authorization
                .ToString();

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            request.Headers.TryAddWithoutValidation(
                "Authorization",
                authorization);
        }

        var tenant =
            context.HttpContext?.User.FindFirst("tid")?.Value;

        if (!string.IsNullOrWhiteSpace(tenant))
        {
            request.Headers.TryAddWithoutValidation(
                "X-Tenant-Id",
                tenant);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new
                {
                    query = arguments.Query,
                    topK = Math.Clamp(
                        arguments.TopK <= 0 ? 8 : arguments.TopK,
                        1,
                        20)
                }),
            Encoding.UTF8,
            "application/json");

        using var response = await http.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var results =
            await response.Content.ReadFromJsonAsync<List<SearchHit>>(
                cancellationToken: cancellationToken)
            ?? [];

        return new SearchToolResult(
            results
                .Select(x => x.Citation)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray(),
            results);
    }

    private static int GetTokenUsage(JsonDocument document)
    {
        return document.RootElement.TryGetProperty(
                   "usage",
                   out var usage) &&
               usage.TryGetProperty(
                   "total_tokens",
                   out var tokens)
            ? tokens.GetInt32()
            : 0;
    }

    private sealed record SearchArguments(
        string Query = "",
        int TopK = 8);

    private sealed record SearchHit(
        string DocumentId,
        string Text,
        double Score,
        string Citation);

    private sealed record SearchToolResult(
        IReadOnlyList<string> Citations,
        IReadOnlyList<SearchHit> Results);
}