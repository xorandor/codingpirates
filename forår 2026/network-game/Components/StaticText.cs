using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class StaticText : IComponent
{
    private readonly string _text;
    private readonly Vector2 _position;
    private readonly Color _color;
    private readonly int _fontSize;

    public StaticText(string text, Vector2 position, Color color, int fontSize = 20)
    {
        _text = text;
        _position = position;
        _color = color;
        _fontSize = fontSize;
    }

    public void Update(UpdateContext context) { }

    public void Render()
    {
        DrawTextEx(GetFontDefault(), _text, _position, _fontSize, 0, _color);
    }
}
