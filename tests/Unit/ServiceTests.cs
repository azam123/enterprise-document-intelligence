using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.DocumentService;
using EnterpriseDocumentIntelligence.IngestionService;
using EnterpriseDocumentIntelligence.IndexingService;
using EnterpriseDocumentIntelligence.McpGateway;
using EnterpriseDocumentIntelligence.ProcessingService;
using EnterpriseDocumentIntelligence.SearchService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using Xunit;

public sealed class DomainBehaviorTests
{
    [Fact]
    public void Document_lifecycle_updates_status_and_version()
    {
        var document = new Document(Guid.NewGuid(), "a.pdf", "application/pdf", 10);
        var version = new DocumentVersion(document.Id, 1, "blob", "abc", 10);
        document.Versions.Add(version);
        document.SetCurrentVersion(version.Id);
        document.MarkProcessing();
        document.MarkReady();
        Assert.Equal(version.Id, document.CurrentVersionId);
        Assert.Equal("Ready", document.Status);
    }

    [Fact]
    public void Processing_job_tracks_attempts_and_failure()
    {
        var job = new ProcessingJob(Guid.NewGuid(), "extract");
        job.Start();
        job.Fail("boom");
        Assert.Equal(1, job.Attempts);
        Assert.Equal("Failed", job.Status);
        Assert.Equal("boom", job.Error);
    }

    [Fact]
    public void Audit_event_requires_tenant_action_and_resource()
    {
        Assert.Throws<ArgumentException>(() => new AuditEvent(Guid.Empty, null, "read", "Document", null, "Success", null, null));
        Assert.Throws<ArgumentException>(() => new AuditEvent(Guid.NewGuid(), null, "", "Document", null, "Success", null, null));
    }
}

public sealed class SemanticChunkerTests
{
    [Fact]
    public void Empty_text_returns_no_chunks()
    {
        Assert.Empty(new SemanticChunker().Chunk(" "));
    }

    [Fact]
    public void Sentences_are_grouped_under_token_limit()
    {
        var result = new SemanticChunker().Chunk("One short sentence. Another short sentence. Third sentence.", 20, 2);
        Assert.NotEmpty(result);
        Assert.All(result, x => Assert.True(x.TokenCount <= 20));
        Assert.All(result, x => Assert.False(string.IsNullOrWhiteSpace(x.Text)));
    }

    [Fact]
    public void Invalid_limits_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticChunker().Chunk("hello world", 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SemanticChunker().Chunk("hello world", 32, 32));
    }
}

public sealed class IndexDocumentFactoryTests
{
    [Fact]
    public void Creates_document_with_tenant_acl_vector_and_citation()
    {
        var tenant = Guid.NewGuid();
        var doc = Guid.NewGuid();
        var version = Guid.NewGuid();
        var principal = Guid.NewGuid();
        var chunk = new EmbeddedChunk(Guid.NewGuid(), 3, "hello", 2, [0.1f, 0.2f]);
        var result = IndexDocumentFactory.Create(tenant, doc, version, chunk, [principal]);
        Assert.Equal(doc.ToString(), result["DocumentId"]);
        Assert.Equal(version.ToString(), result["VersionId"]);
        Assert.Equal("document:" + doc + "/version:" + version + "/chunk:3", result["Citation"]);
        Assert.Contains(principal.ToString(), (string[])result["AllowedPrincipalIds"]);
    }
}

public sealed class SearchPolicyTests
{
    [Fact]
    public void Search_filter_is_tenant_and_principal_scoped()
    {
        var tenant = Guid.NewGuid();
        var user = Guid.NewGuid();
        var filter = SearchApplication.BuildFilter(tenant, user);
        Assert.Contains($"TenantId eq '{tenant}'", filter);
        Assert.Contains(user.ToString(), filter);
        Assert.Contains("AllowedPrincipalIds", filter);
    }

    [Fact]
    public void Search_request_defaults_top_k()
    {
        var request = new SearchRequest("hello");
        Assert.Equal(10, request.TopK);
    }
}

