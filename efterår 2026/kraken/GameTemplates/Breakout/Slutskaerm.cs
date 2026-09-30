using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Holder oeje med liv og klodser, og stopper spillet naar man vinder eller taber.
/// Enter starter forfra. Ejer ogsaa "point" og "liv" i context.State - saetter dem ved start.
/// </summary>
public class Slutskaerm : Component
{
    public int StartLiv { get; set; } = 3;

    public bool Synlig { get; private set; }
    public override bool IsBlocking => Synlig;

    private string _besked = "";

    public override void OnAdded(GameContext context)
    {
        context.State.SetNumber("liv", StartLiv);

        context.On<GameStarted>(_ =>
        {
            Synlig = false;
            context.State.SetNumber("point", 0);
            context.State.SetNumber("liv", StartLiv);
        });

        context.On<KlodserRyddet>(_ => Vis(context, "DU VANDT!", vandt: true));
    }

    public override void Update(GameContext context)
    {
        if (Synlig)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter)) context.Publish(new GameStarted());
            return;
        }

        if (context.State.Number("liv") <= 0) Vis(context, "GAME OVER", vandt: false);
    }

    private void Vis(GameContext context, string besked, bool vandt)
    {
        _besked = besked;
        Synlig = true;

        if (vandt) context.Publish(new GameWon());
        else context.Publish(new GameOver());
    }

    public override void RenderUI()
    {
        if (!Synlig) return;

        float w = Raylib.GetScreenWidth();
        float h = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, 0, (int)w, (int)h, new Color(0, 0, 0, 190));
        Draw.TextCentered(_besked, new Vector2(w / 2f, h / 2f - 40), 80, Color.RayWhite);
        Draw.TextCentered("Tryk Enter for at spille igen", new Vector2(w / 2f, h / 2f + 60), 24, Color.LightGray);
    }
}
