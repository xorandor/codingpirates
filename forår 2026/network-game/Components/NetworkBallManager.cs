using Engine;
using Raylib_cs;
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
            string playerName = joined.Fields[0];
            int ballId = _nextBallId++;
            var position = new Vector2(Random.Shared.Next(100, 900), Random.Shared.Next(100, 500));
            var color = new Color(
                (byte)Random.Shared.Next(50, 256),
                (byte)Random.Shared.Next(50, 256),
                (byte)Random.Shared.Next(50, 256),
                (byte)255);

            // Send all existing balls to the new client
            foreach (var (id, ball) in _balls)
                context.Networking.SendMessageToPlayer(playerName, "BALL_JOINED",
                    id.ToString(),
                    ((int)ball.Position.X).ToString(), ((int)ball.Position.Y).ToString(),
                    ball.Color.R.ToString(), ball.Color.G.ToString(), ball.Color.B.ToString());

            // Spawn the new ball and register it
            var newBall = new ArrowKeyControlledBall(position, _speed, Radius, color, ballId);
            _balls[ballId] = newBall;
            _playerBallId[playerName] = ballId;
            context.AddComponent(newBall);

            // Tell the new client which ball is theirs, then announce the new ball to everyone
            context.Networking.SendMessageToPlayer(playerName, "YOUARE", ballId.ToString());
            context.Networking.BroadcastMessageToClients("BALL_JOINED",
                ballId.ToString(),
                ((int)position.X).ToString(), ((int)position.Y).ToString(),
                color.R.ToString(), color.G.ToString(), color.B.ToString());
        }

        var disconnected = context.Networking.TryConsumeMessage("DISCONNECTED", _ => true);
        if (disconnected != null && disconnected.Fields.Length >= 1
            && _playerBallId.TryGetValue(disconnected.Fields[0], out int removedId))
        {
            context.RemoveComponent(_balls[removedId]);
            _balls.Remove(removedId);
            _playerBallId.Remove(disconnected.Fields[0]);
            context.Networking.BroadcastMessageToClients("BALL_LEFT", removedId.ToString());
        }
    }

    private void UpdateClient(UpdateContext context)
    {
        // Consume YOUARE first so we know our own ball ID before processing BALL_JOINED
        if (_myBallId == -1)
        {
            var youAre = context.Networking.TryConsumeMessage("YOUARE", _ => true);
            if (youAre != null && youAre.Fields.Length >= 1 && int.TryParse(youAre.Fields[0], out int id))
                _myBallId = id;
        }

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
                var ball = new ArrowKeyControlledBall(
                    new Vector2(bx, by), _speed, Radius,
                    new Color(r, g, b, (byte)255),
                    ballId: id, isOwn: id == _myBallId);
                _balls[id] = ball;
                context.AddComponent(ball);
            }
        }

        var ballLeft = context.Networking.TryConsumeMessage("BALL_LEFT", _ => true);
        if (ballLeft != null && ballLeft.Fields.Length >= 1
            && int.TryParse(ballLeft.Fields[0], out int leftId)
            && _balls.TryGetValue(leftId, out var leftBall))
        {
            context.RemoveComponent(leftBall);
            _balls.Remove(leftId);
        }
    }

    public void Render() { }
}
