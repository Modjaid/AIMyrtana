using AgentCore.Abstractions;
using Messaging.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductA.AgentPolicies;
using ProductA.MessageHandlers;
using ProductA.ProjectFlows;

namespace Microsoft.Extensions.DependencyInjection;

public static class ProductAServiceCollectionExtensions
{
    /// <summary>
    /// Messaging runtime, default inbound sink, and agent runner wiring for ProductA.
    /// </summary>
    public static IServiceCollection AddProductAStack(this IServiceCollection services)
    {
        services.AddMessagingRuntimeCore();
        services.AddInboundMessagePipeline();
        services.AddLoggingInboundMiddleware();
        services.TryAddSingleton<IInboundMessageSink, ProductADefaultInboundSink>();

        services.AddAgentCoreDefaults();
        services.AddAgentRunner();
        services.TryAddSingleton<IAgentPolicy, DefaultProductAPolicy>();
        services.TryAddSingleton<IProjectFlow, DefaultProductAFlow>();

        return services;
    }
}
