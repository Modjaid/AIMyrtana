using Messaging.Abstractions;

namespace Adapters.WhatsApp;

public sealed class WhatsAppAdapter : IMessageAdapter
{
    public WhatsAppAdapter(IWhatsAppCloudApi api)
    {
        WebhookHandler = new WhatsAppWebhookHandler();
        OutboundSender = new WhatsAppOutboundSender(api);
    }

    public ChannelKind Channel => ChannelKind.WhatsApp;
    public IWebhookHandler WebhookHandler { get; }
    public IOutboundSender OutboundSender { get; }
}
