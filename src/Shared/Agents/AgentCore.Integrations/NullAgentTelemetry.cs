namespace AgentCore.Integrations;

public sealed class NullAgentTelemetry : IAgentTelemetry
{
    public void PolicyEvaluated(string conversationId, bool allowed) { }
    public void FlowStarted(string conversationId) { }
    public void FlowCompleted(string conversationId, bool success) { }
}
