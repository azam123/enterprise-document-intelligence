namespace EnterpriseDocumentIntelligence.SearchService.Domain;

public sealed record SearchHit(
    string DocumentId,
    string Text,
    double Score,
    string Citation);