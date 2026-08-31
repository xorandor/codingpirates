using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En ring der udvider sig og forsvinder. Et anslag. Fjerner sig selv naar den er faerdig.
///   context.Add(new Traefring { Position = punkt });
/// </summary>
public class Traefring : Component
{
    public float StartRadius { get; set; } = 8f;
    public float SlutRadius { get; set; } = 70f;
    public float Varighed { get; set; } = 0.35f;
    public Color Farve { get; set; } = new(255, 235, 120, 255);

    private float _tid;

    public override void OnAdded(GameContext context) => context.After(Varighed, () => context.Remove(this));

    public override void Update(GameContext context) => _tid += context.DeltaTime;

    public override void Render()
    {
        float andel = Math.Clamp(_tid / Varighed, 0f, 1f);
        float radius = StartRadius + (SlutRadius - StartRadius) * andel;
        var farve = Farve with { A = (byte)(255 * (1f - andel)) };

        Draw.Circle(Position, radius, farve);
        Draw.Circle(Position, radius * 0.7f, farve with { A = (byte)(farve.A / 2) });
    }
}
