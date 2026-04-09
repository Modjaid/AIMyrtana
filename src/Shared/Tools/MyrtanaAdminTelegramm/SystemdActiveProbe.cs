using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MyrtanaAdminTelegramm;

/// <summary>
/// Uses <c>systemctl is-active --quiet</c> (exit 0 = active).
/// </summary>
internal static partial class SystemdActiveProbe
{
    private const string SystemctlPath = "/usr/bin/systemctl";

    [GeneratedRegex(@"^[a-zA-Z0-9@._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeUnitNameRegex();

    public static bool IsSafeUnitName(string unit) =>
        unit.Length is > 0 and <= 256 && SafeUnitNameRegex().IsMatch(unit);

    public static async Task<bool?> IsActiveAsync(string unit, CancellationToken cancellationToken)
    {
        if (!IsSafeUnitName(unit))
            return null;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SystemctlPath,
                    ArgumentList = { "is-active", "--quiet", unit },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            if (!process.Start())
                return null;

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
