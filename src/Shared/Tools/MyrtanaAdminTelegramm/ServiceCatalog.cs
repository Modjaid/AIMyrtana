using System.Text;

namespace MyrtanaAdminTelegramm;

internal static class ServiceCatalog
{
    /// <summary>
    /// Имя для команд вида /Stop… /Activate…: только латинские буквы и цифры (как в Telegram после /).
    /// </summary>
    public static string ToCommandSlug(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9')
                sb.Append(c);
        }

        return sb.ToString();
    }

    public static string DisplayTitle(ServiceEntry s)
    {
        var title = s.Title.Trim();
        var unit = s.Unit.Trim();
        return string.IsNullOrWhiteSpace(title) ? unit : title;
    }

    public static ServiceEntry? FindByKey(IReadOnlyList<ServiceEntry> entries, string key)
    {
        var k = key.Trim();
        if (k.Length == 0)
            return null;

        var slugKey = ToCommandSlug(k);

        foreach (var e in entries)
        {
            var unit = e.Unit.Trim();
            if (unit.Length == 0)
                continue;

            var title = DisplayTitle(e);

            if (slugKey.Length > 0)
            {
                if (ToCommandSlug(title).Equals(slugKey, StringComparison.OrdinalIgnoreCase))
                    return e;

                if (ToCommandSlug(unit).Equals(slugKey, StringComparison.OrdinalIgnoreCase))
                    return e;
            }

            if (title.Equals(k, StringComparison.OrdinalIgnoreCase))
                return e;

            if (unit.Equals(k, StringComparison.OrdinalIgnoreCase))
                return e;

            if (unit.EndsWith(".service", StringComparison.OrdinalIgnoreCase)
                && unit.Length > ".service".Length)
            {
                var baseName = unit[..^".service".Length];
                if (baseName.Equals(k, StringComparison.OrdinalIgnoreCase))
                    return e;
            }
        }

        return null;
    }

    /// <summary>
    /// Подсказка команды для отчёта; <see langword="null"/> если из названия нельзя собрать slug.
    /// </summary>
    public static string? FormatSuggestedCommand(bool active, string displayTitle)
    {
        var slug = ToCommandSlug(displayTitle);
        if (slug.Length == 0)
            return null;

        return active ? $"/Stop{slug}" : $"/Activate{slug}";
    }
}
