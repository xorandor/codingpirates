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
    private const int FontSize = 20;

    public StaticText(string text, Vector2 position, Color color)
    {
        _text = text;
        _position = position;
        _color = color;
    }

    public void Update(UpdateContext context) { }

    public void Render()
    {
        DrawTextEx(GetFontDefault(), _text, _position, FontSize, 0, _color);
    }
}
