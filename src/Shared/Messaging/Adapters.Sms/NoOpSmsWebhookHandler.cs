using Messaging.Abstractions;

namespace Adapters.Sms;

internal sealed class NoOpSmsWebhookHandler : IWebhookHandler
{
    public ChannelKind Channel => ChannelKind.Sms;

    public Task<WebhookHandleResult> HandleAsync(
        WebhookContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new WebhookHandleResult(true, Array.Empty<InboundMessage>()));
}
