using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En markoer til hver spiller - ogsaa vaerten. Koerer kun paa serveren: naar en spiller
/// kommer med, faar de en markoer i en tilfaeldig farve, og naar de gaar, forsvinder den.
/// </summary>
public class MarkoerPerSpiller : Component
{
    /// <summary>Navnet paa vaertens markoer hvis vaerten ikke selv har tastet et navn.</summary>
    public string VaertNavn { get; set; } = "Vaert";

    public override bool Persistent => true;

    private readonly Dictionary<string, Markoer> _markoerer = new(StringComparer.OrdinalIgnoreCase);

    public override void OnAdded(GameContext context)
    {
        if (!context.IsServer) return;

        context.On<PlayerJoined>(e => Lav(context, e.PlayerName, fjernstyret: true));
        context.On<PlayerLeft>(e =>
        {
            if (_markoerer.Remove(e.PlayerName, out var markoer)) context.Remove(markoer);
        });

        Lav(context, context.MyName.Length > 0 ? context.MyName : VaertNavn, fjernstyret: false);
    }

    private void Lav(GameContext context, string navn, bool fjernstyret)
    {
        if (_markoerer.ContainsKey(navn)) return;

        var markoer = new Markoer
        {
            Navn = navn,
            ControlledBy = fjernstyret ? navn : null,
            Farve = new Color(
                (byte)Random.Shared.Next(70, 240),
                (byte)Random.Shared.Next(70, 240),
                (byte)Random.Shared.Next(70, 240),
                (byte)255),
            Position = new Vector3(Random.Shared.Next(-350, 350), Random.Shared.Next(-250, 250), 0)
        };

        _markoerer[navn] = context.Add(markoer);

        // Fortael spilleren at den her er DERES - saa kan deres kamera osv. finde den.
        if (fjernstyret) context.ClaimForPlayer(navn, markoer);
    }
}
