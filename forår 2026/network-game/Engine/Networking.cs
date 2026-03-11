using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Engine;

public class Networking
{
    private readonly TcpListener _listener;
    private TcpClient? _client;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<TcpClient, string> _players = new();
    private readonly List<NetworkMessage> _messageQueue = [];
    private readonly object _messageQueueLock = new();

    public IPAddress LocalIp { get; }
    public string? ConnectedIp { get; private set; }
    public bool IsConnected => _client?.Connected ?? false;
    public IReadOnlyList<string> ConnectedPlayers => [.._players.Values];

    public Networking()
    {
        LocalIp = FindLanIpAddress();
        _listener = new TcpListener(LocalIp, 12345);
    }

    private static IPAddress FindLanIpAddress()
    {
        foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (iface.OperationalStatus != OperationalStatus.Up) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var addr in iface.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address;
            }
        }
        return IPAddress.Loopback;
    }

    // Server mode
    public void Start()
    {
        _listener.Start();
        _ = Task.Run(AcceptConnectionsAsync);
    }

    public void Stop()
    {
        _listener.Stop();
    }

    private async Task AcceptConnectionsAsync()
    {
        while (true)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch
            {
                break;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, leaveOpen: true);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
                HandleMessageFromClient(line, client);
        }
        catch { }
        finally
        {
            if (_players.TryRemove(client, out string? playerName))
            {
                lock (_messageQueueLock)
                    _messageQueue.Add(new NetworkMessage("DISCONNECTED", [playerName]));
            }
            client.Close();
        }
    }

    private void HandleMessageFromClient(string line, TcpClient client)
    {
        Console.WriteLine($"NETWORK RECEIVED: {line}");
        var parts = line.Split(';');

        if (parts[0] == "JOINED" && parts.Length >= 2)
            _players[client] = parts[1];

        lock (_messageQueueLock)
            _messageQueue.Add(new NetworkMessage(parts[0], parts[1..]));
    }

    private void SendToClient(TcpClient client, string message)
    {
        try
        {
            if (client.Connected)
                client.GetStream().Write(Encoding.UTF8.GetBytes(message));
        }
        catch { }
    }

    public void SendMessageToPlayer(string playerName, string messageType, params string[] parameters)
    {
        var message = parameters.Length > 0
            ? $"{messageType};{string.Join(';', parameters)}\n"
            : $"{messageType}\n";

        foreach (var (client, name) in _players)
        {
            if (name == playerName)
            {
                SendToClient(client, message);
                return;
            }
        }
    }

    private void BroadcastToClients(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        foreach (var tcpClient in _players.Keys)
        {
            try
            {
                if (tcpClient.Connected)
                    tcpClient.GetStream().Write(bytes);
            }
            catch { }
        }
    }

    public void BroadcastMessageToClients(string messageType, params string[] parameters)
    {
        var message = parameters.Length > 0
            ? $"{messageType};{string.Join(';', parameters)}\n"
            : $"{messageType}\n";
        BroadcastToClients(message);
    }

    // Client mode
    public bool TryConnect(string ip)
    {
        try
        {
            _client = new TcpClient();
            var task = _client.ConnectAsync(ip, 12345);
            if (!task.Wait(TimeSpan.FromSeconds(3)))
            {
                _client.Close();
                _client = null;
                return false;
            }
            ConnectedIp = ip;
            return true;
        }
        catch
        {
            _client?.Close();
            _client = null;
            return false;
        }
    }

    public void SendJoined(string playerName)
    {
        if (_client == null || !_client.Connected) return;
        var bytes = Encoding.UTF8.GetBytes($"JOINED;{playerName}\n");
        _client.GetStream().Write(bytes);
    }

    public void StartListening()
    {
        if (_client == null || !_client.Connected) return;
        _ = Task.Run(ListenFromServerAsync);
    }

    private async Task ListenFromServerAsync()
    {
        try
        {
            using var reader = new StreamReader(_client!.GetStream(), Encoding.UTF8, leaveOpen: true);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(';');
                lock (_messageQueueLock)
                    _messageQueue.Add(new NetworkMessage(parts[0], parts[1..]));
            }
        }
        catch { }
    }

    public NetworkMessage? TryConsumeMessage(string command, Func<NetworkMessage, bool> match)
    {
        lock (_messageQueueLock)
        {
            var index = _messageQueue.FindIndex(m => m.Command == command && match(m));
            if (index < 0) return null;
            var msg = _messageQueue[index];
            _messageQueue.RemoveAt(index);
            return msg;
        }
    }

    public void SendMessageToServer(string messageType, params string[] parameters)
    {
        if (_client == null || !_client.Connected) return;
        var message = parameters.Length > 0
            ? $"{messageType};{string.Join(';', parameters)}\n"
            : $"{messageType}\n";
        var bytes = Encoding.UTF8.GetBytes(message);
        _client.GetStream().Write(bytes);
    }
}

public class NetworkMessage
{
    public string Command { get; }
    public string[] Fields { get; }

    public NetworkMessage(string command, string[] fields)
    {
        Command = command;
        Fields = fields;
    }
}
