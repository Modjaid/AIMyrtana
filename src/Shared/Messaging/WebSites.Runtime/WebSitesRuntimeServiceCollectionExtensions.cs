using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebSites.Abstractions;
using WebSites.Runtime;

namespace Microsoft.Extensions.DependencyInjection;

public static class WebSitesRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddLeadSubmissionCoordinator(
        this IServiceCollection services,
        int maxSubmissionsPerWindow = 20,
        int windowMinutes = 10)
    {
        services.TryAddSingleton(_ => new InMemorySubmissionRateGate(
            maxSubmissionsPerWindow,
            TimeSpan.FromMinutes(windowMinutes)));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILeadAntiSpamRule, HoneypotAntiSpamRule>());
        services.AddSingleton<LeadSubmissionCoordinator>();
        return services;
    }
}
