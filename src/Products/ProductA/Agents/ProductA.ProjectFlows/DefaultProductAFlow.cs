using AgentCore.Abstractions;

namespace ProductA.ProjectFlows;

public sealed class DefaultProductAFlow : IProjectFlow
{
    public Task<AgentRunResult> RunAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AgentRunResult(true, AssistantReply: null, Error: null));
}
