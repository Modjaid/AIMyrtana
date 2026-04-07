using Messaging.Abstractions;

namespace Adapters.WhatsApp;

public sealed class WhatsAppOutboundSender : IOutboundSender
{
    private readonly IWhatsAppCloudApi _api;

    public WhatsAppOutboundSender(IWhatsAppCloudApi api) => _api = api;

    public ChannelKind Channel => ChannelKind.WhatsApp;

    public Task<SendResult> SendAsync(
        OutboundMessage message,
        CancellationToken cancellationToken = default) =>
        message.Channel != ChannelKind.WhatsApp
            ? Task.FromResult(new SendResult(false, Error: "Wrong channel"))
            : _api.SendTextAsync(message.ExternalChatId, message.Text, cancellationToken);
}
