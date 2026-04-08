using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class Coin : IComponent
{
    private readonly Vector2 _position;
    private readonly float _radius;
    private float _animTimer;

    public Vector2 Position => _position;
    public float Radius => _radius;

    public Coin(Vector2 position, float radius = 10f)
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
        // Pulserende glow-effekt
        float pulse = 1f + 0.15f * (float)Math.Sin(_animTimer * 4f);
        float glowRadius = _radius * pulse;

        // Ydre glow
        DrawCircleV(_position, glowRadius + 2f, new Color(255, 220, 50, 80));
        // Mønt
        DrawCircleV(_position, glowRadius, Color.Gold);
        // Indre highlight
        DrawCircleV(_position + new Vector2(-_radius * 0.2f, -_radius * 0.2f), glowRadius * 0.4f, Color.Yellow);
    }
}
