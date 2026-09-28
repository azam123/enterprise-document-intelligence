using System.Collections.Concurrent;

using EnterpriseDocumentIntelligence.AgentService.Application;
using EnterpriseDocumentIntelligence.AgentService.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.AgentService.Infrastructure;

public sealed class InMemoryAgentConversationStore : IAgentConversationStore
{
    private readonly ConcurrentDictionary<Guid, AgentConversation> _items = new();

    public Task<AgentConversation?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _items.TryGetValue(id, out var conversation)
                ? conversation
                : null);
    }

    public Task SaveAsync(
        AgentConversation conversation,
        CancellationToken cancellationToken = default)
    {
        _items[conversation.Id] = conversation;

        return Task.CompletedTask;
    }
}

public static class AgentServiceInfrastructure
{
    public static IServiceCollection AddAgentApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<
            IAgentConversationStore,
            InMemoryAgentConversationStore>();

        services.AddScoped<AgentApplication>();

        return services;
    }
}
