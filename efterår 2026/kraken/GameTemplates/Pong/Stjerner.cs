using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En stjernehimmel langt bag banen. Med Camera.Perspective = true er de fjerneste smaa og
/// de naermeste store, og de glider forskelligt naar kameraet flytter sig. Det er dybde man kan se.
/// </summary>
public class Stjerner : Component
{
    public int Antal { get; set; } = 140;
    public float NaermesteZ { get; set; } = -120f;
    public float FjernesteZ { get; set; } = -900f;

    private readonly List<(Vector3 Position, float Stoerrelse, Color Farve)> _stjerner = [];

    public override void OnAdded(GameContext context)
    {
        var r = Random.Shared;
        for (int i = 0; i < Antal; i++)
        {
            float z = NaermesteZ + r.NextSingle() * (FjernesteZ - NaermesteZ);
            float dybde = (z - NaermesteZ) / (FjernesteZ - NaermesteZ);            // 0 naer, 1 fjern
            byte lys = (byte)(220 - dybde * 120);                                  // fjerne er moerkere

            // Spredes bredere jo laengere vaek, saa de fylder synsfeltet ogsaa i perspektiv.
            float spredning = 1f + dybde * 1.5f;
            var position = new Vector3(
                (r.NextSingle() * 2f - 1f) * 700f * spredning,
                (r.NextSingle() * 2f - 1f) * 400f * spredning,
                z);

            _stjerner.Add((position, 3f + r.NextSingle() * 5f, new Color(lys, lys, (byte)Math.Min(255, lys + 25), (byte)255)));
        }
    }

    public override void Render()
    {
        foreach (var (position, stoerrelse, farve) in _stjerner)
            Draw.Cube(position, new Vector3(stoerrelse), farve);
    }
}
