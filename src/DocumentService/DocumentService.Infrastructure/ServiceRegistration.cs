using EnterpriseDocumentIntelligence.DocumentService.Application;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.DocumentService.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddDocumentService(
        this IServiceCollection services)
    {
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentStorage, BlobDocumentStorage>();
        services.AddScoped<IDocumentEventPublisher, DocumentEventPublisher>();
        services.AddScoped<DocumentServiceApplication>();
        return services;
    }
}
