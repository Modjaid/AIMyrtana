using Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Messaging.Runtime;

public sealed class RetryingOutboundSender : IOutboundSender
{
    private readonly IOutboundSender _inner;
    private readonly int _maxAttempts;
    private readonly TimeSpan _initialDelay;
    private readonly IMessagingMetrics _metrics;
    private readonly ILogger<RetryingOutboundSender> _logger;

    public RetryingOutboundSender(
        IOutboundSender inner,
        int maxAttempts,
        TimeSpan initialDelay,
        IMessagingMetrics? metrics,
        ILogger<RetryingOutboundSender> logger)
    {
        _inner = inner;
        _maxAttempts = Math.Max(1, maxAttempts);
        _initialDelay = initialDelay <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(200) : initialDelay;
        _metrics = metrics ?? new NullMessagingMetrics();
        _logger = logger;
    }

    public ChannelKind Channel => _inner.Channel;

    public async Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken = default)
    {
        _metrics.OutboundAttempt(Channel);
        Exception? last = null;
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                var result = await _inner.SendAsync(message, cancellationToken).ConfigureAwait(false);
                if (result.Ok)
                {
                    _metrics.OutboundCompleted(Channel, true);
                    return result;
                }

                last = new InvalidOperationException(result.Error ?? "Send failed");
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                last = ex;
                _logger.LogWarning(ex, "Outbound send attempt {Attempt}/{Max} failed for {Channel}", attempt, _maxAttempts, Channel);
            }

            if (attempt < _maxAttempts)
            {
                var delay = TimeSpan.FromTicks(_initialDelay.Ticks * (1L << (attempt - 1)));
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        _metrics.OutboundCompleted(Channel, false);
        return new SendResult(false, Error: last?.Message ?? "Send failed");
    }
}
