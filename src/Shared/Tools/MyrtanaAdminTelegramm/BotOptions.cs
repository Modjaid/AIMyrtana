namespace MyrtanaAdminTelegramm;

internal sealed class BotOptions
{
    public string BotToken { get; init; } = "";
    public string ServicesJsonPath { get; init; } = "";
    public long[] AdminUserIds { get; init; } = [];
}
