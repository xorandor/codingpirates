using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class ArrowKeyControlledBall : IComponent
{
    public Vector2 Position { get; set; }
    public Color Color { get; }

    private readonly float _speed;
    private readonly float _radius;
    private readonly int _ballId;   // -1 means no networking
    private readonly bool _isOwn;   // client only: true = controlled by this player

    public ArrowKeyControlledBall(Vector2 position, float speed, float radius, Color color, int ballId = -1, bool isOwn = false)
    {
        Position = position;
        Color = color;
        _speed = speed;
        _radius = radius;
        _ballId = ballId;
        _isOwn = isOwn;
    }

    public void Update(UpdateContext context)
    {
        if (_ballId == -1)
        {
            MoveWithArrowKeys();
            return;
        }

        if (context.Mode == GameMode.Server)
        {
            var msg = context.Networking.TryConsumeMessage("BOXMOVE",
                m => m.Fields.Length >= 3 && m.Fields[0] == _ballId.ToString());

            if (msg != null && int.TryParse(msg.Fields[1], out int x) && int.TryParse(msg.Fields[2], out int y))
            {
                Position = new Vector2(x, y);
                context.Networking.BroadcastMessageToClients("BOXMOVE_SERVER", msg.Fields);
            }
        }
        else if (_isOwn)
        {
            MoveWithArrowKeys();
            context.Networking.SendMessageToServer("BOXMOVE",
                _ballId.ToString(),
                ((int)Position.X).ToString(),
                ((int)Position.Y).ToString());
        }
        else
        {
            var update = context.Networking.TryConsumeMessage("BOXMOVE_SERVER",
                m => m.Fields.Length >= 3 && m.Fields[0] == _ballId.ToString());

            if (update != null && int.TryParse(update.Fields[1], out int sx) && int.TryParse(update.Fields[2], out int sy))
                Position = new Vector2(sx, sy);
        }
    }

    private void MoveWithArrowKeys()
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
