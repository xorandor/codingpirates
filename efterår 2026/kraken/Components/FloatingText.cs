using System.Numerics;
using Kraken;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Components;

/// <summary>
/// Tekst der flyver rundt i verden og hopper tilbage naar den rammer kanten.
///
///   game.Add(new FloatingText { Text = "Saa er vi i gang!", AngleDegrees = 30, Speed = 200 });
///
/// Teksten lever i verden (den har en Position), men bliver tegnet i RenderUI - for
/// tekst er noget der hoerer til paa skaermen. Draw.TextAbove regner selv om.
/// </summary>
public class FloatingText : Component
{
    public string Text { get; set; } = "Hej!";

    /// <summary>Retningen den flyver i. 0 = mod hoejre, 90 = opad.</summary>
    public float AngleDegrees { get; set; } = 35f;

    public float Speed { get; set; } = 180f;
    public int FontSize { get; set; } = 24;
    public Color Color { get; set; } = Color.Black;

    private Vector2 _direction;

    public override void OnAdded(GameContext context)
    {
        float radians = AngleDegrees * MathF.PI / 180f;
        _direction = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
    }

    public override void Update(GameContext context)
    {
        Position += new Vector3(_direction.X, _direction.Y, 0) * Speed * context.DeltaTime;

        float halfHeight = context.Camera.Height / 2f;
        float halfWidth = halfHeight * GetScreenWidth() / GetScreenHeight();
        var centre = context.Camera.Target;

        // Lidt luft, saa teksten ikke naar at forsvinde ud over kanten foer den vender om.
        float margin = FontSize * Text.Length * 0.25f;

        if (Position.X < centre.X - halfWidth + margin || Position.X > centre.X + halfWidth - margin)
        {
            _direction.X = -_direction.X;
            Position = Position with { X = Math.Clamp(Position.X, centre.X - halfWidth + margin, centre.X + halfWidth - margin) };
        }

        if (Position.Y < centre.Y - halfHeight + FontSize || Position.Y > centre.Y + halfHeight - FontSize)
        {
            _direction.Y = -_direction.Y;
            Position = Position with { Y = Math.Clamp(Position.Y, centre.Y - halfHeight + FontSize, centre.Y + halfHeight - FontSize) };
        }
    }

    public override void RenderUI()
        => Draw.TextCentered(Text, Draw.ToScreen(Position), FontSize, Color);
}
