using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace WebSites.Runtime;

/// <summary>
/// Simple fixed-window rate limit per key (e.g. client IP + form id).
/// </summary>
public sealed class InMemorySubmissionRateGate
{
    private readonly int _maxSubmissions;
    private readonly TimeSpan _window;
    private readonly ILogger<InMemorySubmissionRateGate>? _logger;
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _windows = new();

    public InMemorySubmissionRateGate(
        int maxSubmissions,
        TimeSpan window,
        ILogger<InMemorySubmissionRateGate>? logger = null)
    {
        _maxSubmissions = Math.Max(1, maxSubmissions);
        _window = window <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : window;
        _logger = logger;
    }

    public bool TryAllow(string key, DateTimeOffset now)
    {
        var q = _windows.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        lock (q)
        {
            while (q.Count > 0 && now - q.Peek() > _window)
                q.Dequeue();

            if (q.Count >= _maxSubmissions)
            {
                _logger?.LogWarning("Rate limit exceeded for {Key}", key);
                return false;
            }

            q.Enqueue(now);
            return true;
        }
    }
}
