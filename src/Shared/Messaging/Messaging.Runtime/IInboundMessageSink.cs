using Messaging.Abstractions;

namespace Messaging.Runtime;

public interface IInboundMessageSink
{
    Task HandleAsync(InboundMessage message, CancellationToken cancellationToken = default);
}
