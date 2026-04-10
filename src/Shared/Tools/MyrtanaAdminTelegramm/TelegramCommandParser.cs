using System.Diagnostics.CodeAnalysis;

namespace MyrtanaAdminTelegramm;

internal static class TelegramCommandParser
{
    public static bool TryGetCommandName(string text, [NotNullWhen(true)] out string? command)
    {
        command = null;
        var trimmed = text.Trim();
        if (trimmed.Length == 0 || trimmed[0] != '/')
            return false;

        var firstToken = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        var at = firstToken.IndexOf('@', StringComparison.Ordinal);
        if (at > 0)
            firstToken = firstToken[..at];

        command = firstToken;
        return true;
    }
}
