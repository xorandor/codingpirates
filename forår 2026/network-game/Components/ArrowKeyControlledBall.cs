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
    private readonly int? _networkBoxId;

    public ArrowKeyControlledBall(Vector2 position, float speed, float radius, Color color, int? networkBoxId = null)
    {
        _position = position;
        _speed = speed;
        _radius = radius;
        _color = color;
        _networkBoxId = networkBoxId;
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
            _position += direction * _speed * GetFrameTime();

            if (_networkBoxId.HasValue)
                context.Networking.SendBoxMove(_networkBoxId.Value, _position);
        }

        if (_networkBoxId.HasValue)
        {
            var msg = context.Networking.TryConsumeMessage("BOXMOVE",
                m => m.Fields.Length >= 3 && m.Fields[0] == _networkBoxId.Value.ToString());

            if (msg != null && int.TryParse(msg.Fields[1], out int x) && int.TryParse(msg.Fields[2], out int y))
                _position = new Vector2(x, y);
        }
    }

    public void Render()
    {
        DrawCircleV(_position, _radius, _color);
    }
}
