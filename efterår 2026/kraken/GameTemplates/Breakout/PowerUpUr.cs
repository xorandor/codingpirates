using System.Numerics;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Tegner et lille nedtaellingsur - en cirkel der tommes med uret, som en timer paa en mobil.
/// Bruges af Bat (UdvidBat) og Bold (LangsomBold) til at vise hvor lang tid der er tilbage
/// af deres egen effekt. Ren tegning, ingen tilstand - ejeren holder selv styr paa andel.
/// </summary>
public static class PowerUpUr
{
    /// <summary>andel 1 = lige samlet op, 0 = ved at loebe ud.</summary>
    public static void Tegn(Vector2 skaermPosition, float andel, Color farve, float radius = 14f)
    {
        andel = Math.Clamp(andel, 0f, 1f);

        Raylib.DrawCircleSector(skaermPosition, radius, -90f, -90f + 360f * andel, 24, farve with { A = 150 });
        Raylib.DrawCircleLinesV(skaermPosition, radius, farve);
    }
}
