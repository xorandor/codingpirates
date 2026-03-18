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
    private readonly int _ballId;     // -1 means no networking
    private readonly bool _isOwn;     // client only: true = controlled by this player
    public string Name { get; }
    private readonly bool _showName;
    private bool _movedThisFrame;
        
    public ArrowKeyControlledBall(Vector2 position, float speed, float radius, Color color, int ballId = -1, bool isOwn = false, string name = "", bool showName = false)
    {
        Position = position;
        Color = color;
        _speed = speed;
        _radius = radius;
        _ballId = ballId;
        _isOwn = isOwn;
        Name = name;
        _showName = showName;
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

            if (_movedThisFrame)
            {
                context.Networking.SendMessageToServer("BOXMOVE",
                    _ballId.ToString(),
                    ((int)Position.X).ToString(),
                    ((int)Position.Y).ToString());
                
                _movedThisFrame = false;
            }
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
            _movedThisFrame = true;
        }
    }

    public void Render()
    {
        DrawCircleV(Position, _radius, Color);

        if (_showName && !string.IsNullOrEmpty(Name))
        {
            int fontSize = 14;
            int textWidth = MeasureText(Name, fontSize);
            int textX = (int)(Position.X - textWidth / 2);
            int textY = (int)(Position.Y - fontSize / 2);

            DrawText(Name, textX + 1, textY + 1, fontSize, Color.Black);
            DrawText(Name, textX, textY, fontSize, Color.White);
        }
    }
}
