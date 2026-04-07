using Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Messaging.Runtime;

public sealed class LoggingInboundMiddleware : IInboundMessageMiddleware
{
    private readonly ILogger<LoggingInboundMiddleware> _logger;

    public LoggingInboundMiddleware(ILogger<LoggingInboundMiddleware> logger) =>
        _logger = logger;

    public Task InvokeAsync(
        InboundMessage message,
        Func<InboundMessage, Task> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Inbound message {Channel} chat {ChatId}",
            message.Channel,
            message.ExternalChatId);
        return next(message);
    }
}
