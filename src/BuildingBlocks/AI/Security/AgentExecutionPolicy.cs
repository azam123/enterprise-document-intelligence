namespace BuildingBlocks.AI.Security;

/// <summary>Enforces deterministic limits around an agent tool-execution loop.</summary>
public sealed class AgentExecutionPolicy
{
    private readonly HashSet<string> _allowedTools;

    public AgentExecutionPolicy(IEnumerable<string> allowedTools, int maxToolCalls, int maxTokens, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(allowedTools);
        if (maxToolCalls < 0) throw new ArgumentOutOfRangeException(nameof(maxToolCalls));
        if (maxTokens <= 0) throw new ArgumentOutOfRangeException(nameof(maxTokens));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        _allowedTools = new HashSet<string>(
            allowedTools.Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.OrdinalIgnoreCase);
        MaxToolCalls = maxToolCalls;
        MaxTokens = maxTokens;
        Timeout = timeout;
    }

    public int MaxToolCalls { get; }
    public int MaxTokens { get; }
    public TimeSpan Timeout { get; }

    public AgentPolicyDecision Evaluate(string toolName, int completedToolCalls, int requestedTokenBudget)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return AgentPolicyDecision.Deny("Tool name is required.");
        if (!_allowedTools.Contains(toolName))
            return AgentPolicyDecision.Deny("Tool is not on the configured allow-list.");
        if (completedToolCalls < 0 || completedToolCalls >= MaxToolCalls)
            return AgentPolicyDecision.Deny("Maximum tool-call count has been reached.");
        if (requestedTokenBudget <= 0 || requestedTokenBudget > MaxTokens)
            return AgentPolicyDecision.Deny("Requested token budget is outside the configured limit.");

        return AgentPolicyDecision.Allow();
    }
}

public sealed record AgentPolicyDecision(bool Allowed, string Reason)
{
    public static AgentPolicyDecision Allow() => new(true, "Agent action is within policy.");
    public static AgentPolicyDecision Deny(string reason) => new(false, reason);
}
