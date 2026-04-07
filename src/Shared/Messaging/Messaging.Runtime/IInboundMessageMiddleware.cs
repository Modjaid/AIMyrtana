using Messaging.Abstractions;

namespace Messaging.Runtime;

public interface IInboundMessageMiddleware
{
    Task InvokeAsync(
        InboundMessage message,
        Func<InboundMessage, Task> next,
        CancellationToken cancellationToken = default);
}
