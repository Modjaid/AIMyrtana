using AgentForSite.WebAdapter;
using AgentForSite.AgentImplementations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAgentForSiteStack();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", product = "AgentForSite" }));
app.MapAgentForSiteWebAdapter();

app.MapPost(
    "/api/chat",
    async Task<IResult> (ChatRequest body, IOpenAiAgentClient openAi, CancellationToken cancellationToken) =>
    {
        var message = body.Messages?.LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content;
        if (string.IsNullOrWhiteSpace(message))
            return Results.BadRequest(new { error = "Missing user message." });

        try
        {
            var reply = await openAi.GetReplyAsync(message, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new { reply });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    });

app.Run();

internal sealed record ChatRequest(IReadOnlyList<ChatMessage> Messages);
internal sealed record ChatMessage(string Role, string Content);
