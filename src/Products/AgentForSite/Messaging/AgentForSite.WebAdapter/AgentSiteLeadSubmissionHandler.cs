using AgentCore;
using AgentCore.Abstractions;
using Microsoft.Extensions.Logging;
using WebSites.Abstractions;

namespace AgentForSite.WebAdapter;

/// <summary>
/// Связка входящего лида с сайта с оркестратором <see cref="AgentRunner"/>.
/// </summary>
public sealed class AgentSiteLeadSubmissionHandler(
    AgentRunner agentRunner,
    ILogger<AgentSiteLeadSubmissionHandler> logger) : ILeadSubmissionHandler
{
    public async Task<LeadHandleResult> HandleAsync(
        LeadFormSubmission submission,
        CancellationToken cancellationToken = default)
    {
        var text = string.Join(
            "; ",
            submission.Fields.Select(f => $"{f.Name}={f.Value}"));

        var context = new AgentExecutionContext
        {
            ConversationId = $"{submission.FormId}:{submission.SubmittedAt:O}",
            UserMessage = text,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["formId"] = submission.FormId,
                ["sourcePage"] = submission.SourcePage ?? "",
            },
        };

        var run = await agentRunner.RunAsync(context, cancellationToken).ConfigureAwait(false);
        if (!run.Ok)
        {
            logger.LogWarning("Agent run rejected or failed for form {FormId}: {Error}", submission.FormId, run.Error);
            return new LeadHandleResult(false, run.Error);
        }

        return new LeadHandleResult(true);
    }
}
