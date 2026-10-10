namespace EnterpriseDocumentIntelligence.AgentService.Domain;

public enum AgentTurnRole
{
    User,
    Assistant,
    Tool
}

public sealed record AgentTurn
{
    public AgentTurnRole Role { get; init; }
    public string Content { get; init; }
    public string? ToolName { get; init; }

    public AgentTurn(AgentTurnRole role, string content, string? toolName = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.", nameof(content));

        Role = role;
        Content = content;
        ToolName = toolName;
    }
}

public sealed class AgentConversation(Guid id, string tenantId)
{
    private readonly List<AgentTurn> _turns = [];

    public Guid Id { get; } = id == Guid.Empty
        ? throw new ArgumentException("Conversation id is required.", nameof(id))
        : id;

    public string TenantId { get; } =
        string.IsNullOrWhiteSpace(tenantId)
            ? throw new ArgumentException("Tenant is required.", nameof(tenantId))
            : tenantId.Trim();

    public IReadOnlyList<AgentTurn> Turns => _turns;

    public void Add(AgentTurn turn)
    {
        ArgumentNullException.ThrowIfNull(turn);
        _turns.Add(turn);
    }
}
