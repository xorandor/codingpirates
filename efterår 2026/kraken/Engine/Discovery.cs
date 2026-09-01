using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Kraken;

/// <summary>
/// Saadan finder klienter et spil uden at nogen skal taste en IP-adresse.
///
/// En klient raaber DISCOVER ud paa netvaerket over UDP (port 12346). Enhver server der
/// hoerer det, svarer direkte tilbage med GAME og spillets navn, vaertens maskine, antal
/// spillere og om der er kode paa. Klienten laeser serverens adresse af selve svaret -
/// den vises aldrig for nogen. Alt efter det foregaar over TCP som altid.
///
/// UDP-pakker der ikke forstaas, ignoreres - samme regel som paa TCP.
/// </summary>
internal class Discovery
{
    /// <summary>UDP-porten der soeges paa. TCP-spillet ligger paa porten ved siden af (12345).</summary>
    public const int Port = 12346;

    private UdpClient? _udp;

    /// <summary>
    /// Startes paa serveren. Svarer paa DISCOVER med GAME. Tallene skal vaere levende
    /// (antal spillere aendrer sig), saa den faar smaa funktioner i stedet for faste vaerdier.
    /// </summary>
    internal void StartResponder(Func<string> title, Func<int> playerCount, Func<bool> locked)
    {
        try
        {
            _udp = new UdpClient();
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
        }
        catch
        {
            // Porten er optaget (nok en anden server paa maskinen). Spillet skal stadig koere -
            // det kan bare ikke findes automatisk.
            Console.WriteLine("Kraken: kunne ikke aabne soege-porten (UDP 12346). Spillet koerer, men kan ikke findes i listen.");
            _udp = null;
            return;
        }

        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    var pakke = await _udp.ReceiveAsync();
                    if (Encoding.UTF8.GetString(pakke.Buffer).Trim() != "DISCOVER") continue;

                    string svar = Networking.Format("GAME",
                    [
                        title(),
                        Environment.MachineName,
                        Networking.Port.ToString(),
                        playerCount().ToString(),
                        locked() ? "1" : "0"
                    ]);

                    byte[] data = Encoding.UTF8.GetBytes(svar);
                    await _udp.SendAsync(data, data.Length, pakke.RemoteEndPoint);
                }
                catch (ObjectDisposedException) { break; }
                catch { /* daarlig pakke eller netvaerksbump - videre */ }
            }
        });
    }

    internal void Stop()
    {
        try { _udp?.Close(); } catch { /* lukket i forvejen */ }
    }
}

/// <summary>Et spil fundet paa netvaerket. Ip'en bruges kun til at forbinde - den vises aldrig.</summary>
internal record FoundGame(string Ip, string Title, string Host, int TcpPort, int Players, bool Locked);

/// <summary>
/// Klientens soegning: raaber DISCOVER hvert andet sekund og samler GAME-svarene i en liste.
/// Svarer den samme server igen, opdateres dens raekke i stedet for at der kommer en ny.
/// </summary>
internal class DiscoveryClient : IDisposable
{
    private readonly UdpClient? _udp;
    private readonly Dictionary<string, FoundGame> _fundet = [];
    private double _sidstSpurgt = double.NegativeInfinity;

    public DiscoveryClient()
    {
        try
        {
            _udp = new UdpClient(0) { EnableBroadcast = true };
            _ = Task.Run(LytEfterSvar);
        }
        catch
        {
            Console.WriteLine("Kraken: kunne ikke aabne en UDP-port - kan ikke soege efter spil.");
            _udp = null;
        }
    }

    /// <summary>De spil der er fundet indtil nu, sorteret efter navn.</summary>
    public IReadOnlyList<FoundGame> Games
    {
        get { lock (_fundet) return [.. _fundet.Values.OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase)]; }
    }

    /// <summary>Kaldes hver frame. Sender DISCOVER hvert andet sekund.</summary>
    public void Tick(double nu)
    {
        if (_udp == null || nu - _sidstSpurgt < 2.0) return;
        _sidstSpurgt = nu;

        byte[] data = Encoding.UTF8.GetBytes("DISCOVER");

        // Baade broadcast og loopback: broadcast naar ikke altid sikkert tilbage til ens egen
        // maskine paa Windows, og server + klient paa een maskine skal virke.
        try { _udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, Discovery.Port)); } catch { }
        try { _udp.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, Discovery.Port)); } catch { }
    }

    private async Task LytEfterSvar()
    {
        while (true)
        {
            try
            {
                var svar = await _udp!.ReceiveAsync();
                var felter = Encoding.UTF8.GetString(svar.Buffer).Trim().Split(';');
                if (felter.Length < 6 || felter[0] != "GAME") continue;

                var spil = new FoundGame(
                    svar.RemoteEndPoint.Address.ToString(),
                    felter[1],
                    felter[2],
                    int.TryParse(felter[3], out int port) ? port : Networking.Port,
                    int.TryParse(felter[4], out int antal) ? antal : 0,
                    felter[5] == "1");

                // Den samme server svarer baade paa broadcast og paa loopback-kopien, saa den
                // ville staa to gange hvis afsender-adressen var noeglen. Vaert + port ER
                // serveren. Har vi allerede loopback-adressen, beholdes den - den virker
                // altid naar server og klient er paa samme maskine.
                string noegle = $"{spil.Host}:{spil.TcpPort}";
                lock (_fundet)
                {
                    if (_fundet.TryGetValue(noegle, out var kendt) && kendt.Ip == "127.0.0.1")
                        spil = spil with { Ip = kendt.Ip };
                    _fundet[noegle] = spil;
                }
            }
            catch (ObjectDisposedException) { break; }
            catch { /* daarlig pakke - videre */ }
        }
    }

    public void Dispose()
    {
        try { _udp?.Close(); } catch { /* lukket i forvejen */ }
    }
}
