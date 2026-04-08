using AgentCore.Abstractions;

namespace ProductA.AgentPolicies;

public sealed class DefaultProductAPolicy : IAgentPolicy
{
    public Task<PolicyResult> EvaluateAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PolicyResult(true));
}
