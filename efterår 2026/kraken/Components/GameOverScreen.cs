using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// Kommer frem af sig selv naar nogen sender beskeden GameOver ud, og forsvinder
/// igen naar man trykker Enter.
///
///   game.Add(new GameOverScreen());
///   game.Add(new GameOverScreen { Message = "AV!", RestartsGame = false });
///
/// Med RestartsGame slaaet til nulstiller den point-tallet og sender GameStarted ud,
/// saa alle komponenter kan stille sig tilbage til start.
/// </summary>
public class GameOverScreen : Component
{
    public string Message { get; set; } = "GAME OVER";
    public string Subtitle { get; set; } = "Tryk Enter for at spille igen";
    public Color OverlayColor { get; set; } = new(0, 0, 0, 190);
    public Color MessageColor { get; set; } = Color.Red;
    public Color SubtitleColor { get; set; } = Color.LightGray;
    public int FontSize { get; set; } = 90;

    /// <summary>Nulstiller pointene og sender GameStarted ud, naar man trykker Enter.</summary>
    public bool RestartsGame { get; set; } = true;

    /// <summary>Hvilket tal der nulstilles ved genstart.</summary>
    public string ScoreKey { get; set; } = "score";

    public bool IsVisible { get; private set; }

    public override bool IsBlocking => IsVisible && Enabled;
    public override bool Persistent => true;

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;

    public override void OnAdded(GameContext context) => context.On<GameOver>(_ => Show());

    public override void Update(GameContext context)
    {
        if (!IsVisible) return;
        if (!IsKeyPressed(KeyboardKey.Enter) && !IsKeyPressed(KeyboardKey.Space)) return;

        Hide();

        if (!RestartsGame) return;

        context.State.SetNumber(ScoreKey, 0);
        context.Publish(new GameStarted());
    }

    public override void RenderUI()
    {
        if (!IsVisible) return;

        float width = GetScreenWidth();
        float height = GetScreenHeight();

        DrawRectangle(0, 0, (int)width, (int)height, OverlayColor);
        Draw.TextCentered(Message, new Vector2(width / 2f, height / 2f - 60), FontSize, MessageColor);
        Draw.TextCentered(Subtitle, new Vector2(width / 2f, height / 2f + 60), 24, SubtitleColor);
    }
}
