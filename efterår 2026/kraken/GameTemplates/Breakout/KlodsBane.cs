using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Bygger muren af klodser og holder oeje med naar den er ryddet. Publisher KlodserRyddet
/// een gang, saa Slutskaerm kan vise sejr - den kender ikke Slutskaerm, kun beskeden.
/// </summary>
public class KlodsBane : Component
{
    public int Raekker { get; set; } = 6;
    public int Kolonner { get; set; } = 10;
    public float Bredde { get; set; } = 104f;
    public float Hoejde { get; set; } = 32f;
    public float Mellemrum { get; set; } = 8f;
    public float OeverstY { get; set; } = 260f;
    public Color[] Farver { get; set; } = [Color.Red, Color.Orange, Color.Gold, Color.Green, Color.SkyBlue, Color.Purple];
    public int[] PointPrRaekke { get; set; } = [60, 50, 40, 30, 20, 10];

    private bool _harBygget;
    private bool _ryddet;

    public override void OnAdded(GameContext context)
    {
        context.On<GameStarted>(_ => Byg(context));
        Byg(context);
    }

    public override void Update(GameContext context)
    {
        if (_ryddet || !_harBygget || context.Find<Klods>().Any()) return;

        _ryddet = true;
        context.Publish(new KlodserRyddet());
    }

    private void Byg(GameContext context)
    {
        context.RemoveAll<Klods>();
        _ryddet = false;
        _harBygget = true;

        float samletBredde = Kolonner * (Bredde + Mellemrum) - Mellemrum;
        float startX = -samletBredde / 2f + Bredde / 2f;

        for (int r = 0; r < Raekker; r++)
        {
            var farve = Farver[r % Farver.Length];
            int point = PointPrRaekke[r % PointPrRaekke.Length];
            float y = OeverstY - r * (Hoejde + Mellemrum);

            for (int k = 0; k < Kolonner; k++)
            {
                float x = startX + k * (Bredde + Mellemrum);
                context.Add(new Klods
                {
                    Position = new Vector3(x, y, 0),
                    Bredde = Bredde,
                    Hoejde = Hoejde,
                    Farve = farve,
                    Point = point
                });
            }
        }
    }
}
