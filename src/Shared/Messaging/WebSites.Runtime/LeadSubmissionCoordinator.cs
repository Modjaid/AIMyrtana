using Microsoft.Extensions.Logging;
using WebSites.Abstractions;

namespace WebSites.Runtime;

public sealed class LeadSubmissionCoordinator
{
    private readonly IEnumerable<ILeadAntiSpamRule> _rules;
    private readonly ILeadSubmissionHandler _handler;
    private readonly InMemorySubmissionRateGate? _rateGate;
    private readonly ILogger<LeadSubmissionCoordinator> _logger;

    public LeadSubmissionCoordinator(
        IEnumerable<ILeadAntiSpamRule> rules,
        ILeadSubmissionHandler handler,
        ILogger<LeadSubmissionCoordinator> logger,
        InMemorySubmissionRateGate? rateGate = null)
    {
        _rules = rules;
        _handler = handler;
        _rateGate = rateGate;
        _logger = logger;
    }

    public async Task<LeadHandleResult> ProcessAsync(
        LeadFormSubmission submission,
        CancellationToken cancellationToken = default)
    {
        if (_rateGate is not null)
        {
            var key = $"{submission.ClientIp ?? "unknown"}:{submission.FormId}";
            if (!_rateGate.TryAllow(key, submission.SubmittedAt))
                return new LeadHandleResult(false, "Too many submissions. Try again later.");
        }

        foreach (var rule in _rules)
        {
            var reason = await rule.EvaluateAsync(submission, cancellationToken).ConfigureAwait(false);
            if (reason is not null)
            {
                _logger.LogInformation("Lead rejected by anti-spam: {Reason}", reason);
                return new LeadHandleResult(false, reason);
            }
        }

        return await _handler.HandleAsync(submission, cancellationToken).ConfigureAwait(false);
    }
}
