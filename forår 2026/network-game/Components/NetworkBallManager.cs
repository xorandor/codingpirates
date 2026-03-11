using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class NetworkBallManager : IComponent
{
    private const float Radius = 20;

    private readonly float _speed;
    private readonly Dictionary<int, ArrowKeyControlledBall> _balls = new();
    private readonly Dictionary<string, int> _playerBallId = new(); // server only
    private int _myBallId = -1;  // client only
    private int _nextBallId = 1; // server only

    public NetworkBallManager(float speed = 300)
    {
        _speed = speed;
    }

    public void Update(UpdateContext context)
    {
        if (context.Mode == GameMode.Server)
            UpdateServer(context);
        else
            UpdateClient(context);
    }

    private void UpdateServer(UpdateContext context)
    {
        var joined = context.Networking.TryConsumeMessage("JOINED", _ => true);
        if (joined != null && joined.Fields.Length >= 1)
        {
            // Send all existing balls to the new client
            foreach (var (id, ball) in _balls)
                context.Networking.SendMessageToPlayer(joined.Fields[0], "BALL_JOINED",
                    id.ToString(),
                    ((int)ball.Position.X).ToString(), ((int)ball.Position.Y).ToString(),
                    ball.Color.R.ToString(), ball.Color.G.ToString(), ball.Color.B.ToString());

            // Create a new ball with a random color and position
            int newId = _nextBallId++;
            var newBall = new ArrowKeyControlledBall(
                position: new Vector2(Random.Shared.Next(100, 900), Random.Shared.Next(100, 500)),
                speed: _speed,
                radius: Radius,
                color: new Color(
                    (byte)Random.Shared.Next(50, 256),
                    (byte)Random.Shared.Next(50, 256),
                    (byte)Random.Shared.Next(50, 256),
                    (byte)255)
            );
            _balls[newId] = newBall;
            _playerBallId[joined.Fields[0]] = newId;

            // Tell everyone about the new ball, and the new client which ball is theirs
            context.Networking.BroadcastMessageToClients("BALL_JOINED",
                newId.ToString(),
                ((int)newBall.Position.X).ToString(), ((int)newBall.Position.Y).ToString(),
                newBall.Color.R.ToString(), newBall.Color.G.ToString(), newBall.Color.B.ToString());
            context.Networking.SendMessageToPlayer(joined.Fields[0], "YOUARE", newId.ToString());
        }

        var disconnected = context.Networking.TryConsumeMessage("DISCONNECTED", _ => true);
        if (disconnected != null && disconnected.Fields.Length >= 1
            && _playerBallId.TryGetValue(disconnected.Fields[0], out int removedId))
        {
            _balls.Remove(removedId);
            _playerBallId.Remove(disconnected.Fields[0]);
            context.Networking.BroadcastMessageToClients("BALL_LEFT", removedId.ToString());
        }

        // Process incoming ball moves
        foreach (var (id, ball) in _balls)
        {
            var msg = context.Networking.TryConsumeMessage("BOXMOVE",
                m => m.Fields.Length >= 3 && m.Fields[0] == id.ToString());

            if (msg != null && int.TryParse(msg.Fields[1], out int x) && int.TryParse(msg.Fields[2], out int y))
            {
                ball.Position = new Vector2(x, y);
                context.Networking.BroadcastMessageToClients("BOXMOVE_SERVER", msg.Fields);
            }
        }
    }

    private void UpdateClient(UpdateContext context)
    {
        // Learn about new balls (may receive several in one frame)
        NetworkMessage? ballJoined;
        while ((ballJoined = context.Networking.TryConsumeMessage("BALL_JOINED", _ => true)) != null)
        {
            if (ballJoined.Fields.Length >= 6
                && int.TryParse(ballJoined.Fields[0], out int id)
                && int.TryParse(ballJoined.Fields[1], out int bx)
                && int.TryParse(ballJoined.Fields[2], out int by)
                && byte.TryParse(ballJoined.Fields[3], out byte r)
                && byte.TryParse(ballJoined.Fields[4], out byte g)
                && byte.TryParse(ballJoined.Fields[5], out byte b))
            {
                _balls[id] = new ArrowKeyControlledBall(
                    new Vector2(bx, by), _speed, Radius, new Color(r, g, b, (byte)255));
            }
        }

        var ballLeft = context.Networking.TryConsumeMessage("BALL_LEFT", _ => true);
        if (ballLeft != null && ballLeft.Fields.Length >= 1 && int.TryParse(ballLeft.Fields[0], out int leftId))
            _balls.Remove(leftId);

        // Get own ball ID if not yet received
        if (_myBallId == -1)
        {
            var youAre = context.Networking.TryConsumeMessage("YOUARE", _ => true);
            if (youAre != null && youAre.Fields.Length >= 1 && int.TryParse(youAre.Fields[0], out int id))
                _myBallId = id;
        }

        // Move own ball using ArrowKeyControlledBall's Update
        if (_balls.TryGetValue(_myBallId, out var myBall))
        {
            var oldPosition = myBall.Position;
            myBall.Update(context);

            if (myBall.Position != oldPosition)
                context.Networking.SendMessageToServer("BOXMOVE",
                    _myBallId.ToString(),
                    ((int)myBall.Position.X).ToString(),
                    ((int)myBall.Position.Y).ToString());
        }

        // Update other balls from server
        foreach (var (id, ball) in _balls)
        {
            if (id == _myBallId) continue;

            var update = context.Networking.TryConsumeMessage("BOXMOVE_SERVER",
                m => m.Fields.Length >= 3 && m.Fields[0] == id.ToString());

            if (update != null && int.TryParse(update.Fields[1], out int sx) && int.TryParse(update.Fields[2], out int sy))
                ball.Position = new Vector2(sx, sy);
        }
    }

    public void Render()
    {
        foreach (var ball in _balls.Values)
            ball.Render();
    }
}
