using Messaging.Abstractions;

namespace Adapters.Sms;

public sealed class SmsOutboundSender : IOutboundSender
{
    private readonly ISmsGateway _gateway;

    public SmsOutboundSender(ISmsGateway gateway) => _gateway = gateway;

    public ChannelKind Channel => ChannelKind.Sms;

    public Task<SendResult> SendAsync(
        OutboundMessage message,
        CancellationToken cancellationToken = default) =>
        message.Channel != ChannelKind.Sms
            ? Task.FromResult(new SendResult(false, Error: "Wrong channel"))
            : _gateway.SendAsync(message.ExternalChatId, message.Text, cancellationToken);
}
