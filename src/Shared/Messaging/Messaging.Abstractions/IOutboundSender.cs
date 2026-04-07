namespace Messaging.Abstractions;

public interface IOutboundSender
{
    ChannelKind Channel { get; }

    Task<SendResult> SendAsync(
        OutboundMessage message,
        CancellationToken cancellationToken = default);
}
