using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// En startskaerm der ligger hen over spillet, indtil man trykker Enter eller mellemrum.
///
///   game.Add(new StartScreen { Title = "PONG" });
///
/// Mens den er fremme er alt andet sat paa pause (IsBlocking). Verden bliver stadig tegnet,
/// den staar bare stille. Naar man trykker, sender den beskeden GameStarted ud.
/// </summary>
public class StartScreen : Component
{
    public string Title { get; set; } = "MIT SPIL";
    public string Subtitle { get; set; } = "Tryk Enter for at starte";
    public Color OverlayColor { get; set; } = new(0, 0, 0, 180);
    public Color TitleColor { get; set; } = Color.White;
    public Color SubtitleColor { get; set; } = Color.LightGray;
    public int TitleFontSize { get; set; } = 80;

    public bool IsVisible { get; private set; } = true;

    public override bool IsBlocking => IsVisible && Enabled;
    public override bool Persistent => true;

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;

    public override void OnAdded(GameContext context) => context.On<GameOver>(_ => Hide());

    public override void Update(GameContext context)
    {
        if (!IsVisible) return;
        if (!IsKeyPressed(KeyboardKey.Enter) && !IsKeyPressed(KeyboardKey.Space)) return;

        Hide();
        context.Publish(new GameStarted());
    }

    public override void RenderUI()
    {
        if (!IsVisible) return;

        float width = GetScreenWidth();
        float height = GetScreenHeight();

        DrawRectangle(0, 0, (int)width, (int)height, OverlayColor);
        Draw.TextCentered(Title, new Vector2(width / 2f, height / 2f - 60), TitleFontSize, TitleColor);
        Draw.TextCentered(Subtitle, new Vector2(width / 2f, height / 2f + 60), 24, SubtitleColor);
    }
}
