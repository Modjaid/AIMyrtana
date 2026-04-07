using AgentCore.Abstractions;
using AgentCore.Integrations;
using Microsoft.Extensions.Logging;

namespace AgentCore;

/// <summary>
/// Orchestrates policy → flow without embedding product-specific logic.
/// </summary>
public sealed class AgentRunner
{
    private readonly IAgentPolicy _policy;
    private readonly IProjectFlow _flow;
    private readonly IAgentTelemetry _telemetry;
    private readonly ILogger<AgentRunner> _logger;

    public AgentRunner(
        IAgentPolicy policy,
        IProjectFlow flow,
        IAgentTelemetry? telemetry,
        ILogger<AgentRunner> logger)
    {
        _policy = policy;
        _flow = flow;
        _telemetry = telemetry ?? new NullAgentTelemetry();
        _logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var policy = await _policy.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        _telemetry.PolicyEvaluated(context.ConversationId, policy.Allow);
        if (!policy.Allow)
        {
            _logger.LogInformation(
                "Policy blocked conversation {Id}: {Reason}",
                context.ConversationId,
                policy.BlockReason);
            return new AgentRunResult(false, null, policy.BlockReason ?? "Blocked by policy");
        }

        _telemetry.FlowStarted(context.ConversationId);
        try
        {
            var result = await _flow.RunAsync(context, cancellationToken).ConfigureAwait(false);
            _telemetry.FlowCompleted(context.ConversationId, result.Ok);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent flow failed for {Id}", context.ConversationId);
            _telemetry.FlowCompleted(context.ConversationId, false);
            return new AgentRunResult(false, null, ex.Message);
        }
    }
}
