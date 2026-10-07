using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Skaermen man ser foerst - og igen efter hvert spil: titel, styring og highscore-listen.
/// Mens den er fremme, staar alt andet stille. Enter starter spillet (GameStarted).
/// Kommer frem igen naar NavneSkaerm har gemt et navn (HighscoreGemt).
/// </summary>
public class Velkomstskaerm : Component
{
    public string Titel { get; set; } = "ASTEROIDS";
    public Color TitelFarve { get; set; } = Color.RayWhite;
    public Color OverlayFarve { get; set; } = new(0, 0, 0, 170);

    public bool Synlig { get; private set; } = true;
    public override bool IsBlocking => Synlig;
    public override bool Persistent => true;

    private List<Placering> _liste = [];
    private bool _vistLige;

    public override void OnAdded(GameContext context)
    {
        _liste = Highscore.Laes();

        context.On<GameStarted>(_ => Synlig = false);
        context.On<HighscoreGemt>(_ =>
        {
            _liste = Highscore.Laes();
            Synlig = true;
            _vistLige = true;   // det Enter der gemte navnet, maa ikke ogsaa starte et nyt spil
        });
    }

    public override void Update(GameContext context)
    {
        if (!Synlig) return;

        if (_vistLige)
        {
            _vistLige = false;
            return;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Enter)) context.Publish(new GameStarted());
    }

    public override void RenderUI()
    {
        if (!Synlig) return;

        float w = Raylib.GetScreenWidth();
        float h = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, 0, (int)w, (int)h, OverlayFarve);
        Draw.TextCentered(Titel, new Vector2(w / 2f, 90), 80, TitelFarve);
        Draw.TextCentered("Pil venstre/hoejre drejer - pil op giver gas - mellemrum skyder", new Vector2(w / 2f, 150), 20, Color.LightGray);

        Draw.TextCentered("HIGHSCORE", new Vector2(w / 2f, 215), 30, Color.Gold);

        if (_liste.Count == 0)
            Draw.TextCentered("Ingen endnu - bliv den foerste!", new Vector2(w / 2f, 260), 22, Color.Gray);

        for (int i = 0; i < _liste.Count; i++)
        {
            float y = 255 + i * 30;
            var farve = i == 0 ? Color.Gold : Color.RayWhite;
            Draw.Text($"{i + 1,2}. {_liste[i].Navn}", new Vector2(w / 2f - 180, y), 24, farve);
            string point = _liste[i].Point.ToString();
            Draw.Text(point, new Vector2(w / 2f + 180 - Raylib.MeasureText(point, 24), y), 24, farve);
        }

        // Blinker, saa man kan se der skal trykkes.
        if ((int)(Raylib.GetTime() * 2) % 2 == 0)
            Draw.TextCentered("Tryk Enter for at starte", new Vector2(w / 2f, h - 60), 26, Color.RayWhite);
    }
}
