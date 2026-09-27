using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class AzureOpenAiEmbeddingClient(HttpClient http, IConfiguration configuration)
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));
        var endpoint=configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/')??throw new InvalidOperationException("AzureOpenAI:Endpoint missing");
        var deployment=configuration["AzureOpenAI:EmbeddingDeployment"]??throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment missing");
        var key=configuration["AzureOpenAI:ApiKey"];
        for(var attempt=1;attempt<=3;attempt++)
        {
            using var req=new HttpRequestMessage(HttpMethod.Post,$"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2024-10-21");
            if(!string.IsNullOrWhiteSpace(key)) req.Headers.Add("api-key",key);
            req.Content=new StringContent(JsonSerializer.Serialize(new{input=text}),Encoding.UTF8,"application/json");
            using var res=await http.SendAsync(req,ct);
            var body=await res.Content.ReadAsStringAsync(ct);
            if(res.IsSuccessStatusCode)
            {
                using var doc=JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            }
            if(attempt==3 || ((int)res.StatusCode<429 && (int)res.StatusCode<500)) res.EnsureSuccessStatusCode();
            await Task.Delay(TimeSpan.FromMilliseconds(250*Math.Pow(2,attempt-1)),ct);
        }
        throw new InvalidOperationException("Embedding request failed.");
    }
}
