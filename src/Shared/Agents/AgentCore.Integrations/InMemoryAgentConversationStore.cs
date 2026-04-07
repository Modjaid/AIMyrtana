using System.Collections.Concurrent;

namespace AgentCore.Integrations;

public sealed class InMemoryAgentConversationStore : IAgentConversationStore
{
    private readonly ConcurrentDictionary<string, List<ConversationTurn>> _byConversation = new();

    public Task<IReadOnlyList<ConversationTurn>> GetRecentAsync(
        string conversationId,
        int maxTurns,
        CancellationToken cancellationToken = default)
    {
        if (!_byConversation.TryGetValue(conversationId, out var list))
            return Task.FromResult<IReadOnlyList<ConversationTurn>>(Array.Empty<ConversationTurn>());

        lock (list)
        {
            var take = Math.Max(0, maxTurns);
            var slice = list.Count <= take
                ? list.ToList()
                : list.Skip(list.Count - take).ToList();
            return Task.FromResult<IReadOnlyList<ConversationTurn>>(slice);
        }
    }

    public Task AppendAsync(
        string conversationId,
        ConversationTurn turn,
        CancellationToken cancellationToken = default)
    {
        var list = _byConversation.GetOrAdd(conversationId, _ => new List<ConversationTurn>());
        lock (list)
        {
            list.Add(turn);
        }

        return Task.CompletedTask;
    }
}
