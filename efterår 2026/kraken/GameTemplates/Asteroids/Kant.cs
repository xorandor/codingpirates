using System.Numerics;

namespace Mine;

/// <summary>
/// Banens kanter. Alt der flyver ud paa den ene side, kommer ind paa den modsatte - som i
/// det gamle Asteroids. margen er hvor langt ud over kanten noget maa komme, foer det hopper
/// over (brug tingens radius, saa den er helt ude af syne naar den hopper).
/// </summary>
public static class Kant
{
    public static Vector3 Wrap(Vector3 position, float baneBredde, float baneHoejde, float margen)
    {
        float kantX = baneBredde / 2f + margen;
        float kantY = baneHoejde / 2f + margen;

        if (position.X > kantX) position.X = -kantX;
        else if (position.X < -kantX) position.X = kantX;

        if (position.Y > kantY) position.Y = -kantY;
        else if (position.Y < -kantY) position.Y = kantY;

        return position;
    }

    /// <summary>Er positionen helt uden for banen (plus margen)?</summary>
    public static bool Udenfor(Vector3 position, float baneBredde, float baneHoejde, float margen)
        => MathF.Abs(position.X) > baneBredde / 2f + margen || MathF.Abs(position.Y) > baneHoejde / 2f + margen;
}
