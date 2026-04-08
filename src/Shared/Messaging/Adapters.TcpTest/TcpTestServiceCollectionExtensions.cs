using Adapters.TcpTest;
using Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class TcpTestServiceCollectionExtensions
{
    /// <summary>
    /// Registers TCP line-based test messaging: <see cref="IMessageAdapter"/>, webhook handler, outbound sender,
    /// and by default an inbound listener (same protocol as TcpTestClient.Console).
    /// Requires <see cref="MessagingRuntimeServiceCollectionExtensions.AddInboundMessagePipeline"/> and an <see cref="Messaging.Runtime.IInboundMessageSink"/>.
    /// </summary>
    public static IServiceCollection AddTcpTestMessaging(
        this IServiceCollection services,
        Action<TcpTestMessagingOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<TcpTestMessagingOptions>(_ => { });

        services.TryAddSingleton<TcpTestListenEndpoint>();
        services.TryAddSingleton<TcpTestAdapter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageAdapter>(sp => sp.GetRequiredService<TcpTestAdapter>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IWebhookHandler>(sp => sp.GetRequiredService<TcpTestAdapter>().WebhookHandler));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IOutboundSender>(sp => sp.GetRequiredService<TcpTestAdapter>().OutboundSender));
        services.AddHostedService<TcpTestInboundListenerHostedService>();
        return services;
    }
}
