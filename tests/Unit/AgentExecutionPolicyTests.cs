using BuildingBlocks.AI.Security;
using Xunit;

namespace Unit;

public sealed class AgentExecutionPolicyTests
{
    private readonly AgentExecutionPolicy _policy =
        new(["search", "summarize"], maxToolCalls: 3, maxTokens: 4_000, timeout: TimeSpan.FromSeconds(20));

    [Fact]
    public void Evaluate_AllowsConfiguredToolWithinLimits()
    {
        Assert.True(_policy.Evaluate("search", completedToolCalls: 0, requestedTokenBudget: 1_000).Allowed);
    }

    [Fact]
    public void Evaluate_RejectsToolOutsideAllowList()
    {
        Assert.False(_policy.Evaluate("delete-all", 0, 1_000).Allowed);
    }

    [Fact]
    public void Evaluate_RejectsToolCallLimit()
    {
        Assert.False(_policy.Evaluate("search", 3, 1_000).Allowed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4001)]
    public void Evaluate_RejectsInvalidTokenBudget(int tokenBudget)
    {
        Assert.False(_policy.Evaluate("search", 0, tokenBudget).Allowed);
    }

    [Fact]
    public void Constructor_RejectsInvalidLimits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AgentExecutionPolicy(["search"], 1, 0, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Evaluate_IsCaseInsensitiveForToolNames()
    {
        Assert.True(_policy.Evaluate("SEARCH", 0, 100).Allowed);
    }
}
