using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

namespace EnterpriseDocumentIntelligence.IndexingService;

public static class IndexDocumentFactory
{
    public static SearchDocument Create(Guid tenantId, Guid documentId, Guid versionId, EmbeddedChunk chunk, IReadOnlyList<Guid>? allowedPrincipalIds)
    {
        return new SearchDocument
        {
            ["id"] = chunk.ChunkId.ToString(),
            ["TenantId"] = tenantId.ToString(),
            ["DocumentId"] = documentId.ToString(),
            ["VersionId"] = versionId.ToString(),
            ["ChunkNumber"] = chunk.Number,
            ["Text"] = chunk.Text,
            ["Citation"] = $"document:{documentId}/version:{versionId}/chunk:{chunk.Number}",
            ["ContentVector"] = chunk.Embedding,
            ["AllowedPrincipalIds"] = (allowedPrincipalIds ?? Array.Empty<Guid>()).Select(x => x.ToString()).ToArray()
        };
    }
}