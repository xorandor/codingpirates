using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Kraken;

/// <summary>
/// Tegneredskaber. Alt i "World"-delen skal kaldes fra Render(), alt i "Screen"-delen
/// fra RenderUI(). Bruger du raylib direkte, gaelder den samme regel:
/// 3D-funktioner (DrawSphere, DrawCube, DrawModel ...) hoerer til i Render(),
/// 2D-funktioner (DrawText, DrawRectangle ...) hoerer til i RenderUI().
/// </summary>
public static class Draw
{
    /// <summary>Det kamera der bruges lige nu. Saettes af motoren hver frame.</summary>
    internal static Camera3D Camera;

    // ---------------------------------------------------------------- World

    /// <summary>Et fladt billede i verden. size er hoejden i world units; bredden foelger billedet.</summary>
    public static void Sprite(Texture2D texture, Vector3 position, float size, Color? tint = null)
        => DrawBillboard(Camera, texture, position, size, tint ?? Color.White);

    /// <summary>Som ovenfor, men henter billedet via Assets. Fx Draw.Sprite("coin.png", Position, 40).</summary>
    public static void Sprite(string textureName, Vector3 position, float size, Color? tint = null)
        => Sprite(Assets.Texture(textureName), position, size, tint);

    /// <summary>Et fladt billede med selvvalgt bredde og hoejde, og evt. drejet.</summary>
    public static void Sprite(Texture2D texture, Vector3 position, Vector2 size, float rotationDegrees = 0f, Color? tint = null)
        => DrawBillboardPro(
            Camera, texture,
            new Rectangle(0, 0, texture.Width, texture.Height),
            position, Vector3.UnitY, size, size / 2f,
            rotationDegrees, tint ?? Color.White);

    /// <summary>En kugle. Med standardkameraet ser den ud som en almindelig fyldt cirkel.</summary>
    public static void Ball(Vector3 position, float radius, Color color)
        => DrawSphere(position, radius, color);

    public static void Cube(Vector3 position, Vector3 size, Color color)
        => DrawCubeV(position, size, color);

    /// <summary>Et fyldt rektangel i xy-planet, med centrum i position.</summary>
    public static void Rectangle(Vector3 position, Vector2 size, Color color)
        => DrawCubeV(position, new Vector3(size.X, size.Y, 0.01f), color);

    public static void Line(Vector3 from, Vector3 to, Color color)
        => DrawLine3D(from, to, color);

    public static void Model(string modelName, Vector3 position, float scale = 1f, Color? tint = null)
        => DrawModel(Assets.Model(modelName), position, scale, tint ?? Color.White);

    // --------------------------------------------------------------- Screen

    /// <summary>Regner en position i verden om til en position paa skaermen. Brug den i RenderUI().</summary>
    public static Vector2 ToScreen(Vector3 worldPosition)
        => GetWorldToScreen(worldPosition, Camera);

    /// <summary>Tekst paa skaermen. Kald fra RenderUI().</summary>
    public static void Text(string text, Vector2 screenPosition, int fontSize, Color color)
        => DrawTextEx(GetFontDefault(), text, screenPosition, fontSize, fontSize / 10f, color);

    /// <summary>Tekst centreret om et punkt paa skaermen. Kald fra RenderUI().</summary>
    public static void TextCentered(string text, Vector2 screenPosition, int fontSize, Color color)
    {
        int width = MeasureText(text, fontSize);
        Text(text, screenPosition - new Vector2(width / 2f, fontSize / 2f), fontSize, color);
    }

    /// <summary>Tekst der svaever over noget i verden - fx et spillernavn. Kald fra RenderUI().</summary>
    public static void TextAbove(string text, Vector3 worldPosition, float worldOffsetY, int fontSize, Color color)
        => TextCentered(text, ToScreen(worldPosition + new Vector3(0, worldOffsetY, 0)), fontSize, color);
}
