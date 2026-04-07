namespace AgentCore.Abstractions;

/// <summary>
/// Product-specific guardrails and routing; implemented in <c>ProductX.AgentPolicies</c>.
/// </summary>
public interface IAgentPolicy
{
    Task<PolicyResult> EvaluateAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}

public sealed record PolicyResult(bool Allow, string? BlockReason = null);
