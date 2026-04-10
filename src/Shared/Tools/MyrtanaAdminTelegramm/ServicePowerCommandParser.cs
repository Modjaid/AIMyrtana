using System.Diagnostics.CodeAnalysis;

namespace MyrtanaAdminTelegramm;

internal static class ServicePowerCommandParser
{
    public static bool TryParse(string text, out bool activate, [NotNullWhen(true)] out string? serviceKey)
    {
        activate = false;
        serviceKey = null;
        var t = text.Trim();
        if (t.Length < 2 || t[0] != '/')
            return false;

        var s = t.AsSpan(1);
        if (s.StartsWith("Stop", StringComparison.OrdinalIgnoreCase))
        {
            activate = false;
            s = s.Slice(4).TrimStart();
        }
        else if (s.StartsWith("Activate", StringComparison.OrdinalIgnoreCase))
        {
            activate = true;
            s = s.Slice(8).TrimStart();
        }
        else
            return false;

        if (s.Length == 0)
            return false;

        serviceKey = s.ToString();
        return true;
    }
}
