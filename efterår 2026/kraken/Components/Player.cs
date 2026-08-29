using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// En spiller. Styres med piletasterne (eller WASD). Husk at y peger OPAD.
///
///   game.Add(new Player { Speed = 300, Color = Color.Blue, Name = "Mig" });
///   game.Add(new Player { Sprite = "helt.png", Size = 60, MaxLives = 5 });
///
/// Knapper:  Mellemrum = skub ting vaek     Venstre shift = dash
///
/// Spilleren kender ikke en eneste anden komponent ved navn. Den kigger kun efter
/// EVNER: kan det her samles op (ICollectable)? goer det skade (IHarmful)? kan det
/// skubbes (IPushable)? Derfor virker den ogsaa sammen med dine egne komponenter.
/// </summary>
public class Player : Component, IDamageable
{
    public override string? Credits => Author;

    /// <summary>Skriv dit navn her, saa staar det nederst paa skaermen naar spillet koerer.</summary>
    public string? Author { get; set; }

    /// <summary>
    /// Navnet paa den spiller over netvaerket der styrer denne figur.
    /// null betyder tastaturet her paa maskinen.
    /// </summary>
    public string? ControlledBy { get; set; }

    public float Speed { get; set; } = 250f;

    /// <summary>Hvor stor spilleren er, maalt paa tvaers.</summary>
    public float Size { get; set; } = 40f;

    public Color Color { get; set; } = Color.SkyBlue;

    /// <summary>Valgfrit filnavn paa et billede. Uden det tegnes en simpel figur.</summary>
    public string? Sprite { get; set; }

    /// <summary>Navnet der vises over spilleren. Tom tekst = intet navn.</summary>
    public string Name { get; set; } = "";

    public int MaxLives { get; set; } = 3;

    /// <summary>Holder spilleren indenfor det kameraet kan se.</summary>
    public bool ConstrainToView { get; set; } = true;

    /// <summary>Viser hjerter i hjoernet. Slaa fra hvis der er flere spillere paa skaermen.</summary>
    public bool ShowLives { get; set; } = true;

    /// <summary>Sender beskeden GameOver ud naar spilleren doer.</summary>
    public bool EndsGameOnDeath { get; set; } = true;

    /// <summary>Sekunder man ikke kan tage skade efter et hit.</summary>
    public float InvulnerableAfterHit { get; set; } = 1f;

    public float DashSpeed { get; set; } = 700f;
    public float DashDuration { get; set; } = 0.15f;
    public float DashCooldown { get; set; } = 1f;

    /// <summary>Hvor langt vaek skubbet raekker. 0 slaar skub-evnen fra.</summary>
    public float PushRadius { get; set; } = 120f;
    public float PushCooldown { get; set; } = 2f;

    public int Lives { get; private set; }
    public bool Alive { get; private set; } = true;
    public bool IsDashing => _dashTimer > 0f;

    public override string? NetworkKind => "spiller";

    private Vector3 _startPosition;
    private bool _facingRight = true;
    private float _walkTimer;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector2 _dashDirection = Vector2.UnitX;
    private float _pushCooldownTimer;
    private float _pushRingTimer;
    private float _invulnerableTimer;

    // Render og RenderUI faar ikke noget context med - de skal bare tegne. Har man
    // alligevel brug for det, gemmer man det her, naar man faar det i OnAdded.
    private GameContext? _context;

    public override void OnAdded(GameContext context)
    {
        _context = context;
        _startPosition = Position;
        Lives = MaxLives;
        Collider ??= Collider.Circle(Size / 2f);
        Tags.Add("spiller");

        context.On<GameStarted>(_ => Reset());
        context.On<Healed>(healed =>
        {
            if (ReferenceEquals(healed.Target, this)) Lives = Math.Min(Lives + healed.Amount, MaxLives);
        });
    }

    /// <summary>Stiller spilleren tilbage til start med fulde liv.</summary>
    public void Reset()
    {
        Position = _startPosition;
        Lives = MaxLives;
        Alive = true;
        _dashTimer = 0f;
        _dashCooldownTimer = 0f;
        _pushCooldownTimer = 0f;
        _invulnerableTimer = 0f;
    }

    public override void Update(GameContext context)
    {
        float delta = context.DeltaTime;

        Tick(ref _dashCooldownTimer, delta);
        Tick(ref _pushCooldownTimer, delta);
        Tick(ref _pushRingTimer, delta);
        Tick(ref _invulnerableTimer, delta);

        if (!Alive) return;

        var input = context.InputFor(ControlledBy);
        var direction = input.Direction;

        if (direction.X != 0) _facingRight = direction.X > 0;

        if (_dashTimer > 0f)
        {
            _dashTimer -= delta;
            Position += new Vector3(_dashDirection.X, _dashDirection.Y, 0) * DashSpeed * delta;
            if (_dashTimer <= 0f) _dashCooldownTimer = DashCooldown;
        }
        else if (input.B && _dashCooldownTimer <= 0f && DashDuration > 0f)
        {
            _dashDirection = direction != Vector2.Zero ? direction : (_facingRight ? Vector2.UnitX : -Vector2.UnitX);
            _dashTimer = DashDuration;
        }
        else if (direction != Vector2.Zero)
        {
            Position += new Vector3(direction.X, direction.Y, 0) * Speed * delta;
            _walkTimer += delta * 8f;
        }

        if (input.A && PushRadius > 0f && _pushCooldownTimer <= 0f) Push(context);
        if (ConstrainToView) ClampToView(context);
    }

    private static void Tick(ref float timer, float delta)
    {
        if (timer > 0f) timer -= delta;
    }