public sealed class DocumentExtractorTests
{
    [Fact]
    public async Task Text_extraction_preserves_content()
    {
        var extractor = new DocumentExtractor(new HttpClient(), new ConfigurationBuilder().Build());
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello\nworld"));
        var result = await extractor.ExtractAsync(stream, "text/plain", CancellationToken.None);
        Assert.Equal("hello\nworld", result);
    }

    [Fact]
    public async Task Docx_extraction_reads_word_text()
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("word/document.xml");
            await using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync("""<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body><w:p><w:r><w:t>Hello</w:t></w:r></w:p></w:body></w:document>""");
        }
        stream.Position = 0;
        var extractor = new DocumentExtractor(new HttpClient(), new ConfigurationBuilder().Build());
        Assert.Equal("Hello", await extractor.ExtractAsync(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", CancellationToken.None));
    }

    [Fact]
    public async Task Pdf_requires_document_intelligence_configuration()
    {
        var extractor = new DocumentExtractor(new HttpClient(), new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<InvalidOperationException>(() => extractor.ExtractAsync(new MemoryStream([1,2,3]), "application/pdf", CancellationToken.None));
    }
}

public sealed class McpControllerTests
{
    [Fact]
    public void Tool_request_is_constructible()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("""{"query":"azure","topK":5}""");
        var request = new McpToolRequest("document.search", doc.RootElement.Clone());
        Assert.Equal("document.search", request.Tool);
        Assert.Equal(5, request.Arguments.GetProperty("topK").GetInt32());
    }
}

public sealed class MessageContractTests
{
    [Fact]
    public void Processing_contract_preserves_acl()
    {
        var principal = Guid.NewGuid();
        var message = new DocumentProcessed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            [new ChunkContract(Guid.NewGuid(), 0, "text", 2)], DateTimeOffset.UtcNow, "corr", [principal]);
        Assert.Contains(principal, message.AllowedPrincipalIds!);
    }

    [Fact]
    public void Topics_are_stable()
    {
        Assert.Equal("document-events", Topics.DocumentEvents);
        Assert.Equal("audit-events", Topics.AuditEvents);
    }
}

public sealed class EmbeddingClientTests
{
    [Fact]
    public async Task Embedding_client_parses_vector()
    {
        var handler = new StubHandler("""{"data":[{"embedding":[0.1,0.2,0.3]}]}""");
        var client = new EnterpriseDocumentIntelligence.EmbeddingService.AzureOpenAiEmbeddingClient(
            new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            {
                ["AzureOpenAI:Endpoint"]="https://example.openai.azure.com",
                ["AzureOpenAI:EmbeddingDeployment"]="embedding"
            }).Build(), NullLogger<EnterpriseDocumentIntelligence.EmbeddingService.AzureOpenAiEmbeddingClient>.Instance);
        var vector = await client.EmbedAsync("hello", CancellationToken.None);
        Assert.Equal(3, vector.Length);
        Assert.Equal(0.2f, vector[1]);
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}

public sealed class CurrentUserTests
{
    [Fact]
    public void Current_user_reads_oid_and_tenant()
    {
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid();
        var tenant = Guid.NewGuid();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("oid", userId.ToString()),
            new Claim("tid", tenant.ToString()),
            new Claim(ClaimTypes.Role, "Document.Reader")
        ], "test"));
        var accessor = new HttpContextAccessor { HttpContext = context };
        var current = new EnterpriseDocumentIntelligence.BuildingBlocks.Security.CurrentUser(accessor);
        Assert.Equal(userId, current.UserId);
        Assert.Equal(tenant, current.TenantId);
        Assert.True(current.IsInRole("Document.Reader"));
    }
}

public sealed class StubMessagePublisher : EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure.IMessagePublisher
{
    public List<object> Messages { get; } = [];
    public Task PublishAsync<T>(string topic, T message, string correlationId, CancellationToken ct = default)
    {
        Messages.Add(message!);
        return Task.CompletedTask;
    }
}