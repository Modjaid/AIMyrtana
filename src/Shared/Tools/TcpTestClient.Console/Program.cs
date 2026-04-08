using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length >= 2)
    return await RunCliAsync(args).ConfigureAwait(false);

if (args.Length == 1)
{
    Console.Error.WriteLine("Usage: TcpTestClient.Console <host> <port> [message]");
    Console.Error.WriteLine("Example: TcpTestClient.Console 127.0.0.1 5000 \"PING\"");
    Console.Error.WriteLine("Запуск без аргументов — выбор сервиса из tcp-targets.json.");
    return 1;
}

return await RunInteractiveAsync().ConfigureAwait(false);

static async Task<int> RunCliAsync(string[] args)
{
    var host = args[0];
    if (!int.TryParse(args[1], out var port) || port is < 1 or > 65535)
    {
        Console.Error.WriteLine("Invalid port.");
        return 1;
    }

    var message = args.Length > 2 ? string.Join(" ", args.Skip(2)) : "PING";
    return await SendAndReadResponseAsync(host, port, message).ConfigureAwait(false);
}

static async Task<int> RunInteractiveAsync()
{
    var configPath = Path.Combine(AppContext.BaseDirectory, "tcp-targets.json");
    if (!File.Exists(configPath))
    {
        Console.Error.WriteLine($"Файл не найден: {configPath}");
        Console.Error.WriteLine("Создайте tcp-targets.json рядом с exe или скопируйте из проекта.");
        return 1;
    }

    TcpTargetsFile? file;
    try
    {
        await using var stream = File.OpenRead(configPath);
        file = await JsonSerializer.DeserializeAsync(stream, TcpJsonContext.Default.TcpTargetsFile).ConfigureAwait(false);
    }
    catch (JsonException ex)
    {
        Console.Error.WriteLine($"Ошибка разбора JSON: {ex.Message}");
        return 1;
    }

    var targets = file?.Targets ?? [];
    if (targets.Count == 0)
    {
        Console.Error.WriteLine("В tcp-targets.json нет ни одного сервиса (targets).");
        return 1;
    }

    Console.WriteLine("Куда отправить тестовое сообщение?");
    for (var i = 0; i < targets.Count; i++)
    {
        var t = targets[i];
        Console.WriteLine($"  {i + 1}. {t.Name} ({t.Host}:{t.Port})");
    }

    int index;
    while (true)
    {
        Console.Write("Номер: ");
        var line = Console.ReadLine();
        if (line is null)
            return 1;
        if (int.TryParse(line.Trim(), out var n) && n >= 1 && n <= targets.Count)
        {
            index = n - 1;
            break;
        }

        Console.WriteLine($"Введите число от 1 до {targets.Count}.");
    }

    var target = targets[index];
    Console.Write("Сообщение [PING]: ");
    var msgLine = Console.ReadLine();
    var message = string.IsNullOrWhiteSpace(msgLine) ? "PING" : msgLine.Trim();

    Console.WriteLine($"Подключение к {target.Host}:{target.Port}…");
    return await SendAndReadResponseAsync(target.Host, target.Port, message).ConfigureAwait(false);
}

static async Task<int> SendAndReadResponseAsync(string host, int port, string message)
{
    if (!message.EndsWith('\n'))
        message += Environment.NewLine;

    try
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port).ConfigureAwait(false);
        await using var stream = client.GetStream();
        var bytes = Encoding.UTF8.GetBytes(message);
        await stream.WriteAsync(bytes).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);

        var buffer = new byte[4096];
        stream.ReadTimeout = 5000;
        try
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (read > 0)
                Console.Write(Encoding.UTF8.GetString(buffer, 0, read));
        }
        catch (IOException)
        {
            // Server may close without response
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}

internal sealed class TcpTargetsFile
{
    [JsonPropertyName("targets")]
    public List<TcpTarget> Targets { get; set; } = [];
}

internal sealed class TcpTarget
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("host")]
    public string Host { get; set; } = "127.0.0.1";

    [JsonPropertyName("port")]
    public int Port { get; set; }
}

[JsonSerializable(typeof(TcpTargetsFile))]
internal partial class TcpJsonContext : JsonSerializerContext;