    private void Push(GameContext context)
    {
        bool pushedSomething = false;

        foreach (var other in context.Find<Component>())
        {
            if (ReferenceEquals(other, this) || other is not IPushable pushable) continue;
            if (Vector3.Distance(Position, other.Position) > PushRadius) continue;

            pushable.PushAwayFrom(Position);
            pushedSomething = true;
        }

        _pushRingTimer = 0.4f;
        if (pushedSomething) _pushCooldownTimer = PushCooldown;
    }

    private void ClampToView(GameContext context)
    {
        float halfHeight = context.Camera.Height / 2f;
        float halfWidth = halfHeight * GetScreenWidth() / GetScreenHeight();
        var centre = context.Camera.Target;
        float margin = Size / 2f;

        Position = Position with
        {
            X = Math.Clamp(Position.X, centre.X - halfWidth + margin, centre.X + halfWidth - margin),
            Y = Math.Clamp(Position.Y, centre.Y - halfHeight + margin, centre.Y + halfHeight - margin)
        };
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (!Alive) return;

        if (other is ICollectable collectable)
            collectable.OnCollected(this, context);

        if (other is IHarmful harmful && !IsDashing && _invulnerableTimer <= 0f)
            TakeDamage(harmful.Damage, other, context);
    }

    public void TakeDamage(int amount, Component? source, GameContext context)
    {
        if (!Alive || amount <= 0) return;

        Lives -= amount;
        _invulnerableTimer = InvulnerableAfterHit;
        context.Publish(new Damaged(this, amount, source));

        if (Lives > 0) return;

        Lives = 0;
        Alive = false;
        context.Publish(new Died(this));
        if (EndsGameOnDeath) context.Publish(new GameOver());
    }

    void IDamageable.TakeDamage(int amount, Component source, GameContext context)
        => TakeDamage(amount, source, context);

    public override void Render()
    {
        if (_pushRingTimer > 0f)
        {
            float progress = 1f - _pushRingTimer / 0.4f;
            var glow = new Color((byte)255, (byte)220, (byte)50, (byte)(160 * (1f - progress)));
            Draw.Ball(Position, PushRadius * progress, glow);
        }

        if (IsDashing)
            Draw.Ball(Position, Size, new Color((byte)80, (byte)180, (byte)255, (byte)(140 * (_dashTimer / DashDuration))));

        // Blinker mens man er usaarlig lige efter et hit.
        if (_invulnerableTimer > 0f && (int)(_invulnerableTimer * 10f) % 2 == 0) return;

        if (Sprite != null)
        {
            Draw.Sprite(Sprite, Position, Size, Alive ? Color.White : Color.Gray);
            return;
        }

        float unit = Size / 4f;
        float flip = _facingRight ? 1f : -1f;
        float bounce = MathF.Abs(MathF.Sin(_walkTimer)) * unit * 0.25f;
        var body = Position + new Vector3(0, bounce, 0);

        var shirt = Alive ? Color : Color.Gray;
        var skin = Alive ? new Color(255, 214, 170, 255) : Color.LightGray;
        var trousers = Alive ? Color.DarkBlue : Color.Gray;

        Draw.Cube(body + new Vector3(-unit * 0.6f, -unit * 1.6f, 0), new Vector3(unit * 0.6f, unit, unit * 0.6f), trousers);
        Draw.Cube(body + new Vector3(unit * 0.6f, -unit * 1.6f, 0), new Vector3(unit * 0.6f, unit, unit * 0.6f), trousers);
        Draw.Cube(body, new Vector3(unit * 2f, unit * 2.4f, unit), shirt);
        Draw.Ball(body + new Vector3(0, unit * 1.9f, 0), unit * 0.9f, skin);
        Draw.Cube(body + new Vector3(unit * 0.55f * flip, unit * 2.1f, unit * 0.75f),
            new Vector3(unit * 0.55f, unit * 0.25f, unit * 0.1f), Alive ? Color.Black : Color.Red);
    }

    public override void RenderUI()
    {
        if (!string.IsNullOrEmpty(Name))
            Draw.TextAbove(Name, Position, Size, 16, Color.Black);

        // Hjerterne i hjoernet er MINE. Paa en klient tegner vi dem kun for den figur
        // serveren har sagt er vores, ellers ville alle spilleres liv ligge oveni hinanden.
        if (!ShowLives) return;
        if (IsRemote && !ReferenceEquals(_context?.MyEntity, this)) return;

        for (int i = 0; i < Lives; i++)
            DrawHeart(new Vector2(GetScreenWidth() - 30 - i * 34, 60), 26, Color.Red);
    }

    private static void DrawHeart(Vector2 centre, float size, Color color)
    {
        DrawCircleV(centre + new Vector2(-size * 0.22f, -size * 0.18f), size * 0.3f, color);
        DrawCircleV(centre + new Vector2(size * 0.22f, -size * 0.18f), size * 0.3f, color);
        DrawTriangle(
            centre + new Vector2(-size * 0.5f, -size * 0.1f),
            centre + new Vector2(0, size * 0.5f),
            centre + new Vector2(size * 0.5f, -size * 0.1f),
            color);
    }

    public override void WriteState(StateWriter state)
    {
        state.Text(Name);
        state.Colour(Color);
        state.Number(Size);
        state.Whole(Lives);
        state.Flag(Alive);
        state.Flag(_facingRight);
        state.Text(Sprite);
    }

    public override void ReadState(StateReader state)
    {
        Name = state.Text();
        Color = state.Colour();
        Size = state.Number();
        Lives = state.Whole();
        Alive = state.Flag();
        _facingRight = state.Flag();
        string sprite = state.Text();
        Sprite = sprite.Length > 0 ? sprite : null;
    }
}
