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
                HandleMessage(line, client);
        }
        catch { }
        finally
        {
            _players.TryRemove(client, out _);
            client.Close();
        }
    }

    private void HandleMessage(string line, TcpClient client)
    {
        var parts = line.Split(';');
        switch (parts[0])
        {
            case "JOINED":
                if (parts.Length >= 2)
                    _players[client] = parts[1];
                break;
            // Unknown commands are ignored per protocol
        }
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
}
