using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// Viser et tal fra spillets faelles hukommelse.
///
///   game.Add(new Score());                               // viser "Score: 0"
///   game.Add(new Score { Key = "liv", Label = "Liv" });   // viser et andet tal
///
/// Score gemmer ikke selv pointene - de ligger i context.State. Det betyder at ALLE
/// komponenter kan give point uden foerst at lede efter Score-komponenten:
///
///   context.State.Add("score", 10);
/// </summary>
public class Score : Component
{
    /// <summary>Hvilket tal i context.State der vises.</summary>
    public string Key { get; set; } = "score";

    public string Label { get; set; } = "Score";
    public Vector2 ScreenPosition { get; set; } = new(20, 40);
    public int FontSize { get; set; } = 30;
    public Color Color { get; set; } = Color.DarkBlue;

    public override bool Persistent => true;

    private int _points;

    public override void OnAdded(GameContext context) => _points = context.State.Number(Key);

    public override void Update(GameContext context) => _points = context.State.Number(Key);

    public override void RenderUI()
        => Draw.Text(Label.Length > 0 ? $"{Label}: {_points}" : _points.ToString(), ScreenPosition, FontSize, Color);
}
