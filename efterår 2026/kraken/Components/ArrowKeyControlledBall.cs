using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// Den simplest mulige figur: en bold der koeres rundt med piletasterne.
/// God til at komme hurtigt i gang, og god at kigge paa naar man vil se hvor lidt
/// der egentlig skal til for at lave en komponent.
///
///   game.Add(new ArrowKeyControlledBall { Speed = 300, Color = Color.Lime });
/// </summary>
public class ArrowKeyControlledBall : Component
{
    public float Speed { get; set; } = 300f;
    public float Radius { get; set; } = 20f;
    public Color Color { get; set; } = Color.Blue;
    public string Name { get; set; } = "";

    /// <summary>Spillernavnet der styrer bolden over netvaerket. null = tastaturet her.</summary>
    public string? ControlledBy { get; set; }

    public override string? NetworkKind => "bold";

    public override void OnAdded(GameContext context) => Collider ??= Collider.Circle(Radius);

    public override void Update(GameContext context)
    {
        var direction = context.InputFor(ControlledBy).Direction;
        Position += new Vector3(direction.X, direction.Y, 0) * Speed * context.DeltaTime;
    }

    public override void Render() => Draw.Ball(Position, Radius, Color);

    public override void RenderUI()
    {
        if (Name.Length > 0) Draw.TextAbove(Name, Position, Radius + 10, 16, Color.Black);
    }

    public override void WriteState(StateWriter state)
    {
        state.Number(Radius);
        state.Colour(Color);
        state.Text(Name);
    }

    public override void ReadState(StateReader state)
    {
        Radius = state.Number();
        Color = state.Colour();
        Name = state.Text();
    }
}
