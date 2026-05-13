using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class WinCondition : IComponent
{
    public string? Credits => "Oliver";

    private readonly int _scoreToWin;
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 0.05f;

    public static bool HasWon { get; private set; } = false;

    public static void ResetWin()
    {
        HasWon = false;
    }

    public WinCondition(int scoreToWin = 100)
    {
        _scoreToWin = scoreToWin;
    }

    public void Update(UpdateContext context)
    {
        var score = context.GetComponents<Score>().FirstOrDefault();
        if (score == null) return;

        if (!HasWon && score.Points >= _scoreToWin)
            HasWon = true;

        if (HasWon)
        {
            _spawnTimer += GetFrameTime();
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer -= SpawnInterval;
                float x = Random.Shared.Next(0, GetScreenWidth());
                float speed = Random.Shared.Next(150, 350);
                float radius = Random.Shared.Next(8, 18);
                context.AddComponent(new FallingCoin(new Vector2(x, -radius), speed, radius));
            }
        }
        else
        {
            _spawnTimer = 0f;
        }
    }

    public void Render()
    {
        if (!HasWon) return;

        int screenW = GetScreenWidth();
        int screenH = GetScreenHeight();

        DrawRectangle(0, 0, screenW, screenH, new Color(0, 0, 0, 120));

        int fontSize = 80;
        string text = "YOU WON!";
        int textWidth = MeasureText(text, fontSize);
        int x = screenW / 2 - textWidth / 2;
        int y = screenH / 2 - fontSize / 2;

        DrawText(text, x + 4, y + 4, fontSize, Color.Black);
        DrawText(text, x, y, fontSize, Color.Gold);

        int subFontSize = 24;
        string sub = "Press ENTER to play again";
        int subWidth = MeasureText(sub, subFontSize);
        DrawText(sub, screenW / 2 - subWidth / 2, y + fontSize + 20, subFontSize, Color.White);
    }
}
