using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Falder ned fra en odelagt klods. Saml den op med battet, saa faar du dens effekt - se
/// PowerUpType i Beskeder.cs. PowerUp kender ikke Bat - kun maerkatet "bat".
/// </summary>
public class PowerUp : Component
{
    public PowerUpType Type { get; set; }
    public float Fart { get; set; } = 160f;
    public float Stoerrelse { get; set; } = 26f;
    public float BaneHoejde { get; set; } = 720f;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Box(Stoerrelse, Stoerrelse);
        Tags.Add("powerup");
        Assets.Tone("*powerup", 500, 1200, 0.12f, firkant: false);
    }

    public override void Update(GameContext context)
    {
        Position += new Vector3(0, -Fart * context.DeltaTime, 0);
        if (Position.Y < -BaneHoejde / 2f - Stoerrelse) context.Remove(this);
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (!other.HasTag("bat")) return;

        Assets.Play("*powerup");
        context.Publish(new PowerUpSamlet(Type, Position, FarveFor(Type)));
        context.Remove(this);
    }

    public override void Render() => Draw.Cube(Position, new Vector3(Stoerrelse, Stoerrelse, Stoerrelse * 0.6f), FarveFor(Type));

    /// <summary>Farven for hver slags - bruges baade her og af PowerUpSamlet, saa teksten kan matche.</summary>
    public static Color FarveFor(PowerUpType type) => type switch
    {
        PowerUpType.UdvidBat => Color.SkyBlue,
        PowerUpType.EkstraBold => Color.Orange,
        PowerUpType.LangsomBold => Color.Green,
        PowerUpType.EkstraLiv => Color.Pink,
        _ => Color.White
    };
}
