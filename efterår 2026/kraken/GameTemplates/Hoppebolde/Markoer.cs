using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En spillers markoer. Pilene flytter den, og Enter skyder en hoppebold ud fra den.
/// Al logikken koerer paa serveren - klienten sender kun sine tastetryk.
/// </summary>
public class Markoer : Component
{
    /// <summary>Navnet der staar over markoeren.</summary>
    public string Navn { get; set; } = "";

    /// <summary>Hvilken spillers tastetryk der styrer den. null = tastaturet paa serveren.</summary>
    public string? ControlledBy { get; set; }

    public Color Farve { get; set; } = Color.SkyBlue;
    public float Fart { get; set; } = 380f;
    public float Stoerrelse { get; set; } = 30f;
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    public override string? NetworkKind => "markoer";

    private bool _enterSidst;

    public override void OnAdded(GameContext context) => Tags.Add("markoer");

    public override void Update(GameContext context)
    {
        var input = context.InputFor(ControlledBy);

        Position += new Vector3(input.Direction.X, input.Direction.Y, 0) * Fart * context.DeltaTime;

        float kantX = BaneBredde / 2f - Stoerrelse / 2f;
        float kantY = BaneHoejde / 2f - Stoerrelse / 2f;
        Position = new Vector3(Math.Clamp(Position.X, -kantX, kantX), Math.Clamp(Position.Y, -kantY, kantY), 0);

        // En ny hoppebold - men kun paa selve trykket, ikke 60 gange i sekundet mens
        // tasten holdes nede. Derfor huskes hvad Enter var i sidste frame.
        if (input.Enter && !_enterSidst)
            context.Add(new HoppeBold { Position = Position, Farve = Farve });

        _enterSidst = input.Enter;
    }

    public override void Render()
        => Draw.Cube(Position, new Vector3(Stoerrelse, Stoerrelse, Stoerrelse), Farve);

    public override void RenderUI()
    {
        if (Navn.Length > 0)
            Draw.TextAbove(Navn, Position, Stoerrelse, 16, Color.RayWhite);
    }

    public override void WriteState(StateWriter state)
    {
        state.Text(Navn);
        state.Colour(Farve);
        state.Number(Stoerrelse);
    }

    public override void ReadState(StateReader state)
    {
        Navn = state.Text();
        Farve = state.Colour();
        Stoerrelse = state.Number();
    }
}
