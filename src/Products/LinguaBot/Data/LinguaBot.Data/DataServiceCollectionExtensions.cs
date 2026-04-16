using LinguaBot.Data;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class LinguaBotDataServiceCollectionExtensions
{
    public static IServiceCollection AddLinguaBotData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LinguaBotDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IScheduledTaskRepository, ScheduledTaskRepository>();
        return services;
    }
}
