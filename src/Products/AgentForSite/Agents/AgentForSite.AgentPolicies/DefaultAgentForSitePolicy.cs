using AgentCore.Abstractions;

namespace AgentForSite.AgentPolicies;

public sealed class DefaultAgentForSitePolicy : IAgentPolicy
{
    public Task<PolicyResult> EvaluateAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PolicyResult(true));
}
