using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Engine;

public class Networking
{
    private readonly TcpListener _listener;
    public IPAddress LocalIp { get; }

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
}
