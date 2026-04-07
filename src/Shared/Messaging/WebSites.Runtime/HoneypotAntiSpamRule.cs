using WebSites.Abstractions;

namespace WebSites.Runtime;

/// <summary>
/// If the named field is non-empty, treat as bot (honeypot).
/// </summary>
public sealed class HoneypotAntiSpamRule : ILeadAntiSpamRule
{
    private readonly string _fieldName;

    public HoneypotAntiSpamRule(string fieldName = "company_website") =>
        _fieldName = fieldName;

    public Task<string?> EvaluateAsync(
        LeadFormSubmission submission,
        CancellationToken cancellationToken = default)
    {
        var trap = submission.Fields.FirstOrDefault(f =>
            string.Equals(f.Name, _fieldName, StringComparison.OrdinalIgnoreCase));
        if (trap is not null && !string.IsNullOrWhiteSpace(trap.Value))
            return Task.FromResult<string?>($"Honeypot field '{_fieldName}' must be empty");

        return Task.FromResult<string?>(null);
    }
}
