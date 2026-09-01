using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Holder oeje med begge pointtal og stopper spillet naar en af siderne naar maalet.
/// Enter starter forfra. Bygget til Pong, fordi den skal kende BEGGE pointtal.
/// </summary>
public class Slutskaerm : Component
{
    public int PointForAtVinde { get; set; } = 11;

    public bool Synlig { get; private set; }
    public override bool IsBlocking => Synlig;

    private string _besked = "";

    public override void OnAdded(GameContext context) => context.On<GameStarted>(_ => Synlig = false);

    public override void Update(GameContext context)
    {
        if (Synlig)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                context.State.SetNumber("venstre", 0);
                context.State.SetNumber("hoejre", 0);
                context.Publish(new GameStarted());
            }
            return;
        }

        if (context.State.Number("venstre") >= PointForAtVinde) Vis(context, "DU VANDT!", vandt: true);
        else if (context.State.Number("hoejre") >= PointForAtVinde) Vis(context, "COMPUTEREN VANDT", vandt: false);
    }

    private void Vis(GameContext context, string besked, bool vandt)
    {
        _besked = besked;
        Synlig = true;

        // Send den rigtige besked ud - saa spiller SoundEffects fanfaren eller sorgen.
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
