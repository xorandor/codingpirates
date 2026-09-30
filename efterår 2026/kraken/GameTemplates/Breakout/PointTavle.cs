using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Viser point oeverst til venstre og antal liv oeverst til hoejre. Laeser dem fra
/// context.State under "point" og "liv" - det er Slutskaerm der laegger dem derind.
/// </summary>
public class PointTavle : Component
{
    public int FontSize { get; set; } = 36;
    public Color Farve { get; set; } = Color.RayWhite;

    // Skal blive ved med at laese liv/point ogsaa mens StartScreen/Slutskaerm blokerer,
    // ellers staar der "Liv: 0" indtil man trykker Enter foerste gang.
    public override bool RunsWhileBlocked => true;

    private int _point, _liv;

    public override void Update(GameContext context)
    {
        _point = context.State.Number("point");
        _liv = context.State.Number("liv");
    }

    public override void RenderUI()
    {
        float bredde = Raylib.GetScreenWidth();
        string livTekst = $"Liv: {_liv}";
        float livBredde = Raylib.MeasureText(livTekst, FontSize);

        Draw.Text($"Point: {_point}", new Vector2(24, 20), FontSize, Farve);
        Draw.Text(livTekst, new Vector2(bredde - 24 - livBredde, 20), FontSize, Farve);
    }
}
