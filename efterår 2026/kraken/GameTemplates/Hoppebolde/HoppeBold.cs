using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En bold der hopper rundt mellem banens fire kanter. Serveren ejer den og flytter den -
/// klienterne faar bare at vide hvor den er, og tegner den.
/// </summary>
public class HoppeBold : Component
{
    public Color Farve { get; set; } = Color.Gold;

    /// <summary>0 = tilfaeldig stoerrelse.</summary>
    public float Radius { get; set; }

    /// <summary>0 = tilfaeldig fart.</summary>
    public float Fart { get; set; }

    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    public override string? NetworkKind => "hoppebold";

    private Vector2 _retning;

    public override void OnAdded(GameContext context)
    {
        if (Radius <= 0) Radius = Random.Shared.Next(10, 24);
        if (Fart <= 0) Fart = Random.Shared.Next(160, 340);

        double vinkel = Random.Shared.NextDouble() * Math.Tau;
        _retning = new Vector2((float)Math.Cos(vinkel), (float)Math.Sin(vinkel));

        Collider ??= Collider.Circle(Radius);
        Tags.Add("hoppebold");
    }

    public override void Update(GameContext context)
    {
        Position += new Vector3(_retning.X, _retning.Y, 0) * Fart * context.DeltaTime;

        // Vend kun naar bolden er paa vej UD - ellers kan den saette sig fast i kanten.
        float kantX = BaneBredde / 2f - Radius;
        float kantY = BaneHoejde / 2f - Radius;

        if ((Position.X > kantX && _retning.X > 0) || (Position.X < -kantX && _retning.X < 0))
            _retning = _retning with { X = -_retning.X };
        if ((Position.Y > kantY && _retning.Y > 0) || (Position.Y < -kantY && _retning.Y < 0))
            _retning = _retning with { Y = -_retning.Y };
    }

    public override void Render() => Draw.Ball(Position, Radius, Farve);

    public override void WriteState(StateWriter state)
    {
        state.Colour(Farve);
        state.Number(Radius);
    }

    public override void ReadState(StateReader state)
    {
        Farve = state.Colour();
        Radius = state.Number();
    }
}
