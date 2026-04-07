namespace WebSites.Abstractions;

public interface ILeadSubmissionHandler
{
    Task<LeadHandleResult> HandleAsync(
        LeadFormSubmission submission,
        CancellationToken cancellationToken = default);
}
