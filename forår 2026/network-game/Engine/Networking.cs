using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Engine;

public class Networking
{
    private readonly TcpListener _listener;
    private TcpClient? _client;

    public IPAddress LocalIp { get; }
    public string? ConnectedIp { get; private set; }
    public bool IsConnected => _client?.Connected ?? false;

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
                client.Close();
            }
            catch
            {
                break;
            }
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
}
