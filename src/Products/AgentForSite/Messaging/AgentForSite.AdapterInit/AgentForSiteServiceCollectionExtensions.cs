using AgentCore.Abstractions;
using AgentForSite.AgentImplementations;
using AgentForSite.AgentPolicies;
using AgentForSite.ProjectFlows;
using AgentForSite.WebAdapter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebSites.Abstractions;
namespace Microsoft.Extensions.DependencyInjection;

public static class AgentForSiteServiceCollectionExtensions
{
    /// <summary>
    /// WebSites.Runtime (приём лидов), <see cref="ILeadSubmissionHandler"/> → оркестратор <see cref="AgentCore.AgentRunner"/>.
    /// </summary>
    public static IServiceCollection AddAgentForSiteStack(this IServiceCollection services)
    {
        services.AddLeadSubmissionCoordinator();
        services.AddAgentCoreDefaults();
        services.AddAgentRunner();
        services.AddHttpClient(nameof(OpenAiAgentClient));
        services.TryAddSingleton<IOpenAiAgentClient, OpenAiAgentClient>();
        services.TryAddSingleton<ILeadSubmissionHandler, AgentSiteLeadSubmissionHandler>();
        services.TryAddSingleton<IAgentPolicy, DefaultAgentForSitePolicy>();
        services.TryAddSingleton<IProjectFlow, DefaultAgentForSiteFlow>();

        return services;
    }
}
