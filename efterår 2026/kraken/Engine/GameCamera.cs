using System.Numerics;
using Raylib_cs;

namespace Kraken;

/// <summary>
/// Kameraet i Kraken er altid et 3D-kamera, men det staar som standard og kigger lige
/// ind i xy-planet med ortografisk projektion. Det betyder at et 2D-spil bare virker:
/// x gaar til hoejre, y gaar OPAD, og z er dit lag.
///
/// Skru paa Tilt hvis du vil bevise for dig selv at det faktisk er 3D.
/// </summary>
public class GameCamera
{
    /// <summary>Punktet kameraet kigger paa. Flyt det for at lade kameraet foelge en spiller.</summary>
    public Vector3 Target { get; set; } = Vector3.Zero;

    /// <summary>Hvor mange world units der er plads til i hoejden. Mindre tal = mere zoomet ind.</summary>
    public float Height { get; set; } = 720f;

    /// <summary>Grader. 0 = ren 2D-visning forfra. 45 = skraat oppefra. PrÃ¸v F3 i spillet.</summary>
    public float Tilt { get; set; }

    /// <summary>Grader. Drejer kameraet om y-aksen.</summary>
    public float Turn { get; set; }

    /// <summary>Afstand fra Target ud til kameraet. Har kun betydning naar Perspective er true.</summary>
    public float Distance { get; set; } = 400f;

    /// <summary>Saet til true for aegte perspektiv (ting langt vaek bliver mindre).</summary>
    public bool Perspective { get; set; }

    /// <summary>Synsvinkel i grader. Bruges kun naar Perspective er true.</summary>
    public float FieldOfView { get; set; } = 45f;

    internal Camera3D ToRaylib()
    {
        float tilt = Tilt * MathF.PI / 180f;
        float turn = Turn * MathF.PI / 180f;

        // Retning fra Target ud mod kameraet, og den tilhoerende op-vektor.
        var offset = new Vector3(
            MathF.Sin(turn) * MathF.Cos(tilt),
            MathF.Sin(tilt),
            MathF.Cos(turn) * MathF.Cos(tilt));

        var up = new Vector3(
            -MathF.Sin(turn) * MathF.Sin(tilt),
            MathF.Cos(tilt),
            -MathF.Cos(turn) * MathF.Sin(tilt));

        return new Camera3D
        {
            Position = Target + offset * Distance,
            Target = Target,
            Up = up,
            FovY = Perspective ? FieldOfView : Height,
            Projection = Perspective ? CameraProjection.Perspective : CameraProjection.Orthographic
        };
    }
}


