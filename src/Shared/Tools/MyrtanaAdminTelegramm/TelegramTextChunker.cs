namespace MyrtanaAdminTelegramm;

internal static class TelegramTextChunker
{
    public const int MaxMessageLength = 4096;

    public static IEnumerable<string> Chunk(string text, int maxLen = MaxMessageLength)
    {
        if (text.Length <= maxLen)
        {
            yield return text;
            yield break;
        }

        var start = 0;
        while (start < text.Length)
        {
            var remaining = text.Length - start;
            var take = Math.Min(maxLen, remaining);
            if (take < remaining)
            {
                var slice = text.AsSpan(start, take);
                var lastNl = slice.LastIndexOf('\n');
                if (lastNl > maxLen / 2)
                    take = lastNl + 1;
            }

            yield return text.Substring(start, take);
            start += take;
        }
    }
}
