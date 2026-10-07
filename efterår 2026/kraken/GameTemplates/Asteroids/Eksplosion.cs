using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En sky af smaa stumper, der flyver ud fra et punkt og bliver mindre, til de er vaek.
/// Fjerner sig selv naar den er faerdig.
///   context.Add(new Eksplosion { Position = punkt, Farve = Color.Orange });
/// </summary>
public class Eksplosion : Component
{
    public int Antal { get; set; } = 12;
    public float Varighed { get; set; } = 0.7f;
    public float Fart { get; set; } = 220f;
    public float Spredning { get; set; } = 10f;
    public float StumpStoerrelse { get; set; } = 8f;
    public Color Farve { get; set; } = Color.Orange;

    private readonly List<(Vector3 Sted, Vector3 Fart, float Stoerrelse)> _stumper = [];
    private float _tid;

    public override void OnAdded(GameContext context)
    {
        var r = Random.Shared;
        for (int i = 0; i < Antal; i++)
        {
            float vinkel = r.NextSingle() * MathF.PI * 2f;
            float fart = Fart * (0.3f + r.NextSingle() * 0.9f);
            var retning = new Vector3(MathF.Cos(vinkel), MathF.Sin(vinkel), 0);
            _stumper.Add((Position + retning * r.NextSingle() * Spredning, retning * fart, StumpStoerrelse * (0.5f + r.NextSingle())));
        }

        context.After(Varighed, () => context.Remove(this));
    }

    public override void Update(GameContext context)
    {
        _tid += context.DeltaTime;
        for (int i = 0; i < _stumper.Count; i++)
        {
            var (sted, fart, stoerrelse) = _stumper[i];
            _stumper[i] = (sted + fart * context.DeltaTime, fart, stoerrelse);
        }
    }

    public override void Render()
    {
        float tilbage = 1f - Math.Clamp(_tid / Varighed, 0f, 1f);
        foreach (var (sted, _, stoerrelse) in _stumper)
            Draw.Cube(sted, new Vector3(stoerrelse * tilbage), Farve);
    }
}
