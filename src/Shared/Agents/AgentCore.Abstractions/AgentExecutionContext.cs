namespace AgentCore.Abstractions;

public sealed class AgentExecutionContext
{
    public required string ConversationId { get; init; }
    public required string UserMessage { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
