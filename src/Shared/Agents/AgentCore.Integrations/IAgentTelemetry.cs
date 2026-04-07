namespace AgentCore.Integrations;

public interface IAgentTelemetry
{
    void PolicyEvaluated(string conversationId, bool allowed);
    void FlowStarted(string conversationId);
    void FlowCompleted(string conversationId, bool success);
}
