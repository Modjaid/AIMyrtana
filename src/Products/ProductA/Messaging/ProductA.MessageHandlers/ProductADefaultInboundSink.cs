using Messaging.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.Logging;

namespace ProductA.MessageHandlers;

public sealed class ProductADefaultInboundSink(ILogger<ProductADefaultInboundSink> logger) : IInboundMessageSink
{
    public Task HandleAsync(InboundMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Inbound {Channel} chat={Chat} text={Text}",
            message.Channel,
            message.ExternalChatId,
            message.Text);
        return Task.CompletedTask;
    }
}
