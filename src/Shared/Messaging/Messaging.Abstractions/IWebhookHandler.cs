namespace Messaging.Abstractions;

public interface IWebhookHandler
{
    ChannelKind Channel { get; }

    Task<WebhookHandleResult> HandleAsync(
        WebhookContext context,
        CancellationToken cancellationToken = default);
}
