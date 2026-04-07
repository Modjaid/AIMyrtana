namespace WebSites.Abstractions;

public interface ILeadAntiSpamRule
{
    /// <summary>
    /// Returns null if the submission passes; otherwise a rejection reason.
    /// </summary>
    Task<string?> EvaluateAsync(
        LeadFormSubmission submission,
        CancellationToken cancellationToken = default);
}
