using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// Giver hver spiller der forbinder sig deres egen figur. Koerer kun paa serveren -
/// klienterne faar figurerne tilsendt helt af sig selv.
///
///   game.Add(new NetworkPlayers());
///
///   game.Add(new NetworkPlayers
///   {
///       CreatePlayer = () =&gt; new Player { Speed = 400, Sprite = "helt.png", MaxLives = 5 }
///   });
///
/// Det er her hele netvaerksmodellen bliver konkret: serveren bestemmer, laver figurerne
/// og flytter dem. Klienten sender kun sine tastetryk og tegner det den faar at vide.
/// </summary>
public class NetworkPlayers : Component
{
    /// <summary>Lav ogsaa en figur til den der koerer serveren.</summary>
    public bool IncludeHost { get; set; } = true;

    /// <summary>Navnet paa den der koerer serveren.</summary>
    public string HostName { get; set; } = "Vaert";

    /// <summary>Hvor langt fra midten figurerne kan dukke op.</summary>
    public float SpawnSpread { get; set; } = 250f;

    /// <summary>Giver hver spiller sin egen tilfaeldige farve. Slaa fra hvis du selv vil bestemme.</summary>
    public bool RandomColors { get; set; } = true;

    /// <summary>Lav din egen slags spiller her. Uden den bruges en helt almindelig Player.</summary>
    public Func<Player>? CreatePlayer { get; set; }

    public override bool Persistent => true;

    private readonly Dictionary<string, Player> _players = new(StringComparer.OrdinalIgnoreCase);

    public override void OnAdded(GameContext context)
    {
        if (!context.IsServer) return;

        context.On<PlayerJoined>(joined => Spawn(context, joined.PlayerName, remote: true));
        context.On<PlayerLeft>(left => Despawn(context, left.PlayerName));

        if (IncludeHost) Spawn(context, HostName, remote: false);
    }

    private void Spawn(GameContext context, string playerName, bool remote)
    {
        if (_players.ContainsKey(playerName)) return;

        var player = CreatePlayer?.Invoke() ?? new Player();

        player.Name = playerName;
        player.ControlledBy = remote ? playerName : null;
        player.ShowLives = !remote;   // hjerterne i hjoernet er kun vaertens
        player.EndsGameOnDeath = false;
        player.Position = new Vector3(
            Random.Shared.Next((int)-SpawnSpread, (int)SpawnSpread),
            Random.Shared.Next((int)-SpawnSpread, (int)SpawnSpread),
            0);

        if (RandomColors)
            player.Color = new Color(
                (byte)Random.Shared.Next(60, 240),
                (byte)Random.Shared.Next(60, 240),
                (byte)Random.Shared.Next(60, 240),
                (byte)255);

        _players[playerName] = context.Add(player);

        // Fortael klienten hvilken figur der er deres, saa den kan lade kameraet foelge den.
        if (remote) context.ClaimForPlayer(playerName, player);
    }

    private void Despawn(GameContext context, string playerName)
    {
        if (!_players.Remove(playerName, out var player)) return;

        context.Remove(player);
    }
}
