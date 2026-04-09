using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyrtanaAdminTelegramm;

internal sealed class ServicesConfigFile
{
    [JsonPropertyName("services")]
    public List<ServiceEntry> Services { get; set; } = [];
}

internal sealed class ServiceEntry
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "";
}

internal static class ServicesConfigLoader
{
    public static async Task<ServicesConfigFile?> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync(stream, ServicesJsonContext.Default.ServicesConfigFile, cancellationToken)
            .ConfigureAwait(false);
    }
}

[JsonSerializable(typeof(ServicesConfigFile))]
internal sealed partial class ServicesJsonContext : JsonSerializerContext;
