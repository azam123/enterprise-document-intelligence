namespace EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;

public interface IQueryEmbeddingService
{
    Task<float[]> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken);
}