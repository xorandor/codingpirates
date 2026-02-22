using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class FloatingText : IComponent
{
    private readonly string _text;
    private Vector2 _position;
    private Vector2 _direction;
    private readonly float _speed;
    private const int FontSize = 20;

    public FloatingText(string text, float angle, float speed)
    {
        _text = text;
        float radians = angle * MathF.PI / 180f;
        _direction = new Vector2(MathF.Cos(radians), -MathF.Sin(radians));
        _speed = speed;
        _position = new Vector2(GetScreenWidth() / 2f, GetScreenHeight() / 2f);
    }

    public void Update(UpdateContext context)
    {
        float delta = GetFrameTime();
        _position += _direction * _speed * delta;

        float textWidth = MeasureTextEx(GetFontDefault(), _text, FontSize, 0).X;
        int screenWidth = GetScreenWidth();
        int screenHeight = GetScreenHeight();

        if (_position.X < 0 || _position.X + textWidth > screenWidth)
        {
            _direction.X = -_direction.X;
            _position.X = Math.Clamp(_position.X, 0, screenWidth - textWidth);
        }

        if (_position.Y < 0 || _position.Y + FontSize > screenHeight)
        {
            _direction.Y = -_direction.Y;
            _position.Y = Math.Clamp(_position.Y, 0, screenHeight - FontSize);
        }
        
    }

    public void Render()
    {
        DrawTextEx(GetFontDefault(), _text, _position, FontSize, 0, Color.Black);
    }
}
