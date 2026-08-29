using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// En tekst der bliver liggende paa den samme plads paa skaermen.
///
///   game.Add(new StaticText { Text = "Mit spil", ScreenPosition = new(20, 20), FontSize = 40 });
/// </summary>
public class StaticText : Component
{
    public string Text { get; set; } = "";

    /// <summary>Skaermkoordinater. (0,0) er oeverste venstre hjoerne.</summary>
    public Vector2 ScreenPosition { get; set; } = new(20, 20);

    public int FontSize { get; set; } = 20;
    public Color Color { get; set; } = Color.Black;

    /// <summary>Naar true er ScreenPosition midten af teksten i stedet for dens venstre kant.</summary>
    public bool Centered { get; set; }

    public override void RenderUI()
    {
        if (Centered)
            Draw.TextCentered(Text, ScreenPosition, FontSize, Color);
        else
            Draw.Text(Text, ScreenPosition, FontSize, Color);
    }
}
