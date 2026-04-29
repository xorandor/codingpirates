using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class ManualScoreCounter : IComponent
{
    private readonly Vector2 _position;
    private readonly int _fontSize;
    private readonly Color _color;

    public int Score { get; set; }

    public ManualScoreCounter(Vector2 position, int fontSize = 30, Color? color = null)
    {
        _position = position;
        _fontSize = fontSize;
        _color = color ?? Color.Black;
    }

    public void Update(UpdateContext context)
    {
    }

    public void Render()
    {
        DrawText($"Score: {Score}", (int)_position.X, (int)_position.Y, _fontSize, _color);
    }
}
