namespace Messaging.Abstractions;

/// <summary>
/// Product-specific composition can implement this to map a channel to inbound parsing and outbound sending.
/// </summary>
public interface IMessageAdapter
{
    ChannelKind Channel { get; }
    IWebhookHandler WebhookHandler { get; }
    IOutboundSender OutboundSender { get; }
}
