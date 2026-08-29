using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// En kanon der drejer rundt og skyder kugler ud.
///
///   game.Add(new CircleShooter { Position = new(0, 0, 0), AutoShootEverySeconds = 1f });
///   game.Add(new CircleShooter { ShootKey = KeyboardKey.Space, MaxBounces = 3 });
///
/// Kuglerne er egne komponenter (CircleShooter.Bullet). De goer skade paa alt der
/// kan tage skade, og kan skubbes vaek af en spiller.
/// </summary>
public class CircleShooter : Component
{
    public float Radius { get; set; } = 15f;
    public float BarrelLength { get; set; } = 35f;
    public Color Color { get; set; } = Color.DarkGray;

    /// <summary>Hvor hurtigt kanonen drejer, i radianer pr. sekund.</summary>
    public float RotationSpeed { get; set; } = 3f;

    public float BulletSpeed { get; set; } = 400f;
    public float BulletRadius { get; set; } = 8f;
    public Color BulletColor { get; set; } = Color.Maroon;

    /// <summary>Hvor mange gange en kugle maa hoppe paa kanten foer den forsvinder. 0 = uendeligt.</summary>
    public int MaxBounces { get; set; }

    /// <summary>Skyd naar der trykkes paa denne tast. null = ingen tast.</summary>
    public KeyboardKey? ShootKey { get; set; }

    /// <summary>Skyd helt af sig selv med faste mellemrum. null = kun paa tast.</summary>
    public float? AutoShootEverySeconds { get; set; }

    public override string? NetworkKind => "kanon";

    private float _angle;
    private TimerHandle? _autoShoot;

    public override void OnAdded(GameContext context)
    {
        // Hverken tast eller automatik valgt? Saa bruger vi mellemrum, saa der da sker noget.
        if (ShootKey == null && AutoShootEverySeconds == null)
            ShootKey = KeyboardKey.Space;

        if (AutoShootEverySeconds is > 0f)
            _autoShoot = context.Every(AutoShootEverySeconds.Value, () => Shoot(context));
    }

    public override void OnRemoved(GameContext context) => _autoShoot?.Cancel();

    public override void Update(GameContext context)
    {
        _angle += RotationSpeed * context.DeltaTime;

        if (ShootKey != null && IsKeyPressed(ShootKey.Value)) Shoot(context);
    }

    /// <summary>Skyder en kugle ud, lige der hvor roret peger hen.</summary>
    public void Shoot(GameContext context)
    {
        var direction = new Vector2(MathF.Cos(_angle), MathF.Sin(_angle));

        context.Add(new Bullet
        {
            Position = Position + new Vector3(direction.X, direction.Y, 0) * (Radius + BarrelLength),
            Direction = direction,
            Speed = BulletSpeed,
            Radius = BulletRadius,
            Color = BulletColor,
            MaxBounces = MaxBounces
        });
    }

    public override void Render()
    {
        Draw.Ball(Position, Radius, Color);

        var direction = new Vector3(MathF.Cos(_angle), MathF.Sin(_angle), 0);
        Draw.Line(Position + direction * Radius, Position + direction * (Radius + BarrelLength), Color);
        Draw.Ball(Position + direction * (Radius + BarrelLength), Radius * 0.35f, Color);
    }

    public override void WriteState(StateWriter state)
    {
        state.Number(Radius);
        state.Number(BarrelLength);
        state.Number(_angle);
        state.Colour(Color);
    }

    public override void ReadState(StateReader state)
    {
        Radius = state.Number();
        BarrelLength = state.Number();
        _angle = state.Number();
        Color = state.Colour();
    }

    /// <summary>En kugle fra kanonen. Kan ogsaa bruges alene.</summary>
    public class Bullet : Component, IHarmful, IPushable
    {
        public Vector2 Direction { get; set; } = Vector2.UnitX;
        public float Speed { get; set; } = 400f;
        public float Radius { get; set; } = 8f;
        public Color Color { get; set; } = Color.Maroon;
        public int MaxBounces { get; set; }
        public int Damage { get; set; } = 1;

        public override string? NetworkKind => "kugle";

        private int _bounces;

        public override void OnAdded(GameContext context)
        {
            Collider ??= Collider.Circle(Radius);
            Tags.Add("kugle");
        }

        public override void Update(GameContext context)
        {
            Position += new Vector3(Direction.X, Direction.Y, 0) * Speed * context.DeltaTime;

            float halfHeight = context.Camera.Height / 2f;
            float halfWidth = halfHeight * GetScreenWidth() / GetScreenHeight();
            var centre = context.Camera.Target;
            bool bounced = false;

            if (Position.X - Radius < centre.X - halfWidth || Position.X + Radius > centre.X + halfWidth)
            {
                Direction = Direction with { X = -Direction.X };
                Position = Position with { X = Math.Clamp(Position.X, centre.X - halfWidth + Radius, centre.X + halfWidth - Radius) };
                bounced = true;
            }

            if (Position.Y - Radius < centre.Y - halfHeight || Position.Y + Radius > centre.Y + halfHeight)
            {
                Direction = Direction with { Y = -Direction.Y };
                Position = Position with { Y = Math.Clamp(Position.Y, centre.Y - halfHeight + Radius, centre.Y + halfHeight - Radius) };
                bounced = true;
            }

            if (bounced && MaxBounces > 0 && ++_bounces >= MaxBounces)
                context.Remove(this);
        }

        public void PushAwayFrom(Vector3 source)
        {
            var away = new Vector2(Position.X - source.X, Position.Y - source.Y);
            if (away != Vector2.Zero) Direction = Vector2.Normalize(away);
        }

        public override void Render() => Draw.Ball(Position, Radius, Color);

        public override void WriteState(StateWriter state)
        {
            state.Number(Radius);
            state.Colour(Color);
        }

        public override void ReadState(StateReader state)
        {
            Radius = state.Number();
            Color = state.Colour();
        }
    }
}
