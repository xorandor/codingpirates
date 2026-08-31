using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Viser de to pointtal oeverst, paa hver side af midterlinjen. Tallene ligger i
/// context.State under "venstre" og "hoejre" - det er bolden der laegger dem derind.
/// </summary>
public class PointTavle : Component
{
    public int FontSize { get; set; } = 60;
    public Color Farve { get; set; } = Color.RayWhite;

    private int _venstre, _hoejre;

    public override void Update(GameContext context)
    {
        _venstre = context.State.Number("venstre");
        _hoejre = context.State.Number("hoejre");
    }

    public override void RenderUI()
    {
        float midt = Raylib.GetScreenWidth() / 2f;
        float venstreBredde = Raylib.MeasureText(_venstre.ToString(), FontSize);

        // Hoejrestillet til venstre for midten, venstrestillet til hoejre - saa tallene vokser udad.
        Draw.Text(_venstre.ToString(), new Vector2(midt - 60 - venstreBredde, 30), FontSize, Farve);
        Draw.Text(_hoejre.ToString(), new Vector2(midt + 60, 30), FontSize, Farve);
    }
}
