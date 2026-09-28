using EnterpriseDocumentIntelligence.AgentService.Domain;

namespace EnterpriseDocumentIntelligence.AgentService.Application;

public interface IAgentConversationStore
{
    Task<AgentConversation?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AgentConversation conversation,
        CancellationToken cancellationToken = default);
}

public sealed class AgentApplication(IAgentConversationStore store)
{
    public async Task<AgentConversation> StartAsync(
        Guid id,
        string tenantId,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var conversation =
            await store.GetAsync(id, cancellationToken)
            ?? new AgentConversation(id, tenantId);

        conversation.Add(new AgentTurn(AgentTurnRole.User, prompt));

        await store.SaveAsync(conversation, cancellationToken);

        return conversation;
    }
}
