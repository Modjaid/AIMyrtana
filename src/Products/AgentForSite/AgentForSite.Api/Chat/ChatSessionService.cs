using System.Collections.Concurrent;
using AgentForSite.AgentImplementations;
using Microsoft.Extensions.Caching.Memory;

namespace AgentForSite.Api.Chat;

/// <summary>
/// In-memory chat history keyed by client <c>sessionId</c> (not IP), with sliding + absolute expiration.
/// </summary>
public sealed class ChatSessionService(IMemoryCache cache, ILogger<ChatSessionService> logger)
{
    private const string CacheKeyPrefix = "chat_session:";
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(2);
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromHours(24);
    private const int MaxContentLength = 24_000;
    private const int MaxMessages = 100;

    private readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.Ordinal);

    public static bool IsValidSessionId(string? sessionId) =>
        !string.IsNullOrWhiteSpace(sessionId) && Guid.TryParse(sessionId, out _);

    /// <summary>
    /// Merges client payload with server copy; returns messages to send to the model (ends with latest user turn).
    /// </summary>
    public bool TryPrepareTurn(
        string sessionId,
        IReadOnlyList<ChatRequestMessage> clientMessages,
        out IReadOnlyList<OpenAiChatMessage>? forLlm,
        out string? error)
    {
        forLlm = null;
        error = null;

        if (clientMessages is null || clientMessages.Count == 0)
        {
            error = "Missing messages.";
            return false;
        }

        var lastIdx = -1;
        for (var i = clientMessages.Count - 1; i >= 0; i--)
        {
            if (string.Equals(clientMessages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
            {
                lastIdx = i;
                break;
            }
        }

        if (lastIdx < 0 || string.IsNullOrWhiteSpace(clientMessages[lastIdx].Content))
        {
            error = "Missing user message.";
            return false;
        }

        foreach (var m in clientMessages)
        {
            if (m.Content is { Length: > MaxContentLength })
            {
                error = "Message too long.";
                return false;
            }
        }

        var lastUser = ToOpenAi(clientMessages[lastIdx]);
        var prefix = new List<ChatRequestMessage>(lastIdx);
        for (var i = 0; i < lastIdx; i++)
            prefix.Add(clientMessages[i]);

        var gate = _locks.GetOrAdd(sessionId, _ => new object());
        lock (gate)
        {
            var key = CacheKeyPrefix + sessionId;
            if (!cache.TryGetValue(key, out List<OpenAiChatMessage>? server) || server is null)
                server = [];

            if (server.Count == 0)
            {
                foreach (var m in clientMessages)
                    server.Add(ToOpenAi(m));
            }
            else
            {
                if (PrefixEqualsServer(prefix, server))
                {
                    var end = server[^1];
                    if (!string.Equals(end.Role, "user", StringComparison.OrdinalIgnoreCase) ||
                        end.Content != lastUser.Content)
                        server.Add(lastUser);
                }
                else
                {
                    logger.LogDebug(
                        "Chat session {SessionId}: client prefix diverged from server; using server + last user.",
                        sessionId);
                    var end = server[^1];
                    if (!string.Equals(end.Role, "user", StringComparison.OrdinalIgnoreCase) ||
                        end.Content != lastUser.Content)
                        server.Add(lastUser);
                }
            }

            TrimHistory(server);
            TouchCache(key, server);
            forLlm = server.ToArray();
        }

        return true;
    }

    public void AppendAssistant(string sessionId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > MaxContentLength)
            return;

        var gate = _locks.GetOrAdd(sessionId, _ => new object());
        lock (gate)
        {
            var key = CacheKeyPrefix + sessionId;
            if (!cache.TryGetValue(key, out List<OpenAiChatMessage>? server) || server is null)
                return;

            server.Add(new OpenAiChatMessage("assistant", content));
            TrimHistory(server);
            TouchCache(key, server);
        }
    }

    private static void TrimHistory(List<OpenAiChatMessage> server)
    {
        while (server.Count > MaxMessages)
        {
            if (server.Count > 0 &&
                string.Equals(server[0].Role, "system", StringComparison.OrdinalIgnoreCase))
                server.RemoveAt(1);
            else
                server.RemoveAt(0);
        }
    }

    private void TouchCache(string key, List<OpenAiChatMessage> server)
    {
        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(SlidingExpiration)
            .SetAbsoluteExpiration(AbsoluteExpiration);
        cache.Set(key, server, options);
    }

    private static bool PrefixEqualsServer(IReadOnlyList<ChatRequestMessage> prefix, IReadOnlyList<OpenAiChatMessage> server)
    {
        if (prefix.Count != server.Count)
            return false;
        for (var i = 0; i < prefix.Count; i++)
        {
            if (!string.Equals(prefix[i].Role, server[i].Role, StringComparison.OrdinalIgnoreCase))
                return false;
            if (prefix[i].Content != server[i].Content)
                return false;
        }

        return true;
    }

    private static OpenAiChatMessage ToOpenAi(ChatRequestMessage m) =>
        new(m.Role?.Trim() ?? "user", m.Content ?? "");
}

public sealed record ChatRequestMessage(string Role, string Content);
