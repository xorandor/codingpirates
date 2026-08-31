using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// En moent der ligger i verden og kan samles op.
///
///   game.Add(new Coin { Position = new(120, 80, 0) });
///   game.Add(new Coin { Position = new(-200, 0, 0), Radius = 25, Value = 50, Sprite = "coin.png" });
///
/// Moenten ved ingenting om hvem der samler den op. Den siger bare "jeg kan samles op"
/// (ICollectable), og saa kan alle bruge den - ogsaa dine egne komponenter i MyGames.
/// </summary>
public class Coin : Component, ICollectable
{
    /// <summary>Hvor stor moenten er, og hvor taet man skal vaere for at samle den op.</summary>
    public float Radius { get; set; } = 15f;

    public Color Color { get; set; } = Color.Gold;

    /// <summary>Hvor mange point moenten er vaerd.</summary>
    public int Value { get; set; } = 1;

    /// <summary>Hvilket tal i context.State pointene laegges til.</summary>
    public string ScoreKey { get; set; } = "score";

    /// <summary>Valgfrit filnavn paa et billede, fx "coin.png".</summary>
    public string? Sprite { get; set; }

    /// <summary>Hvor mange world units moenten svaever op og ned. 0 slaar det fra.</summary>
    public float BobHeight { get; set; } = 6f;

    public override string? NetworkKind => "moent";

    private bool _taken;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Circle(Radius);
        Tags.Add("moent");
    }

    public void OnCollected(Component collector, GameContext context)
    {
        if (_taken) return; // to spillere kan naa den i den samme frame
        _taken = true;

        context.State.Add(ScoreKey, Value);
        context.Publish(new Collected(collector, this, Value));
        context.Remove(this);
    }

    public override void Render()
    {
        // Svaevet ligger KUN i tegningen, ikke i Position. Ellers ville moenten kaempe med
        // alt andet der vil flytte den - fx magneten - og vinde hver eneste frame.
        // GetTime() betyder ogsaa at den svaever paa en klient, hvor Update ikke koerer.
        var drawn = Position + new Vector3(0, MathF.Sin((float)GetTime() * 3f + Position.X) * BobHeight, 0);

        if (Sprite != null)
        {
            Draw.Sprite(Sprite, drawn, Radius * 2f);
            return;
        }

        Draw.Ball(drawn, Radius, Color);
        Draw.Ball(drawn + new Vector3(-Radius * 0.25f, Radius * 0.25f, Radius * 0.6f), Radius * 0.35f, Color.Yellow);
    }

    public override void WriteState(StateWriter state)
    {
        state.Number(Radius);
        state.Colour(Color);
        state.Text(Sprite);
    }

    public override void ReadState(StateReader state)
    {
        Radius = state.Number();
        Color = state.Colour();
        string sprite = state.Text();
        Sprite = sprite.Length > 0 ? sprite : null;
    }
}
