using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// Holder oeje med pointtallet, og erklaerer spillet vundet naar maalet er naaet.
///
///   game.Add(new WinCondition { ScoreToWin = 100 });
///
/// Naar man vinder, sender den beskeden GameWon ud og lader det regne med monter.
/// </summary>
public class WinCondition : Component
{
    public override string? Credits => "Oliver";

    public int ScoreToWin { get; set; } = 100;

    /// <summary>Hvilket tal i context.State der holdes oeje med.</summary>
    public string ScoreKey { get; set; } = "score";

    public string Message { get; set; } = "DU VANDT!";
    public Color MessageColor { get; set; } = Color.Gold;

    /// <summary>Lader det regne med monter naar man vinder.</summary>
    public bool CoinRain { get; set; } = true;

    public bool HasWon { get; private set; }

    public override bool Persistent => true;

    private TimerHandle? _rain;

    public override void OnAdded(GameContext context) => context.On<GameStarted>(_ => Reset(context));

    public override void Update(GameContext context)
    {
        if (HasWon)
        {
            if (IsKeyPressed(KeyboardKey.Enter)) Restart(context);
            return;
        }

        if (context.State.Number(ScoreKey) < ScoreToWin) return;

        HasWon = true;
        context.State.SetFlag("vundet", true);
        context.Publish(new GameWon());

        if (CoinRain) _rain = context.Every(0.05f, () => DropCoin(context));
    }

    private void Restart(GameContext context)
    {
        Reset(context);
        context.State.SetNumber(ScoreKey, 0);
        context.Publish(new GameStarted());
    }

    private void Reset(GameContext context)
    {
        HasWon = false;
        context.State.SetFlag("vundet", false);
        _rain?.Cancel();
        _rain = null;
        context.RemoveAll<FallingCoin>();
    }

    private static void DropCoin(GameContext context)
    {
        float halfHeight = context.Camera.Height / 2f;
        float halfWidth = halfHeight * GetScreenWidth() / GetScreenHeight();

        context.Add(new FallingCoin
        {
            Position = new Vector3(
                Random.Shared.Next((int)-halfWidth, (int)halfWidth),
                halfHeight + 30f,
                0),
            Speed = Random.Shared.Next(150, 350),
            Radius = Random.Shared.Next(8, 18)
        });
    }

    public override void RenderUI()
    {
        if (!HasWon) return;

        float width = GetScreenWidth();
        float height = GetScreenHeight();

        DrawRectangle(0, 0, (int)width, (int)height, new Color(0, 0, 0, 120));
        Draw.TextCentered(Message, new Vector2(width / 2f, height / 2f), 80, MessageColor);
        Draw.TextCentered("Tryk Enter for at spille igen", new Vector2(width / 2f, height / 2f + 80), 24, Color.White);
    }
}
