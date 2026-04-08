using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class Player : IComponent
{
    private Vector2 _position;
    private readonly float _speed;
    private readonly float _radius;
    private readonly Color _color;
    private bool _alive = true;

    public Player(Vector2 position, float speed, float radius, Color color)
    {
        _position = position;
        _speed = speed;
        _radius = radius;
        _color = color;
    }

    public void Update(UpdateContext context)
    {
        if (!_alive)
            return;

        // Bevægelse med piletaster
        Vector2 direction = Vector2.Zero;

        if (IsKeyDown(KeyboardKey.Right)) direction.X += 1;
        if (IsKeyDown(KeyboardKey.Left)) direction.X -= 1;
        if (IsKeyDown(KeyboardKey.Down)) direction.Y += 1;
        if (IsKeyDown(KeyboardKey.Up)) direction.Y -= 1;

        if (direction != Vector2.Zero)
        {
            direction = Vector2.Normalize(direction);
            _position += direction * _speed * GetFrameTime();
        }

        // Tjek kollision med alle kugler fra CircleShooter
        foreach (var bullet in context.GetComponents<CircleShooter.Bullet>())
        {
            float distance = Vector2.Distance(_position, bullet.Position);
            if (distance < _radius + bullet.Radius)
            {
                _alive = false;
                break;
            }
        }
    }

    public void Render()
    {
        if (_alive)
        {
            DrawCircleV(_position, _radius, _color);
        }
        else
        {
            DrawCircleV(_position, _radius, Color.Gray);
            int fontSize = 20;
            string text = "DU ER DØD!";
            int textWidth = MeasureText(text, fontSize);
            DrawText(text, (int)_position.X - textWidth / 2, (int)_position.Y - fontSize / 2, fontSize, Color.Red);
        }
    }
}
