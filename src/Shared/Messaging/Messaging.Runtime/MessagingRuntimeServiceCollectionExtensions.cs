using Messaging.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingRuntimeCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessagingMetrics, NullMessagingMetrics>();
        services.TryAddSingleton<WebhookDispatcher>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="InboundMessagePipeline"/>; product must register <see cref="IInboundMessageSink"/>
    /// and any <see cref="IInboundMessageMiddleware"/> implementations.
    /// </summary>
    public static IServiceCollection AddInboundMessagePipeline(this IServiceCollection services)
    {
        services.AddSingleton<InboundMessagePipeline>();
        return services;
    }

    public static IServiceCollection AddLoggingInboundMiddleware(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IInboundMessageMiddleware, LoggingInboundMiddleware>());
        return services;
    }
}
