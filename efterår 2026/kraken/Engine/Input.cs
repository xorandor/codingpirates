using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Kraken;

/// <summary>
/// Hvad der bliver trykket paa lige nu. Der er seks knapper, som paa en simpel controller.
///
/// Pointen er, at det er den SAMME type uanset om trykket kommer fra tastaturet foran dig
/// eller fra en spiller ovre paa den anden side af netvaerket. Din komponent kan ikke se
/// forskel - og det er derfor det samme spil virker baade alene og online.
///
///   var input = context.InputFor(ControlledBy);
///   Position += new Vector3(input.Direction.X, input.Direction.Y, 0) * Speed * context.DeltaTime;
/// </summary>
public readonly struct Input
{
    public bool Up { get; init; }
    public bool Down { get; init; }
    public bool Left { get; init; }
    public bool Right { get; init; }

    /// <summary>Mellemrum paa tastaturet.</summary>
    public bool A { get; init; }

    /// <summary>Venstre shift paa tastaturet.</summary>
    public bool B { get; init; }

    /// <summary>Enter paa tastaturet.</summary>
    public bool Enter { get; init; }

    /// <summary>Retningen der trykkes i, som en vektor med laengde 1. Nul hvis der ikke trykkes.</summary>
    public Vector2 Direction
    {
        get
        {
            var direction = Vector2.Zero;
            if (Right) direction.X += 1;
            if (Left) direction.X -= 1;
            if (Up) direction.Y += 1;      // y peger opad
            if (Down) direction.Y -= 1;
            return direction == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(direction);
        }
    }

    public bool AnyDirection => Up || Down || Left || Right;

    internal static Input FromKeyboard() => new()
    {
        Up = IsKeyDown(KeyboardKey.Up) || IsKeyDown(KeyboardKey.W),
        Down = IsKeyDown(KeyboardKey.Down) || IsKeyDown(KeyboardKey.S),
        Left = IsKeyDown(KeyboardKey.Left) || IsKeyDown(KeyboardKey.A),
        Right = IsKeyDown(KeyboardKey.Right) || IsKeyDown(KeyboardKey.D),
        A = IsKeyDown(KeyboardKey.Space),
        B = IsKeyDown(KeyboardKey.LeftShift),
        Enter = IsKeyDown(KeyboardKey.Enter)
    };

    /// <summary>Pakker knapperne til syv tegn, saa de kan sendes over netvaerket.</summary>
    internal string Pack() =>
        new([Bit(Up), Bit(Down), Bit(Left), Bit(Right), Bit(A), Bit(B), Bit(Enter)]);

    private static char Bit(bool pressed) => pressed ? '1' : '0';

    internal static Input Unpack(string packed)
    {
        if (packed.Length < 7) return default;

        return new Input
        {
            Up = packed[0] == '1',
            Down = packed[1] == '1',
            Left = packed[2] == '1',
            Right = packed[3] == '1',
            A = packed[4] == '1',
            B = packed[5] == '1',
            Enter = packed[6] == '1'
        };
    }
}
