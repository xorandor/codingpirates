using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class PowerBuffExtraHealth : IComponent
{
    private readonly Vector2 _position;
    private readonly float _radius;
    private float _animTimer;

    public Vector2 Position => _position;
    public float Radius => _radius;

    public PowerBuffExtraHealth(Vector2 position, float radius = 20f)
    {
        _position = position;
        _radius = radius;
    }

    public void Update(UpdateContext context)
    {
        _animTimer += GetFrameTime();
    }

    public void Render()
    {
        float pulse = 1f + 0.1f * MathF.Sin(_animTimer * 4f);
        float r = _radius * pulse;

        // Ring rundt om hjertet
        DrawRing(_position, r * 0.85f, r * 0.95f, 0f, 360f, 32, Color.Pink);

        // Hjertet i midten
        DrawHeart(_position, r * 1.3f, Color.Red);
    }

    private static void DrawHeart(Vector2 center, float size, Color color)
    {
        float lobeRadius = size * 0.3f;
        var leftLobe = new Vector2(center.X - size * 0.22f, center.Y - size * 0.18f);
        var rightLobe = new Vector2(center.X + size * 0.22f, center.Y - size * 0.18f);
        DrawCircleV(leftLobe, lobeRadius, color);
        DrawCircleV(rightLobe, lobeRadius, color);

        // Nedadvendt trekant der danner bunden af hjertet (CCW vertex-rækkefølge)
        var topLeft = new Vector2(center.X - size * 0.5f, center.Y - size * 0.1f);
        var topRight = new Vector2(center.X + size * 0.5f, center.Y - size * 0.1f);
        var bottom = new Vector2(center.X, center.Y + size * 0.5f);
        DrawTriangle(topLeft, bottom, topRight, color);
    }
}
