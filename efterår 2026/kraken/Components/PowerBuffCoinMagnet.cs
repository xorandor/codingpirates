using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// En magnet man kan samle op. Bagefter bliver monter trukket hen mod en i et stykke tid.
///
///   game.Add(new PowerBuffCoinMagnet { Position = new(-150, 100, 0), Duration = 8f });
///
/// Naar den samles op, fjerner den sig selv og saetter en CoinMagnetEffect paa den der
/// tog den. Selve effekten er altsaa ogsaa bare en komponent - det er saadan man laver
/// noget der varer et stykke tid, uden at rode i den komponent det gaar ud over.
/// </summary>
public class PowerBuffCoinMagnet : Component, ICollectable
{
    public float Radius { get; set; } = 20f;

    /// <summary>Hvor mange sekunder magneten virker.</summary>
    public float Duration { get; set; } = 5f;

    /// <summary>Hvor langt vaek magneten kan naa monterne.</summary>
    public float PullRadius { get; set; } = 250f;

    /// <summary>Hvor hurtigt monterne bliver trukket ind.</summary>
    public float PullSpeed { get; set; } = 400f;

    public int Value => 0;

    public override string? NetworkKind => "magnet";

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

        context.Add(new CoinMagnetEffect
        {
            Owner = collector,
            SecondsLeft = Duration,
            PullRadius = PullRadius,
            PullSpeed = PullSpeed
        });

        context.Remove(this);
    }

    public override void Render() => DrawMagnet(Position, Radius);

    /// <summary>Tegner en hesteskomagnet. Bruges ogsaa af effekten mens den er aktiv.</summary>
    public static void DrawMagnet(Vector3 centre, float size)
    {
        float armWidth = size * 0.45f;
        float armHeight = size * 1.1f;
        float span = size * 0.75f;

        Draw.Rectangle(centre + new Vector3(0, size * 0.6f, 0), new Vector2(size * 2f, armWidth), Color.Red);
        Draw.Rectangle(centre + new Vector3(-span, 0, 0), new Vector2(armWidth, armHeight), Color.Red);
        Draw.Rectangle(centre + new Vector3(span, 0, 0), new Vector2(armWidth, armHeight), Color.Red);
        Draw.Rectangle(centre + new Vector3(-span, -armHeight * 0.65f, 0), new Vector2(armWidth, size * 0.35f), Color.LightGray);
        Draw.Rectangle(centre + new Vector3(span, -armHeight * 0.65f, 0), new Vector2(armWidth, size * 0.35f), Color.LightGray);
    }
}

/// <summary>
/// Selve magnet-effekten. Foelger den der samlede magneten op, traekker alt med
/// maerkatet "moent" hen mod den, og fjerner sig selv naar tiden er gaaet.
/// </summary>
public class CoinMagnetEffect : Component
{
    /// <summary>Den komponent effekten sidder paa.</summary>
    public Component? Owner { get; set; }

    public float SecondsLeft { get; set; } = 5f;
    public float PullRadius { get; set; } = 250f;
    public float PullSpeed { get; set; } = 400f;

    public override void Update(GameContext context)
    {
        // Er den man sad paa forsvundet ud af spillet, forsvinder effekten med.
        if (Owner is not { IsInGame: true })
        {
            context.Remove(this);
            return;
        }

        Position = Owner.Position;
        SecondsLeft -= context.DeltaTime;

        if (SecondsLeft <= 0f)
        {
            context.Remove(this);
            return;
        }

        float step = PullSpeed * context.DeltaTime;

        foreach (var coin in context.FindByTag("moent"))
        {
            var toOwner = Position - coin.Position;
            float distance = toOwner.Length();

            if (distance > PullRadius || distance < 0.001f) continue;

            coin.Position += toOwner / distance * MathF.Min(step, distance);
        }
    }

    public override void Render()
    {
        if (Owner == null) return;

        PowerBuffCoinMagnet.DrawMagnet(Position + new Vector3(0, 40, 0), 10f);
    }
}
