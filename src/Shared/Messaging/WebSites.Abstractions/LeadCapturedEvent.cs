namespace WebSites.Abstractions;

public sealed record LeadCapturedEvent(
    Guid EventId,
    LeadFormSubmission Submission,
    DateTimeOffset OccurredAt);
