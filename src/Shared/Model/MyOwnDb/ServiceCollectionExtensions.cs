using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MyOwnDb;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyOwnDb(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDb = null)
    {
        services.AddOptions<MyOwnDbOptions>()
            .Bind(configuration.GetSection(MyOwnDbOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                $"{MyOwnDbOptions.SectionName}:{nameof(MyOwnDbOptions.ConnectionString)} is required.")
            .ValidateOnStart();

        services.AddDbContext<MyOwnDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<MyOwnDbOptions>>().Value;

            options.UseNpgsql(
                dbOptions.ConnectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName);
                    npgsql.EnableRetryOnFailure();
                });

            configureDb?.Invoke(options);
        });

        return services;
    }
}

