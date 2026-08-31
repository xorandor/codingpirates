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

    /// <summary>Lyset. Saettes af motoren. Kugler, terninger og modeller lyses; sprites og linjer ikke.</summary>
    internal static Lighting Lighting = new();

    // ---------------------------------------------------------------- World

    /// <summary>Et fladt billede i verden. size er hoejden i world units; bredden foelger billedet.</summary>
    public static void Sprite(Texture2D texture, Vector3 position, float size, Color? tint = null)
        => Lighting.Unlit(() => DrawBillboard(Camera, texture, position, size, tint ?? Color.White));

    /// <summary>Som ovenfor, men henter billedet via Assets. Fx Draw.Sprite("coin.png", Position, 40).</summary>
    public static void Sprite(string textureName, Vector3 position, float size, Color? tint = null)
        => Sprite(Assets.Texture(textureName), position, size, tint);

    /// <summary>Et fladt billede med selvvalgt bredde og hoejde, og evt. drejet.</summary>
    public static void Sprite(Texture2D texture, Vector3 position, Vector2 size, float rotationDegrees = 0f, Color? tint = null)
        => Lighting.Unlit(() => DrawBillboardPro(
            Camera, texture,
            new Rectangle(0, 0, texture.Width, texture.Height),
            position, Vector3.UnitY, size, size / 2f,
            rotationDegrees, tint ?? Color.White));

    /// <summary>
    /// En kugle. Med standardkameraet og uden lys ser den ud som en almindelig fyldt cirkel.
    /// Med en Light i spillet faar den en lys side og en skyggeside.
    /// </summary>
    public static void Ball(Vector3 position, float radius, Color color)
    {
        var model = Assets.UnitSphere();
        Lighting.Prepare(ref model);
        DrawModel(model, position, radius, color);
        Lighting.AfterModel();
    }

    public static void Cube(Vector3 position, Vector3 size, Color color)
        => DrawCubeV(position, size, color);

    /// <summary>Et fyldt rektangel i xy-planet, med centrum i position.</summary>
    public static void Rectangle(Vector3 position, Vector2 size, Color color)
        => DrawCubeV(position, new Vector3(size.X, size.Y, 0.01f), color);

    public static void Line(Vector3 from, Vector3 to, Color color)
        => Lighting.Unlit(() => DrawLine3D(from, to, color));

    /// <summary>En ring (kun omridset) i xy-planet. God til effekter - et anslag, en rystelse.</summary>
    public static void Circle(Vector3 position, float radius, Color color)
        => Lighting.Unlit(() => DrawCircle3D(position, radius, Vector3.UnitZ, 0f, color));

    /// <summary>
    /// Tegn noget med din egen shader. Lyset saettes paa den foerst, hvis den bruger de samme
    /// uniform-navne som lys.fs - se Assets/shaders/glimt.fs for et eksempel man kan kopiere.
    ///   Draw.Shaded(minShader, () => Draw.Cube(Position, size, color));
    /// </summary>
    public static void Shaded(Shader shader, Action draw)
    {
        Lighting.Apply(shader);
        Lighting.With(shader, draw);
    }

    public static void Model(string modelName, Vector3 position, float scale = 1f, Color? tint = null)
    {
        var model = Assets.Model(modelName);
        Lighting.Prepare(ref model);
        DrawModel(model, position, scale, tint ?? Color.White);
        Lighting.AfterModel();
    }

    /// <summary>En model drejet et antal grader om en akse. Fx Vector3.UnitY for at snurre rundt.</summary>
    public static void Model(string modelName, Vector3 position, Vector3 axis, float degrees, float scale = 1f, Color? tint = null)
    {
        var model = Assets.Model(modelName);
        Lighting.Prepare(ref model);
        DrawModelEx(model, position, axis, degrees, new Vector3(scale), tint ?? Color.White);
        Lighting.AfterModel();
    }

    /// <summary>
    /// En model med en samlet drejning. Brug den naar drejningen skal bygges op over tid,
    /// fx en bold der ruller: rotation = Quaternion.Concatenate(rotation, Quaternion.CreateFromAxisAngle(akse, vinkel)).
    /// </summary>
    public static void Model(string modelName, Vector3 position, Quaternion rotation, float scale = 1f, Color? tint = null)
    {
        var model = Assets.Model(modelName);
        model.Transform = Matrix4x4.CreateFromQuaternion(rotation);
        Lighting.Prepare(ref model);
        DrawModel(model, position, scale, tint ?? Color.White);
        Lighting.AfterModel();
    }

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
