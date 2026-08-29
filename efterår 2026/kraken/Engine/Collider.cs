using System.Numerics;

namespace Kraken;

/// <summary>
/// Formen motoren bruger til at opdage sammenstoed.
///
///   Collider = Collider.Circle(20);        // en cirkel med radius 20
///   Collider = Collider.Box(40, 60);       // en kasse der er 40 bred og 60 hoej
///
/// Kollision regnes ud i xy-planet - z bliver ignoreret. Det passer til de spil vi laver,
/// hvor alt staar i den samme flade, ogsaa selv om verden er 3D.
/// </summary>
public class Collider
{
    private readonly bool _isCircle;
    private readonly float _radius;
    private readonly Vector2 _size;
    private readonly Vector2 _offset;

    private Collider(bool isCircle, float radius, Vector2 size, Vector2 offset)
    {
        _isCircle = isCircle;
        _radius = radius;
        _size = size;
        _offset = offset;
    }

    /// <summary>En cirkel. offset flytter cirklen i forhold til komponentens Position.</summary>
    public static Collider Circle(float radius, Vector2 offset = default)
        => new(true, radius, Vector2.Zero, offset);

    /// <summary>En kasse maalt fra midten. offset flytter kassen i forhold til komponentens Position.</summary>
    public static Collider Box(float width, float height, Vector2 offset = default)
        => new(false, 0f, new Vector2(width, height), offset);

    private Vector2 Centre(Vector3 position) => new Vector2(position.X, position.Y) + _offset;

    internal static bool Overlaps(Collider a, Vector3 aPosition, Collider b, Vector3 bPosition)
    {
        Vector2 centreA = a.Centre(aPosition);
        Vector2 centreB = b.Centre(bPosition);

        if (a._isCircle && b._isCircle)
            return Vector2.DistanceSquared(centreA, centreB) <= (a._radius + b._radius) * (a._radius + b._radius);

        if (!a._isCircle && !b._isCircle)
            return MathF.Abs(centreA.X - centreB.X) * 2f <= a._size.X + b._size.X
                && MathF.Abs(centreA.Y - centreB.Y) * 2f <= a._size.Y + b._size.Y;

        return a._isCircle
            ? CircleTouchesBox(centreA, a._radius, centreB, b._size)
            : CircleTouchesBox(centreB, b._radius, centreA, a._size);
    }

    private static bool CircleTouchesBox(Vector2 circleCentre, float radius, Vector2 boxCentre, Vector2 boxSize)
    {
        // Naermeste punkt paa kassen, og saa er det bare en afstand.
        var half = boxSize / 2f;
        var closest = new Vector2(
            Math.Clamp(circleCentre.X, boxCentre.X - half.X, boxCentre.X + half.X),
            Math.Clamp(circleCentre.Y, boxCentre.Y - half.Y, boxCentre.Y + half.Y));

        return Vector2.DistanceSquared(circleCentre, closest) <= radius * radius;
    }
}
