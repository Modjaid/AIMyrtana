namespace WebSites.Abstractions;

public sealed record LeadFormSubmission(
    string FormId,
    string? SourcePage,
    IReadOnlyList<LeadFormField> Fields,
    string? ClientIp,
    DateTimeOffset SubmittedAt);
