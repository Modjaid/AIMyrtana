namespace AgentCore.Integrations;

public interface IAgentConversationStore
{
    Task<IReadOnlyList<ConversationTurn>> GetRecentAsync(
        string conversationId,
        int maxTurns,
        CancellationToken cancellationToken = default);

    Task AppendAsync(
        string conversationId,
        ConversationTurn turn,
        CancellationToken cancellationToken = default);
}

public sealed record ConversationTurn(string Role, string Content, DateTimeOffset At);
