using Messaging.Abstractions;

namespace Messaging.Runtime;

public sealed class NullMessagingMetrics : IMessagingMetrics
{
    public void WebhookReceived(ChannelKind channel, string handlerName) { }
    public void WebhookHandled(ChannelKind channel, bool success, int messageCount) { }
    public void OutboundAttempt(ChannelKind channel) { }
    public void OutboundCompleted(ChannelKind channel, bool success) { }
}
