using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// En moent der falder ned og forsvinder naar den er ude af billedet. God til konfetti.
///
///   game.Add(new FallingCoin { Position = new(0, 400, 0), Speed = 250 });
///
/// Saet Collectable til true hvis man skal kunne samle den op undervejs.
/// </summary>
public class FallingCoin : Component, ICollectable
{
    public float Speed { get; set; } = 200f;
    public float Radius { get; set; } = 12f;
    public Color Color { get; set; } = Color.Gold;

    /// <summary>Skal man kunne samle den op? Er den false, er den ren pynt.</summary>
    public bool Collectable { get; set; }

    public int Value { get; set; } = 1;
    public string ScoreKey { get; set; } = "score";

    // Ren pynt behoever ikke over netvaerket - saa laver hver maskine bare sit eget regnvejr.
    // Kan man samle den op, er den en del af spillet, og saa er det serveren der bestemmer.
    public override string? NetworkKind => Collectable ? "faldende-moent" : null;

    private float _timer;
    private bool _taken;

    public override void OnAdded(GameContext context)
    {
        if (Collectable)
        {
            Collider ??= Collider.Circle(Radius);
            Tags.Add("moent");
        }
    }

    public override void Update(GameContext context)
    {
        _timer += context.DeltaTime;
        Position -= new Vector3(0, Speed * context.DeltaTime, 0);   // y peger opad, saa nedad er minus

        float bottom = context.Camera.Target.Y - context.Camera.Height / 2f;
        if (Position.Y + Radius < bottom) context.Remove(this);
    }

    public void OnCollected(Component collector, GameContext context)
    {
        if (!Collectable || _taken) return;
        _taken = true;

        context.State.Add(ScoreKey, Value);
        context.Publish(new Collected(collector, this, Value));
        context.Remove(this);
    }

    public override void Render()
    {
        float pulse = 1f + 0.15f * MathF.Sin(_timer * 4f);
        Draw.Ball(Position, Radius * pulse, Color);
    }

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
