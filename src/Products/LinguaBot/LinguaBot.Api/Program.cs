using LinguaBot.Data;
using Messaging.Abstractions;
using Messaging.Runtime;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LinguaBot")
    ?? throw new InvalidOperationException("ConnectionStrings:LinguaBot is required. Set via env: ConnectionStrings__LinguaBot");

var telegramToken = builder.Configuration["Telegram:BotToken"]
    ?? throw new InvalidOperationException("Telegram:BotToken is required. Set via env: Telegram__BotToken");

var openAiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI:ApiKey is required. Set via env: OpenAI__ApiKey");

var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";

builder.Services.AddLinguaBotStack(connectionString, telegramToken, openAiKey, openAiModel);
builder.Services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(10));

var app = builder.Build();

// Apply EF Core schema on startup (no migration files needed — use MigrateAsync after adding EF migrations).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LinguaBotDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Telegram webhook endpoint — register this URL in Telegram via:
//   POST https://api.telegram.org/bot<TOKEN>/setWebhook?url=https://<your-domain>/telegram/webhook
app.MapPost("/telegram/webhook", async (
    HttpRequest request,
    WebhookDispatcher dispatcher,
    InboundMessagePipeline pipeline) =>
{
    var context = new WebhookContext
    {
        Path = request.Path,
        Body = request.Body,
        Headers = request.Headers
            .Where(h => h.Value.Count > 0)
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase),
        Query = request.Query
            .ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase),
    };

    var result = await dispatcher.DispatchAsync(ChannelKind.Telegram, context);
    if (!result.Handled || result.Messages is null)
        return Results.Ok();

    foreach (var msg in result.Messages)
        await pipeline.RunAsync(msg);

    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", product = "LinguaBot" }));

await app.RunAsync();
Environment.Exit(0);
