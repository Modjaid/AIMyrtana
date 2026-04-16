using Cronos;

namespace LinguaBot.Scheduler;

internal static class CronHelper
{
    /// <summary>
    /// Returns the next UTC occurrence of a standard 5-field cron expression.
    /// Falls back to 24 hours from now when no next occurrence is computable.
    /// </summary>
    internal static DateTimeOffset GetNextOccurrence(string cronExpression)
    {
        var expression = CronExpression.Parse(cronExpression, CronFormat.Standard);
        return expression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc)
               ?? DateTimeOffset.UtcNow.AddDays(1);
    }
}
