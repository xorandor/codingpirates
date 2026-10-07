using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Et skud fra skibet. Flyver lige ud og forsvinder, naar det er ude af skaermen - det
/// kommer IKKE ind paa den anden side. Rammer det en asteroide, forsvinder det; det er
/// asteroiden selv der finder ud af, at den er ramt (den maerker maerkatet "skud").
/// </summary>
public class Skud : Component
{
    public Vector2 Retning { get; set; } = new(0, 1);
    public float Fart { get; set; } = 780f;
    public float Stoerrelse { get; set; } = 8f;
    public Color Farve { get; set; } = Color.Yellow;
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Circle(Stoerrelse / 2f);
        Tags.Add("skud");
    }

    public override void Update(GameContext context)
    {
        Position += new Vector3(Retning.X, Retning.Y, 0) * Fart * context.DeltaTime;

        if (Kant.Udenfor(Position, BaneBredde, BaneHoejde, Stoerrelse))
            context.Remove(this);
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (other.HasTag("asteroide")) context.Remove(this);
    }

    public override void Render()
    {
        var hale = Position - new Vector3(Retning.X, Retning.Y, 0) * Stoerrelse * 3f;
        Draw.Line(hale, Position, Farve with { A = 120 });
        Draw.Cube(Position, new Vector3(Stoerrelse), Farve);
    }
}
