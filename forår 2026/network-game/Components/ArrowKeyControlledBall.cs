using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class ArrowKeyControlledBall : IComponent
{
    private Vector2 _position;
    private readonly float _speed;
    private readonly float _radius;
    private readonly Color _color;

    public ArrowKeyControlledBall(Vector2 position, float speed, float radius, Color color)
    {
        _position = position;
        _speed = speed;
        _radius = radius;
        _color = color;
    }

    public void Update()
    {
        Vector2 direction = Vector2.Zero;

        if (IsKeyDown(KeyboardKey.Right)) direction.X += 1;
        if (IsKeyDown(KeyboardKey.Left))  direction.X -= 1;
        if (IsKeyDown(KeyboardKey.Down))  direction.Y += 1;
        if (IsKeyDown(KeyboardKey.Up))    direction.Y -= 1;

        if (direction != Vector2.Zero)
            direction = Vector2.Normalize(direction);

        _position += direction * _speed * GetFrameTime();
    }

    public void Render()
    {
        DrawCircleV(_position, _radius, _color);
    }
}
