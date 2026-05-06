using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class HighScore : IComponent
{
    private readonly Vector2 _position;
    private readonly int _fontSize;
    private readonly Color _color;

    public int Points { get; private set; }
    public string Label { get; set; }

    public HighScore(Vector2 position, int fontSize = 30, Color? color = null, string label = "High Score")
    {
        _position = position;
        _fontSize = fontSize;
        _color = color ?? Color.Black;
        Label = label;
    }

    public void Submit(int points)
    {
        if (points > Points)
            Points = points;
    }

    public void Reset()
    {
        Points = 0;
    }

    public void Update(UpdateContext context)
    {
    }

    public void Render()
    {
        DrawText($"{Label}: {Points}", (int)_position.X, (int)_position.Y, _fontSize, _color);
    }
}
