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
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content is required.", nameof(content));
        Role = role;
        Content = content;
        ToolName = toolName;
    }
}

public sealed class AgentConversation
{
    private readonly List<AgentTurn> _turns = [];

    public AgentConversation(Guid id, string tenantId)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        Id = id;
        TenantId = tenantId;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public IReadOnlyList<AgentTurn> Turns => _turns;

    public void Add(AgentTurn turn)
    {
        if (turn is null) throw new ArgumentNullException(nameof(turn));
        _turns.Add(turn);
    }
}
