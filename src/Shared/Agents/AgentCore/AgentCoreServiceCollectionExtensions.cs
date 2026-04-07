using AgentCore.Abstractions;
using AgentCore.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class AgentCoreServiceCollectionExtensions
{
    public static IServiceCollection AddAgentCoreDefaults(this IServiceCollection services)
    {
        services.TryAddSingleton<IAgentTelemetry, NullAgentTelemetry>();
        services.TryAddSingleton<IAgentConversationStore, InMemoryAgentConversationStore>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="AgentRunner"/>; product must register <see cref="IAgentPolicy"/> and <see cref="IProjectFlow"/>.
    /// </summary>
    public static IServiceCollection AddAgentRunner(this IServiceCollection services)
    {
        services.AddSingleton<AgentCore.AgentRunner>();
        return services;
    }
}
