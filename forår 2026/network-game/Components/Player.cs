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
    private bool _alive = true;
    private bool _moving;
    private float _walkTimer;
    private bool _facingRight = true;

    public float Speed { get; set; }

    public event EventHandler OnCoinCollected;

    public Player(Vector2 position, float speed, float radius, Color color)
    {
        _position = position;
        _startPosition = position;
        Speed = speed;
        _radius = radius;
        _color = color;
    }

    public void Update(UpdateContext context)
    {
        if (!_alive)
        {
            if (IsKeyPressed(KeyboardKey.Enter))
            {
                _alive = true;
                _position = _startPosition;

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
                    OnCoinCollected(this, EventArgs.Empty);
                }
            }
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
    }

    private static void DrawLimb(Vector2 from, Vector2 to, float thickness, Color color)
    {
        DrawLineEx(from, to, thickness * 2f, color);
        DrawCircleV(to, thickness, color);
    }
}
