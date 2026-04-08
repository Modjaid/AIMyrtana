using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

if (args.Length >= 2)
    return await RunCliAsync(args).ConfigureAwait(false);

if (args.Length == 1)
{
    Console.Error.WriteLine("Usage: TcpTestClient.Console <host> <port> [message]");
    Console.Error.WriteLine("Example: TcpTestClient.Console 127.0.0.1 5000 \"PING\"");
    Console.Error.WriteLine("Run without arguments to pick a service from tcp-targets.json.");
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
        Console.Error.WriteLine($"File not found: {configPath}");
        Console.Error.WriteLine("Create tcp-targets.json next to the exe or copy it from the project.");
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
        Console.Error.WriteLine($"Failed to parse JSON: {ex.Message}");
        return 1;
    }

    var targets = file?.Targets ?? [];
    if (targets.Count == 0)
    {
        Console.Error.WriteLine("No services found in tcp-targets.json (targets).");
        return 1;
    }

    Console.WriteLine("Where do you want to send a test message?");
    for (var i = 0; i < targets.Count; i++)
    {
        var t = targets[i];
        Console.WriteLine($"  {i + 1}. {t.Name} ({t.Host}:{t.Port})");
    }

    int index;
    while (true)
    {
        Console.Write("Number: ");
        var line = Console.ReadLine();
        if (line is null)
            return 1;
        if (int.TryParse(line.Trim(), out var n) && n >= 1 && n <= targets.Count)
        {
            index = n - 1;
            break;
        }

        Console.WriteLine($"Enter a number from 1 to {targets.Count}.");
    }

    var target = targets[index];
    Console.WriteLine($"Target: {target.Name} ({target.Host}:{target.Port})");
    Console.WriteLine("Type a message and press Enter. Empty line exits.");
    while (true)
    {
        Console.Write("Message [PING]: ");
        var msgLine = Console.ReadLine();
        if (msgLine is null)
            return 0;
        if (string.IsNullOrWhiteSpace(msgLine))
            break;

        var message = msgLine.Trim();
        Console.WriteLine($"Connecting to {target.Host}:{target.Port}…");
        var code = await SendAndReadResponseAsync(target.Host, target.Port, message).ConfigureAwait(false);
        if (code != 0)
            Console.Error.WriteLine($"Send failed with exit code {code}.");
        Console.WriteLine();
    }

    Console.WriteLine("Press Enter to exit.");
    Console.ReadLine();
    return 0;
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

        Console.WriteLine("Waiting for reply…");
        var buffer = new byte[4096];

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        try
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token).ConfigureAwait(false);
            if (read > 0)
                Console.Write(Encoding.UTF8.GetString(buffer, 0, read));
            else
                Console.WriteLine("(no data)");
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Timed out waiting for reply (120s).");
            return 3;
        }
        catch (IOException)
        {
            // Server may close without response
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
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
