using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class FallingCoin : IComponent
{
    private Vector2 _position;
    private readonly float _speed;
    private readonly float _radius;
    private float _animTimer;

    public FallingCoin(Vector2 position, float speed, float radius = 10f)
    {
        _position = position;
        _speed = speed;
        _radius = radius;
    }

    public void Update(UpdateContext context)
    {
        if (!WinCondition.HasWon)
        {
            context.RemoveComponent(this);
            return;
        }

        _animTimer += GetFrameTime();
        _position.Y += _speed * GetFrameTime();

        if (_position.Y - _radius > GetScreenHeight())
            context.RemoveComponent(this);
    }

    public void Render()
    {
        float pulse = 1f + 0.15f * (float)Math.Sin(_animTimer * 4f);
        float glowRadius = _radius * pulse;

        DrawCircleV(_position, glowRadius + 2f, new Color(255, 220, 50, 80));
        DrawCircleV(_position, glowRadius, Color.Gold);
        DrawCircleV(_position + new Vector2(-_radius * 0.2f, -_radius * 0.2f), glowRadius * 0.4f, Color.Yellow);
    }
}
