using EnterpriseDocumentIntelligence.SearchService.Application;
using EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;
using EnterpriseDocumentIntelligence.SearchService.Domain;
using Xunit;

public sealed class SearchServiceLayerTests
{
    [Fact]
    public void SearchQuery_Requires_Valid_Identity()
    {
        Assert.Throws<ArgumentException>(() =>
            SearchQuery.Create(
                Guid.Empty,
                Guid.NewGuid(),
                "contract",
                10));

        Assert.Throws<ArgumentException>(() =>
            SearchQuery.Create(
                Guid.NewGuid(),
                Guid.Empty,
                "contract",
                10));
    }

    [Fact]
    public void SearchQuery_Requires_Valid_Query_And_TopK()
    {
        var tenant = Guid.NewGuid();
        var user = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() =>
            SearchQuery.Create(tenant, user, "", 10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SearchQuery.Create(tenant, user, "contract", 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SearchQuery.Create(tenant, user, "contract", 51));
    }

    [Fact]
    public void SearchQuery_Normalizes_Text()
    {
        var query = SearchQuery.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  contract terms  ",
            5);

        Assert.Equal("contract terms", query.Text);
        Assert.Equal(5, query.TopK);
    }

    [Fact]
    public async Task Application_Embeds_Query_And_Delegates_To_Repository()
    {
        var embedding = new FakeEmbeddingService();
        var repository = new FakeSearchRepository();
        var service = new SearchService(embedding, repository);

        var query = SearchQuery.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "contract terms",
            3);

        var results = await service.SearchAsync(query, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("document-1", results[0].DocumentId);
        Assert.Equal("contract terms", embedding.LastText);
        Assert.Equal(3, repository.LastQuery!.TopK);
        Assert.Equal(new float[] { 1, 2, 3 }, repository.LastVector.ToArray());
    }

    private sealed class FakeEmbeddingService : IQueryEmbeddingService
    {
        public string? LastText { get; private set; }

        public Task<float[]> CreateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken)
        {
            LastText = text;
            return Task.FromResult(new float[] { 1, 2, 3 });
        }
    }

    private sealed class FakeSearchRepository : ISearchRepository
    {
        public SearchQuery? LastQuery { get; private set; }

        public ReadOnlyMemory<float> LastVector { get; private set; }

        public Task<IReadOnlyList<SearchHit>> SearchAsync(
            SearchQuery query,
            ReadOnlyMemory<float> vector,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            LastVector = vector;

            IReadOnlyList<SearchHit> results =
            [
                new SearchHit(
                    "document-1",
                    "contract terms",
                    0.98,
                    "documents/contract.pdf")
            ];

            return Task.FromResult(results);
        }
    }
}