using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyOwnDb;

public sealed class MyOwnDbContextFactory : IDesignTimeDbContextFactory<MyOwnDbContext>
{
    public MyOwnDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MYOWNDB_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=myowndb;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MyOwnDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(AssemblyMarker).Assembly.FullName))
            .Options;

        return new MyOwnDbContext(options);
    }
}

