using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

using EnterpriseDocumentIntelligence.EmbeddingService.Application;
using EnterpriseDocumentIntelligence.EmbeddingService.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.EmbeddingService.Infrastructure;

public sealed class InMemoryEmbeddingStore : IEmbeddingStore
{
    private readonly ConcurrentDictionary<string, EmbeddingVector> _items = new();

    public Task SaveAsync(
        EmbeddingVector vector,
        CancellationToken cancellationToken = default)
    {
        _items[$"{vector.DocumentId:N}:{vector.ChunkNumber}"] = vector;

        return Task.CompletedTask;
    }

    public Task<EmbeddingVector?> GetAsync(
        Guid documentId,
        int chunkNumber,
        CancellationToken cancellationToken = default)
    {
        var key = $"{documentId:N}:{chunkNumber}";

        return Task.FromResult(
            _items.TryGetValue(key, out var vector)
                ? vector
                : null);
    }
}

public sealed class DeterministicEmbeddingGenerator : IEmbeddingGenerator
{
    public Task<IReadOnlyList<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));

        IReadOnlyList<float> values = hash
            .Take(8)
            .Select(value => (float)value / 255f)
            .ToArray();

        return Task.FromResult(values);
    }
}

public static class EmbeddingServiceInfrastructure
{
    public static IServiceCollection AddEmbeddingApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IEmbeddingStore, InMemoryEmbeddingStore>();
        services.AddSingleton<IEmbeddingGenerator, DeterministicEmbeddingGenerator>();
        services.AddScoped<EmbeddingApplication>();

        return services;
    }
}
