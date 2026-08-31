using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>Den stiplede linje i midten. Ren pynt.</summary>
public class MidterLinje : Component
{
    public int Antal { get; set; } = 15;
    public float Bredde { get; set; } = 6f;
    public float BaneHoejde { get; set; } = 720f;
    public Color Farve { get; set; } = new(255, 255, 255, 110);

    public override void Render()
    {
        float trin = BaneHoejde / Antal;
        float stregLaengde = trin * 0.55f;

        for (int i = 0; i < Antal; i++)
        {
            float y = -BaneHoejde / 2f + trin * (i + 0.5f);
            Draw.Rectangle(new Vector3(0, y, -0.1f), new Vector2(Bredde, stregLaengde), Farve);
        }
    }
}
