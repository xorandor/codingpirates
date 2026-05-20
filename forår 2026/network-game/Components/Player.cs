using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class Player : IComponent
{
    private Vector2 _position;
    private readonly float _radius;
    private readonly Color _color;
    private readonly Vector2 _startPosition;
    private readonly int _maxLives;
    private int _lives;
    private float _magnetTimeLeft;
    private float _magnetPullRadius;
    private const float MagnetPullSpeed = 350f;
    private readonly KeyboardKey? _pushKey;
    private readonly float _pushRadius;
    private readonly float _pushCooldown;
    private float _pushCooldownTimer;
    private bool _alive = true;
    private bool _moving;
    private float _walkTimer;
    private bool _facingRight = true;
    private bool _gameStarted;

    public float Speed { get; set; }
    public bool ConstrainToScreen { get; set; }

    public void Reset()
    {
        _alive = true;
        _lives = _maxLives;
        _magnetTimeLeft = 0f;
        _pushCooldownTimer = 0f;
        _position = _startPosition;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler<Coin> OnCoinCollected;
    public event EventHandler OnPlayerDied;

    public Player(Vector2 position, float speed, float radius, Color color, bool constrainToScreen = false, int maxLives = 3,
        KeyboardKey? pushKey = null, float pushRadius = 100f, float pushCooldown = 2f)
    {
        _position = position;
        _startPosition = position;
        Speed = speed;
        _radius = radius;
        _color = color;
        ConstrainToScreen = constrainToScreen;
        _maxLives = maxLives;
        _lives = maxLives;
        _pushKey = pushKey;
        _pushRadius = pushRadius;
        _pushCooldown = pushCooldown;
    }

    public void Update(UpdateContext context)
    {
        if (!_gameStarted)
        {
            _gameStarted = true;
            if (OnGameStarted != null)
            {
                OnGameStarted(this, EventArgs.Empty);
            }
        }

        if (!_alive)
        {
            if (IsKeyPressed(KeyboardKey.Enter))
            {
                _alive = true;
                _lives = _maxLives;
                _magnetTimeLeft = 0f;
                _pushCooldownTimer = 0f;
                _position = _startPosition;

                if (OnGameStarted != null)
                {
                    OnGameStarted(this, EventArgs.Empty);
                }

                // Nulstil score
                var score = context.GetComponents<Score>().FirstOrDefault();
                if (score != null)
                    score.Points = 0;
            }
            return;
        }

        // Bevægelse med piletaster
        Vector2 direction = Vector2.Zero;

        if (IsKeyDown(KeyboardKey.Right)) direction.X += 1;
        if (IsKeyDown(KeyboardKey.Left)) direction.X -= 1;
        if (IsKeyDown(KeyboardKey.Down)) direction.Y += 1;
        if (IsKeyDown(KeyboardKey.Up)) direction.Y -= 1;

        _moving = direction != Vector2.Zero;

        if (_moving)
        {
            direction = Vector2.Normalize(direction);
            _position += direction * Speed * GetFrameTime();
            _walkTimer += GetFrameTime() * 8f;
            if (direction.X > 0) _facingRight = true;
            else if (direction.X < 0) _facingRight = false;

            if (ConstrainToScreen)
            {
                _position.X = Math.Clamp(_position.X, _radius, GetScreenWidth() - _radius);
                _position.Y = Math.Clamp(_position.Y, _radius, GetScreenHeight() - _radius);
            }
        }

        // Saml coins op
        foreach (var coin in context.GetComponents<Coin>())
        {
            float distance = Vector2.Distance(_position, coin.Position);
            if (distance < _radius + coin.Radius)
            {
                context.RemoveComponent(coin);
                if(OnCoinCollected != null)
                {
                    OnCoinCollected(this, coin);
                }
            }
        }

        // Saml power-buffs op (giver ekstra liv)
        foreach (var buff in context.GetComponents<PowerBuffExtraHealth>())
        {
            float distance = Vector2.Distance(_position, buff.Position);
            if (distance < _radius + buff.Radius)
            {
                context.RemoveComponent(buff);
                _lives++;
            }
        }

        // Saml mønt-magnet buff op
        foreach (var magnet in context.GetComponents<PowerBuffCoinMagnet>())
        {
            float distance = Vector2.Distance(_position, magnet.Position);
            if (distance < _radius + magnet.Radius)
            {
                context.RemoveComponent(magnet);
                _magnetTimeLeft = magnet.Duration;
                _magnetPullRadius = magnet.PullRadius;
            }
        }

        // Aktiv magnet-effekt: træk mønter inden for pull-radius hen mod spilleren
        if (_magnetTimeLeft > 0f)
        {
            _magnetTimeLeft -= GetFrameTime();
            float pullStep = MagnetPullSpeed * GetFrameTime();
            foreach (var coin in context.GetComponents<Coin>())
            {
                if (Vector2.Distance(_position, coin.Position) < _magnetPullRadius)
                    coin.MoveToward(_position, pullStep);
            }
        }

        // Skub kugler væk når push-tasten er sat og blev trykket — kun hvis cooldown er udløbet
        if (_pushCooldownTimer > 0f)
            _pushCooldownTimer -= GetFrameTime();

        if (_pushKey != null && _pushCooldownTimer <= 0f && IsKeyPressed(_pushKey.Value))
        {
            bool pushedAny = false;
            foreach (var bullet in context.GetComponents<CircleShooter.Bullet>())
            {
                if (Vector2.Distance(_position, bullet.Position) < _pushRadius)
                {
                    bullet.PushAwayFrom(_position);
                    pushedAny = true;
                }
            }
            if (pushedAny)
                _pushCooldownTimer = _pushCooldown;
        }

        // Tjek kollision med alle kugler fra CircleShooter
        foreach (var bullet in context.GetComponents<CircleShooter.Bullet>())
        {
            float distance = Vector2.Distance(_position, bullet.Position);
            if (distance < _radius + bullet.Radius)
            {
                context.RemoveComponent(bullet);
                _lives--;
                if (_lives <= 0)
                {
                    _alive = false;
                    if (OnPlayerDied != null)
                    {
                        OnPlayerDied(this, EventArgs.Empty);
                    }
                }
                break;
            }
        }
    }

    public void Render()
    {
        float s = _radius / 15f; // skaleringsfaktor
        float x = _position.X;
        float y = _position.Y;
        float flip = _facingRight ? 1f : -1f;

        // Benanimation: vinkel svinger frem og tilbage ved gang
        float legSwing = _moving ? (float)Math.Sin(_walkTimer) * 20f : 0f;
        float armSwing = _moving ? (float)Math.Sin(_walkTimer) * 25f : 0f;

        Color hatColor = _alive ? _color : Color.Gray;
        Color shirtColor = _alive ? _color : Color.DarkGray;
        Color pantsColor = _alive ? new Color(30, 60, 200, 255) : Color.Gray;
        Color skinColor = _alive ? new Color(255, 200, 150, 255) : Color.LightGray;
        Color shoeColor = _alive ? new Color(100, 50, 20, 255) : Color.Gray;

        // Ben (bag krop, tegnes først)
        // Venstre ben
        DrawLimb(
            new Vector2(x - 4f * s * flip, y + 8f * s),
            new Vector2(x - 4f * s * flip + (float)Math.Sin((legSwing) * Math.PI / 180f) * 10f * s,
                        y + 8f * s + (float)Math.Cos((legSwing) * Math.PI / 180f) * 10f * s),
            3f * s, pantsColor);
        // Højre ben
        DrawLimb(
            new Vector2(x + 4f * s * flip, y + 8f * s),
            new Vector2(x + 4f * s * flip + (float)Math.Sin((-legSwing) * Math.PI / 180f) * 10f * s,
                        y + 8f * s + (float)Math.Cos((-legSwing) * Math.PI / 180f) * 10f * s),
            3f * s, pantsColor);

        // Sko
        Vector2 leftFootPos = new(
            x - 4f * s * flip + (float)Math.Sin((legSwing) * Math.PI / 180f) * 10f * s,
            y + 8f * s + (float)Math.Cos((legSwing) * Math.PI / 180f) * 10f * s);
        Vector2 rightFootPos = new(
            x + 4f * s * flip + (float)Math.Sin((-legSwing) * Math.PI / 180f) * 10f * s,
            y + 8f * s + (float)Math.Cos((-legSwing) * Math.PI / 180f) * 10f * s);
        DrawCircleV(leftFootPos, 3f * s, shoeColor);
        DrawCircleV(rightFootPos, 3f * s, shoeColor);

        // Krop (overalls)
        DrawRectangleV(new Vector2(x - 7f * s, y - 4f * s), new Vector2(14f * s, 13f * s), shirtColor);

        // Knapper på overalls
        if (_alive)
        {
            DrawCircleV(new Vector2(x - 3f * s, y + 2f * s), 1.5f * s, Color.Yellow);
            DrawCircleV(new Vector2(x + 3f * s, y + 2f * s), 1.5f * s, Color.Yellow);
        }

        // Arme
        // Venstre arm
        DrawLimb(
            new Vector2(x - 7f * s, y - 1f * s),
            new Vector2(x - 7f * s + (float)Math.Sin((-armSwing) * Math.PI / 180f) * 9f * s,
                        y - 1f * s + (float)Math.Cos((-armSwing) * Math.PI / 180f) * 9f * s),
            2.5f * s, skinColor);
        // Højre arm
        DrawLimb(
            new Vector2(x + 7f * s, y - 1f * s),
            new Vector2(x + 7f * s + (float)Math.Sin((armSwing) * Math.PI / 180f) * 9f * s,
                        y - 1f * s + (float)Math.Cos((armSwing) * Math.PI / 180f) * 9f * s),
            2.5f * s, skinColor);

        // Hoved
        DrawCircleV(new Vector2(x, y - 11f * s), 8f * s, skinColor);

        // Øjne
        float eyeOffsetX = 3f * s * flip;
        DrawCircleV(new Vector2(x + eyeOffsetX - 2f * s, y - 13f * s), 1.5f * s, Color.White);
        DrawCircleV(new Vector2(x + eyeOffsetX + 2f * s, y - 13f * s), 1.5f * s, Color.White);
        DrawCircleV(new Vector2(x + eyeOffsetX - 1.5f * s * flip + 0.5f * s * flip, y - 13f * s), 0.8f * s, Color.Black);
        DrawCircleV(new Vector2(x + eyeOffsetX + 2.5f * s * flip - 0.5f * s * flip, y - 13f * s), 0.8f * s, Color.Black);

        // Overskæg
        if (_alive)
            DrawRectangleV(new Vector2(x + eyeOffsetX - 3f * s, y - 10f * s), new Vector2(6f * s, 1.5f * s), new Color(100, 50, 20, 255));

        // Hat (kasket)
        DrawRectangleV(new Vector2(x - 8f * s, y - 19f * s), new Vector2(16f * s, 6f * s), hatColor);
        // Kasket-skygge
        float visorX = _facingRight ? x + 2f * s : x - 11f * s;
        DrawRectangleV(new Vector2(visorX, y - 15f * s), new Vector2(9f * s, 2.5f * s), hatColor);

        // Lille magnet ved siden af kasketten når mønt-magnet er aktiv
        if (_alive && _magnetTimeLeft > 0f)
        {
            float magnetSize = 5f * s;
            float magnetX = x + 13f * s;
            float magnetY = y - 19f * s;
            PowerBuffCoinMagnet.DrawMagnet(new Vector2(magnetX, magnetY), magnetSize);
        }

        if (!_alive)
        {
            // Kryds over øjne
            float ex1 = x + eyeOffsetX - 2f * s;
            float ex2 = x + eyeOffsetX + 2f * s;
            float ey = y - 13f * s;
            DrawLineEx(new Vector2(ex1 - 1.5f * s, ey - 1.5f * s), new Vector2(ex1 + 1.5f * s, ey + 1.5f * s), 1.5f * s, Color.Red);
            DrawLineEx(new Vector2(ex1 + 1.5f * s, ey - 1.5f * s), new Vector2(ex1 - 1.5f * s, ey + 1.5f * s), 1.5f * s, Color.Red);
            DrawLineEx(new Vector2(ex2 - 1.5f * s, ey - 1.5f * s), new Vector2(ex2 + 1.5f * s, ey + 1.5f * s), 1.5f * s, Color.Red);
            DrawLineEx(new Vector2(ex2 + 1.5f * s, ey - 1.5f * s), new Vector2(ex2 - 1.5f * s, ey + 1.5f * s), 1.5f * s, Color.Red);

            int fontSize = 20;
            string text = "DU ER DØD!";
            int textWidth = MeasureText(text, fontSize);
            DrawText(text, (int)x - textWidth / 2, (int)(y + 22f * s), fontSize, Color.Red);
        }

        // Hjerter i øvre højre hjørne — placeret under statuslinjen (som bruger fontSize 40)
        float heartSize = 28f;
        float gap = 6f;
        float rightMargin = 10f;
        float topY = 60f;
        for (int i = 0; i < _lives; i++)
        {
            float centerX = GetScreenWidth() - rightMargin - heartSize / 2f - i * (heartSize + gap);
            float centerY = topY + heartSize / 2f;
            DrawHeart(new Vector2(centerX, centerY), heartSize, Color.Red);
        }
    }

    private static void DrawLimb(Vector2 from, Vector2 to, float thickness, Color color)
    {
        DrawLineEx(from, to, thickness * 2f, color);
        DrawCircleV(to, thickness, color);
    }

    private static void DrawHeart(Vector2 center, float size, Color color)
    {
        float lobeRadius = size * 0.3f;
        var leftLobe = new Vector2(center.X - size * 0.22f, center.Y - size * 0.18f);
        var rightLobe = new Vector2(center.X + size * 0.22f, center.Y - size * 0.18f);
        DrawCircleV(leftLobe, lobeRadius, color);
        DrawCircleV(rightLobe, lobeRadius, color);

        // Nedadvendt trekant der danner bunden af hjertet (CCW vertex-rækkefølge)
        var topLeft = new Vector2(center.X - size * 0.5f, center.Y - size * 0.1f);
        var topRight = new Vector2(center.X + size * 0.5f, center.Y - size * 0.1f);
        var bottom = new Vector2(center.X, center.Y + size * 0.5f);
        DrawTriangle(topLeft, bottom, topRight, color);
    }
}
