using Messaging.Abstractions;
using Telegram.Bot;

namespace Adapters.Telegram;

/// <summary>
/// Bundles Telegram webhook + outbound for product DI registration.
/// </summary>
public sealed class TelegramAdapter : IMessageAdapter
{
    public TelegramAdapter(ITelegramBotClient client)
    {
        WebhookHandler = new TelegramWebhookHandler();
        OutboundSender = new TelegramOutboundSender(client);
    }

    public ChannelKind Channel => ChannelKind.Telegram;
    public IWebhookHandler WebhookHandler { get; }
    public IOutboundSender OutboundSender { get; }
}
