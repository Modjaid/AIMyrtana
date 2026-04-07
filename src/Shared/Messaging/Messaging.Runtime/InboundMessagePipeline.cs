using Messaging.Abstractions;

namespace Messaging.Runtime;

public sealed class InboundMessagePipeline
{
    private readonly IReadOnlyList<IInboundMessageMiddleware> _middleware;
    private readonly IInboundMessageSink _sink;

    public InboundMessagePipeline(
        IEnumerable<IInboundMessageMiddleware> middleware,
        IInboundMessageSink sink)
    {
        _middleware = middleware.ToList();
        _sink = sink;
    }

    public Task RunAsync(InboundMessage message, CancellationToken cancellationToken = default)
    {
        Func<InboundMessage, Task> next = m => _sink.HandleAsync(m, cancellationToken);
        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var mw = _middleware[i];
            var inner = next;
            next = m => mw.InvokeAsync(m, inner, cancellationToken);
        }

        return next(message);
    }
}
