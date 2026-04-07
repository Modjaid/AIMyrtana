namespace Messaging.Abstractions;

/// <summary>
/// Optional hooks for metrics/tracing; products can register a no-op or OpenTelemetry-backed implementation.
/// </summary>
public interface IMessagingMetrics
{
    void WebhookReceived(ChannelKind channel, string handlerName);
    void WebhookHandled(ChannelKind channel, bool success, int messageCount);
    void OutboundAttempt(ChannelKind channel);
    void OutboundCompleted(ChannelKind channel, bool success);
}
