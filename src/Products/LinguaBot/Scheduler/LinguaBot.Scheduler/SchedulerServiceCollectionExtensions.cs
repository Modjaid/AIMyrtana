using LinguaBot.Domain;
using LinguaBot.Scheduler;

namespace Microsoft.Extensions.DependencyInjection;

public static class LinguaBotSchedulerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the scheduler stack: <see cref="ISchedulerService"/>, <see cref="TaskMessageBuilder"/>,
    /// and the <see cref="SchedulerWorker"/> background service.
    /// </summary>
    public static IServiceCollection AddLinguaBotScheduler(this IServiceCollection services)
    {
        services.AddSingleton<ISchedulerService, SchedulerService>();
        services.AddSingleton<TaskMessageBuilder>();
        services.AddScoped<ISpacedRepetitionService, SpacedRepetitionService>();
        services.AddHostedService<SchedulerWorker>();
        return services;
    }
}
