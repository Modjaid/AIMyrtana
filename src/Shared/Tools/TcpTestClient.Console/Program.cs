using System.Net.Sockets;
using System.Text;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: TcpTestClient.Console <host> <port> [message]");
    Console.Error.WriteLine("Example: TcpTestClient.Console 127.0.0.1 5000 \"PING\"");
    return 1;
}

var host = args[0];
if (!int.TryParse(args[1], out var port) || port is < 1 or > 65535)
{
    Console.Error.WriteLine("Invalid port.");
    return 1;
}

var message = args.Length > 2 ? string.Join(" ", args.Skip(2)) : "PING";
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
