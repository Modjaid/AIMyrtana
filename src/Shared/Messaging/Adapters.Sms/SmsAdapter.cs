using Messaging.Abstractions;

namespace Adapters.Sms;

/// <summary>
/// SMS has no universal inbound webhook shape; products register provider-specific <see cref="IWebhookHandler"/> if needed.
/// </summary>
public sealed class SmsAdapter : IMessageAdapter
{
    public SmsAdapter(ISmsGateway gateway, IWebhookHandler? inboundWebhook = null)
    {
        OutboundSender = new SmsOutboundSender(gateway);
        WebhookHandler = inboundWebhook ?? new NoOpSmsWebhookHandler();
    }

    public ChannelKind Channel => ChannelKind.Sms;
    public IWebhookHandler WebhookHandler { get; }
    public IOutboundSender OutboundSender { get; }
}
