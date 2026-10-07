using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Kommer frem naar spillet er slut (GameOver): viser pointene, og lader spilleren skrive sit
/// navn og trykke Enter. Navnet gemmes paa highscore-listen, og saa sendes HighscoreGemt, saa
/// Velkomstskaerm kan vise listen igen. Mens den er fremme, staar alt andet stille.
/// </summary>
public class NavneSkaerm : Component
{
    public int MaxLaengde { get; set; } = 12;
    public string TomtNavn { get; set; } = "???";

    public bool Synlig { get; private set; }
    public override bool IsBlocking => Synlig;
    public override bool Persistent => true;

    private string _navn = "";
    private int _point;
    private bool _nyRekord;

    public override void OnAdded(GameContext context) => context.On<GameOver>(_ =>
    {
        _point = context.State.Number("point");
        _nyRekord = _point > context.State.Number("rekord");
        _navn = "";
        Synlig = true;
    });

    public override void Update(GameContext context)
    {
        if (!Synlig) return;

        // Bogstaver og tal som de bliver tastet - GetCharPressed giver dem eet ad gangen.
        int tegn;
        while ((tegn = Raylib.GetCharPressed()) > 0)
        {
            if (tegn >= 32 && tegn < 127 && _navn.Length < MaxLaengde)
                _navn += (char)tegn;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && _navn.Length > 0)
            _navn = _navn[..^1];

        if (!Raylib.IsKeyPressed(KeyboardKey.Enter)) return;

        string navn = _navn.Trim();
        Highscore.Gem(navn.Length > 0 ? navn : TomtNavn, _point);
        Synlig = false;
        context.Publish(new HighscoreGemt());
    }

    public override void RenderUI()
    {
        if (!Synlig) return;

        float w = Raylib.GetScreenWidth();
        float h = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, 0, (int)w, (int)h, new Color(0, 0, 0, 200));
        Draw.TextCentered("GAME OVER", new Vector2(w / 2f, h / 2f - 150), 80, Color.RayWhite);
        Draw.TextCentered($"{_point} point", new Vector2(w / 2f, h / 2f - 80), 36, _nyRekord ? Color.Gold : Color.LightGray);

        if (_nyRekord)
            Draw.TextCentered("NY REKORD!", new Vector2(w / 2f, h / 2f - 40), 28, Color.Gold);

        Draw.TextCentered("Skriv dit navn:", new Vector2(w / 2f, h / 2f + 20), 24, Color.LightGray);

        // Markoeren blinker, saa man kan se der kan skrives.
        string markoer = (int)(Raylib.GetTime() * 3) % 2 == 0 ? "_" : " ";
        Draw.TextCentered(_navn + markoer, new Vector2(w / 2f, h / 2f + 65), 40, Color.RayWhite);

        Draw.TextCentered("Enter gemmer", new Vector2(w / 2f, h / 2f + 130), 20, Color.Gray);
    }
}
