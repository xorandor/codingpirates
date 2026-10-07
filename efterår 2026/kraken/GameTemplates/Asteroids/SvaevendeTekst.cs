using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En kort tekst der stiger op og forsvinder - "FLERE SKUD!", "NIVEAU 3".
///   context.Add(new SvaevendeTekst { Position = punkt, Tekst = "+1 LIV!" });
/// </summary>
public class SvaevendeTekst : Component
{
    public string Tekst { get; set; } = "";
    public Color Farve { get; set; } = Color.White;
    public float Varighed { get; set; } = 1.1f;
    public float StigningsHastighed { get; set; } = 55f;
    public int FontSize { get; set; } = 22;

    private float _tid;

    public override void OnAdded(GameContext context) => context.After(Varighed, () => context.Remove(this));

    public override void Update(GameContext context)
    {
        _tid += context.DeltaTime;
        Position += new Vector3(0, StigningsHastighed * context.DeltaTime, 0);
    }

    public override void RenderUI()
    {
        float andel = Math.Clamp(_tid / Varighed, 0f, 1f);
        var farve = Farve with { A = (byte)(255 * (1f - andel)) };
        Draw.TextAbove(Tekst, Position, 0, FontSize, farve);
    }
}
