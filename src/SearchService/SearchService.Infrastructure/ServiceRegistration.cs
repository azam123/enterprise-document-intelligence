using EnterpriseDocumentIntelligence.SearchService.Application;
using EnterpriseDocumentIntelligence.SearchService.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.SearchService.Infrastructure;

public sealed class EmptySearchProvider : ISearchProvider
{
    public Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            (IReadOnlyList<SearchHit>)Array.Empty<SearchHit>());
    }
}

public static class SearchServiceInfrastructure
{
    public static IServiceCollection AddSearchApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<ISearchProvider, EmptySearchProvider>();
        services.AddScoped<SearchApplicationService>();

        return services;
    }
}
