using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class PowerBuffCoinMagnet : IComponent
{
    private readonly Vector2 _position;
    private readonly float _radius;
    private readonly float _duration;
    private readonly float _pullRadius;

    public Vector2 Position => _position;
    public float Radius => _radius;
    public float Duration => _duration;
    public float PullRadius => _pullRadius;

    public PowerBuffCoinMagnet(Vector2 position, float duration = 5f, float pullRadius = 200f, float radius = 20f)
    {
        _position = position;
        _radius = radius;
        _duration = duration;
        _pullRadius = pullRadius;
    }

    public void Update(UpdateContext context)
    {
    }

    public void Render()
    {
        DrawMagnet(_position, _radius);
    }

    public static void DrawMagnet(Vector2 center, float size)
    {
        float w = size * 1.5f;
        float h = size * 1.6f;
        float t = size * 0.35f;
        float tipH = t * 0.6f;

        float left = center.X - w / 2f;
        float top = center.Y - h / 2f;
        float right = center.X + w / 2f;
        float bottom = center.Y + h / 2f;

        var red = Color.Red;
        var silver = Color.LightGray;

        // Toppen af hesteskoen
        DrawRectangleV(new Vector2(left, top), new Vector2(w, t), red);
        // Venstre ben (rød, indtil sølvspids)
        DrawRectangleV(new Vector2(left, top + t), new Vector2(t, h - t - tipH), red);
        // Højre ben (rød, indtil sølvspids)
        DrawRectangleV(new Vector2(right - t, top + t), new Vector2(t, h - t - tipH), red);
        // Venstre sølvspids
        DrawRectangleV(new Vector2(left, bottom - tipH), new Vector2(t, tipH), silver);
        // Højre sølvspids
        DrawRectangleV(new Vector2(right - t, bottom - tipH), new Vector2(t, tipH), silver);
    }
}
