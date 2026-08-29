using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// Taeller hvor mange komponenter der har et bestemt maerkat, og viser tallet.
///
///   game.Add(new TagCounter { Tag = "moent", Label = "Monter tilbage" });
///   game.Add(new TagCounter { Tag = "kugle", Label = "Kugler", ScreenPosition = new(20, 120) });
///
/// Den erstatter CoinCounter og CircleShooterBallCounter fra foraaret. Forskellen er,
/// at den ikke kender en eneste komponenttype - den kigger kun efter maerkatet.
/// Saet selv maerkater paa dine egne komponenter: Tags = { "fjende" }.
/// </summary>
public class TagCounter : Component
{
    public string Tag { get; set; } = "moent";
    public string Label { get; set; } = "Antal";
    public Vector2 ScreenPosition { get; set; } = new(20, 80);
    public int FontSize { get; set; } = 24;
    public Color Color { get; set; } = Color.DarkGray;

    public int Count { get; private set; }

    public override void Update(GameContext context) => Count = context.FindByTag(Tag).Count();

    public override void RenderUI() => Draw.Text($"{Label}: {Count}", ScreenPosition, FontSize, Color);
}
