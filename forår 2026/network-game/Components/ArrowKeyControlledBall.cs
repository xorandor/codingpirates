using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class ArrowKeyControlledBall : IComponent
{
    public Vector2 Position { get; set; }
    private readonly float _speed;
    private readonly float _radius;
    public Color Color { get; }

    public ArrowKeyControlledBall(Vector2 position, float speed, float radius, Color color)
    {
        Position = position;
        _speed = speed;
        _radius = radius;
        Color = color;
    }

    public void Update(UpdateContext context)
    {
        Vector2 direction = Vector2.Zero;

        if (IsKeyDown(KeyboardKey.Right)) direction.X += 1;
        if (IsKeyDown(KeyboardKey.Left))  direction.X -= 1;
        if (IsKeyDown(KeyboardKey.Down))  direction.Y += 1;
        if (IsKeyDown(KeyboardKey.Up))    direction.Y -= 1;

        if (direction != Vector2.Zero)
        {
            direction = Vector2.Normalize(direction);
            Position += direction * _speed * GetFrameTime();
        }
    }

    public void Render()
    {
        DrawCircleV(Position, _radius, Color);
    }
}
