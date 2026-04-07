using System.Diagnostics;
using Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Messaging.Runtime;

public sealed class WebhookDispatcher
{
    private readonly IReadOnlyDictionary<ChannelKind, IWebhookHandler> _handlers;
    private readonly IMessagingMetrics _metrics;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IEnumerable<IWebhookHandler> handlers,
        IMessagingMetrics? metrics,
        ILogger<WebhookDispatcher> logger)
    {
        _handlers = handlers
            .GroupBy(h => h.Channel)
            .ToDictionary(g => g.Key, g => g.First());
        _metrics = metrics ?? new NullMessagingMetrics();
        _logger = logger;
    }

    public async Task<WebhookHandleResult> DispatchAsync(
        ChannelKind channel,
        WebhookContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(channel, out var handler))
        {
            _logger.LogWarning("No webhook handler registered for {Channel}", channel);
            return new WebhookHandleResult(false, Error: $"No handler for {channel}");
        }

        var sw = Stopwatch.StartNew();
        _metrics.WebhookReceived(channel, handler.GetType().Name);
        try
        {
            var result = await handler.HandleAsync(context, cancellationToken).ConfigureAwait(false);
            var count = result.Messages?.Count ?? 0;
            _metrics.WebhookHandled(channel, result.Handled && result.Error is null, count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook handler failed for {Channel}", channel);
            _metrics.WebhookHandled(channel, false, 0);
            return new WebhookHandleResult(false, Error: ex.Message);
        }
        finally
        {
            sw.Stop();
            _logger.LogDebug("Webhook {Channel} handled in {ElapsedMs} ms", channel, sw.ElapsedMilliseconds);
        }
    }
}
