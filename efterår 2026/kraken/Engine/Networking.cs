using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Kraken;

/// <summary>
/// Selve roret mellem maskinerne. Den sender og modtager linjer med tekst, og ved
/// ingenting om spillet. Reglerne for beskederne staar i 'Network protocol.md':
/// UTF-8, een besked pr. linje, felter adskilt af semikolon, kommandoen forrest med STORE BOGSTAVER.
///
/// De fleste komponenter skal ikke bruge denne klasse direkte - saet i stedet NetworkKind
/// paa komponenten og lad motoren om resten. Se Readme.
/// </summary>
public class Networking
{
    public const int Port = 12345;

    /// <summary>Sikkerhedsventil, saa en besked ingen henter ikke kan aede al hukommelsen.</summary>
    private const int MaxQueuedMessages = 20000;

    private readonly TcpListener _listener;
    private TcpClient? _client;
    private readonly ConcurrentDictionary<TcpClient, string> _players = new();
    private readonly List<NetworkMessage> _messageQueue = [];
    private readonly Lock _queueLock = new();

    public IPAddress LocalIp { get; }
    public string? ConnectedIp { get; private set; }
    public bool IsConnected => _client?.Connected ?? false;
    public IReadOnlyList<string> ConnectedPlayers => [.. _players.Values];

    /// <summary>Skriver hver eneste besked ud i konsollen. God at slaa til naar noget driller.</summary>
    public bool LogMessages { get; set; }

    public Networking()
    {
        LocalIp = FindLanIpAddress();
        _listener = new TcpListener(LocalIp, Port);
    }

    private static IPAddress FindLanIpAddress()
    {
        foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (iface.OperationalStatus != OperationalStatus.Up) continue;
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var address in iface.GetIPProperties().UnicastAddresses)
                if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                    return address.Address;
        }

        return IPAddress.Loopback;
    }

    // ------------------------------------------------------------------ Server

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(AcceptConnectionsAsync);
    }

    public void Stop()
    {
        try { _listener.Stop(); } catch { /* lukket i forvejen */ }

        foreach (var client in _players.Keys)
            try { client.Close(); } catch { /* ligegyldigt naar vi lukker ned */ }

        _players.Clear();
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
                break; // listeneren er stoppet
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
        catch { /* klienten faldt af */ }
        finally
        {
            if (_players.TryRemove(client, out string? playerName))
                Enqueue(new NetworkMessage("DISCONNECTED", [playerName], playerName));

            client.Close();
        }
    }

    private void HandleMessageFromClient(string line, TcpClient client)
    {
        if (LogMessages) Console.WriteLine($"<- {line}");

        var parts = line.Split(';');
        if (parts.Length == 0) return;

        if (parts[0] == "JOINED" && parts.Length >= 2)
            _players[client] = parts[1];

        _players.TryGetValue(client, out string? sender);
        Enqueue(new NetworkMessage(parts[0], parts[1..], sender ?? ""));
    }

    public void SendMessageToPlayer(string playerName, string messageType, params string[] fields)
    {
        foreach (var (client, name) in _players)
        {
            if (name != playerName) continue;
            SendTo(client, Format(messageType, fields));
            return;
        }
    }

    public void BroadcastMessageToClients(string messageType, params string[] fields)
    {
        if (_players.IsEmpty) return;

        string message = Format(messageType, fields);
        foreach (var client in _players.Keys)
            SendTo(client, message);
    }

    private void SendTo(TcpClient client, string message)
    {
        try
        {
            if (client.Connected)
                client.GetStream().Write(Encoding.UTF8.GetBytes(message));
        }
        catch { /* klienten faldt af - HandleClientAsync rydder op */ }
    }

    // ------------------------------------------------------------------ Klient

    public bool TryConnect(string ip)
    {
        try
        {
            _client = new TcpClient();
            if (!_client.ConnectAsync(ip, Port).Wait(TimeSpan.FromSeconds(3)))
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

    public void SendJoined(string playerName) => SendMessageToServer("JOINED", playerName);

    public void StartListening()
    {
        if (_client is not { Connected: true }) return;
        _ = Task.Run(ListenToServerAsync);
    }

    private async Task ListenToServerAsync()
    {
        try
        {
            using var reader = new StreamReader(_client!.GetStream(), Encoding.UTF8, leaveOpen: true);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (LogMessages) Console.WriteLine($"<- {line}");

                var parts = line.Split(';');
                if (parts.Length > 0)
                    Enqueue(new NetworkMessage(parts[0], parts[1..], "server"));
            }
        }
        catch { /* forbindelsen doede */ }
    }

    public void SendMessageToServer(string messageType, params string[] fields)
    {
        if (_client is not { Connected: true }) return;

        try
        {
            _client.GetStream().Write(Encoding.UTF8.GetBytes(Format(messageType, fields)));
        }
        catch { /* forbindelsen doede */ }
    }

    // ---------------------------------------------------------------- Beskeder

    private static string Format(string messageType, string[] fields)
        => fields.Length > 0
            ? $"{messageType};{string.Join(';', fields.Select(Sanitise))}\n"
            : $"{messageType}\n";

    /// <summary>Semikolon og linjeskift ville oedelaegge formatet, saa de bliver til underscore.</summary>
    private static string Sanitise(string field)
        => field.Replace(';', '_').Replace('\n', '_').Replace('\r', '_');

    private void Enqueue(NetworkMessage message)
    {
        lock (_queueLock)
        {
            if (_messageQueue.Count >= MaxQueuedMessages)
                _messageQueue.RemoveRange(0, MaxQueuedMessages / 2);

            _messageQueue.Add(message);
        }
    }

    /// <summary>Henter den foerste besked der passer, og fjerner den fra koen. null hvis der ikke er nogen.</summary>
    public NetworkMessage? TryConsumeMessage(string command, Func<NetworkMessage, bool>? match = null)
    {
        lock (_queueLock)
        {
            int index = _messageQueue.FindIndex(m => m.Command == command && (match == null || match(m)));
            if (index < 0) return null;

            var message = _messageQueue[index];
            _messageQueue.RemoveAt(index);
            return message;
        }
    }

    /// <summary>Henter alle beskeder af en type paa een gang, i den raekkefoelge de kom.</summary>
    public List<NetworkMessage> ConsumeAll(string command)
    {
        lock (_queueLock)
        {
            var found = _messageQueue.Where(m => m.Command == command).ToList();
            if (found.Count > 0) _messageQueue.RemoveAll(m => m.Command == command);
            return found;
        }
    }
}

/// <summary>En modtaget besked, delt op i kommando og felter.</summary>
public class NetworkMessage(string command, string[] fields, string sender)
{
    /// <summary>Foerste felt, altid med STORE BOGSTAVER, fx "JOINED".</summary>
    public string Command { get; } = command;

    /// <summary>Resten af felterne.</summary>
    public string[] Fields { get; } = fields;

    /// <summary>Navnet paa den spiller der sendte den. Paa en klient staar der altid "server".</summary>
    public string Sender { get; } = sender;

    public string Field(int index) => index >= 0 && index < Fields.Length ? Fields[index] : "";

    public int Number(int index) => int.TryParse(Field(index), out int value) ? value : 0;
}
