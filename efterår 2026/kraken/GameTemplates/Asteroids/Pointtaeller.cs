using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Viser point og rekord oeverst til venstre, liv oeverst til hoejre, tid og niveau i midten,
/// og skibets opgraderinger nederst. Laeser det hele fra context.State - det er Spilstyring
/// og Skib der laegger tallene derind.
/// </summary>
public class Pointtaeller : Component
{
    public int FontSize { get; set; } = 32;
    public Color Farve { get; set; } = Color.RayWhite;
    public Color RekordFarve { get; set; } = Color.Gold;

    // Skal blive ved med at laese tallene ogsaa mens en skaerm blokerer,
    // ellers staar der "Liv: 0" indtil man trykker Enter foerste gang.
    public override bool RunsWhileBlocked => true;

    private int _point, _rekord, _liv, _tid, _niveau, _skud, _fart;

    public override void Update(GameContext context)
    {
        _point = context.State.Number("point");
        _rekord = context.State.Number("rekord");
        _liv = context.State.Number("liv");
        _tid = context.State.Number("tid");
        _niveau = context.State.Number("niveau");
        _skud = context.State.Number("skud-niveau");
        _fart = context.State.Number("fart-niveau");
    }

    public override void RenderUI()
    {
        float bredde = Raylib.GetScreenWidth();
        float hoejde = Raylib.GetScreenHeight();
        int lille = FontSize * 2 / 3;

        // Slaar man rekorden, skifter pointtallet til guld.
        Draw.Text($"Point: {_point}", new Vector2(24, 20), FontSize, _point > _rekord ? RekordFarve : Farve);
        Draw.Text($"Rekord: {_rekord}", new Vector2(24, 24 + FontSize), lille, Color.LightGray);

        string livTekst = $"Liv: {_liv}";
        Draw.Text(livTekst, new Vector2(bredde - 24 - Raylib.MeasureText(livTekst, FontSize), 20), FontSize, Farve);

        Draw.TextCentered($"Niveau {_niveau}   {_tid} s", new Vector2(bredde / 2f, 20 + lille / 2f), lille, Color.LightGray);

        int skudAntal = _skud switch { 1 => 1, 2 => 3, _ => 5 };
        Draw.Text($"Skud: x{skudAntal}   Skudfart: {_fart}/3", new Vector2(24, hoejde - 24 - lille), lille, Color.LightGray);
    }
}
