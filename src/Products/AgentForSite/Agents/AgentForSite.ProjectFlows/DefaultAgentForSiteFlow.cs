using AgentCore.Abstractions;
using AgentForSite.AgentImplementations;

namespace AgentForSite.ProjectFlows;

public sealed class DefaultAgentForSiteFlow(IOpenAiAgentClient openAi) : IProjectFlow
{
    public async Task<AgentRunResult> RunAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var reply = await openAi.GetReplyAsync(context.UserMessage, cancellationToken).ConfigureAwait(false);
        return new AgentRunResult(true, AssistantReply: reply, Error: null);
    }
}
