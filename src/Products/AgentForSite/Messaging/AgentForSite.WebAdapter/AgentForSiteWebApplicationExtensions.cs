using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebSites.Abstractions;
using WebSites.Runtime;

namespace AgentForSite.WebAdapter;

public static class AgentForSiteWebApplicationExtensions
{
    public static WebApplication MapAgentForSiteWebAdapter(this WebApplication app)
    {
        app.MapPost(
            "/api/site/lead",
            async Task<IResult> (
                LeadSubmissionCoordinator coordinator,
                HttpContext http,
                LeadSubmitRequest body,
                CancellationToken cancellationToken) =>
            {
                var ip = http.Connection.RemoteIpAddress?.ToString();
                var submission = new LeadFormSubmission(
                    body.FormId,
                    body.SourcePage,
                    body.Fields,
                    ip,
                    DateTimeOffset.UtcNow);

                var result = await coordinator.ProcessAsync(submission, cancellationToken).ConfigureAwait(false);
                return result.Accepted
                    ? Results.Ok()
                    : Results.BadRequest(new { error = result.Error });
            });

        return app;
    }

    private sealed record LeadSubmitRequest(string FormId, string? SourcePage, IReadOnlyList<LeadFormField> Fields);
}
