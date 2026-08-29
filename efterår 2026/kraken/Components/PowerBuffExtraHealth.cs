using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// Et hjerte man kan samle op for at faa et ekstra liv.
///
///   game.Add(new PowerBuffExtraHealth { Position = new(200, -100, 0) });
///
/// Den ved ikke hvem der samler den op. Den sender bare beskeden Healed ud, og saa
/// er det op til modtageren at gore noget ved det.
/// </summary>
public class PowerBuffExtraHealth : Component, ICollectable
{
    public float Radius { get; set; } = 20f;
    public Color Color { get; set; } = Color.Red;

    /// <summary>Hvor mange liv man faar.</summary>
    public int Lives { get; set; } = 1;

    /// <summary>Buffs giver ikke point, kun liv.</summary>
    public int Value => 0;

    public override string? NetworkKind => "ekstra-liv";

    private bool _taken;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Circle(Radius);
        Tags.Add("buff");
    }

    public void OnCollected(Component collector, GameContext context)
    {
        if (_taken) return;
        _taken = true;

        context.Publish(new Healed(collector, Lives));
        context.Remove(this);
    }

    public override void Render()
    {
        // Pulsen kommer fra uret og ikke fra Update, saa hjertet ogsaa banker paa en klient.
        float pulse = 1f + 0.1f * MathF.Sin((float)GetTime() * 4f);
        DrawHeart(Position, Radius * pulse, Color);
    }

    private static void DrawHeart(Vector3 centre, float size, Color color)
    {
        Draw.Ball(centre + new Vector3(-size * 0.3f, size * 0.25f, 0), size * 0.42f, color);
        Draw.Ball(centre + new Vector3(size * 0.3f, size * 0.25f, 0), size * 0.42f, color);
        Draw.Ball(centre + new Vector3(0, -size * 0.35f, 0), size * 0.45f, color);
        Draw.Rectangle(centre + new Vector3(0, size * 0.1f, 0), new Vector2(size * 1.2f, size * 0.7f), color);
    }
}
