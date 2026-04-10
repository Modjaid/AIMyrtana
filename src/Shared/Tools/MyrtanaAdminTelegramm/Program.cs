using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyrtanaAdminTelegramm;

Console.OutputEncoding = Encoding.UTF8;

var botToken = Environment.GetEnvironmentVariable("MYRTANA_ADMIN_TELEGRAM_BOT_TOKEN")
    ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrWhiteSpace(botToken))
{
    Console.Error.WriteLine(
        "Set MYRTANA_ADMIN_TELEGRAM_BOT_TOKEN or TELEGRAM_BOT_TOKEN to the bot token from BotFather.");
    return 1;
}

var adminsRaw = Environment.GetEnvironmentVariable("Myrtana_Admins");
var adminUserIds = ParseAdminUserIds(adminsRaw);
if (adminUserIds.Length == 0)
{
    Console.Error.WriteLine(
        "Set Myrtana_Admins to a comma-separated list of Telegram user IDs (numeric), e.g. 123456789,987654321.");
    return 1;
}

var servicesJson = Environment.GetEnvironmentVariable("MYRTANA_SERVICES_JSON");
if (string.IsNullOrWhiteSpace(servicesJson))
    servicesJson = Path.Combine(AppContext.BaseDirectory, "services.json");

var options = new BotOptions
{
    BotToken = botToken.Trim(),
    ServicesJsonPath = servicesJson.Trim(),
    AdminUserIds = adminUserIds,
};

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    o.SingleLine = true;
});

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<IAdminTelegramCommandHandler, ServicesAdminCommandHandler>();
builder.Services.AddSingleton<ServicePowerCommandHandler>();
builder.Services.AddSingleton<TelegramAdminCommandRouter>();
builder.Services.AddHostedService<TelegramAdminBotWorker>();

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
return 0;

static long[] ParseAdminUserIds(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return [];

    return raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(s => long.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : (long?)null)
        .Where(id => id.HasValue)
        .Select(id => id!.Value)
        .Distinct()
        .ToArray();
}
