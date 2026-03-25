using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class CircleShooter : IComponent
{
    private readonly Vector2 _position;
    private readonly float _radius;
    private readonly float _stickLength;
    private readonly Color _color;
    private readonly float _rotationSpeed; // radians per second
    private readonly float _bulletSpeed;
    private readonly float _bulletRadius;

    private float _angle; // current angle in radians

    public CircleShooter(Vector2 position, float radius, float stickLength, Color color,
        float rotationSpeed = 3f, float bulletSpeed = 400f, float bulletRadius = 5f)
    {
        _position = position;
        _radius = radius;
        _stickLength = stickLength;
        _color = color;
        _rotationSpeed = rotationSpeed;
        _bulletSpeed = bulletSpeed;
        _bulletRadius = bulletRadius;
    }

    public void Update(UpdateContext context)
    {
        _angle += _rotationSpeed * GetFrameTime();

        if (IsKeyPressed(KeyboardKey.Space))
        {
            Vector2 direction = new((float)Math.Cos(_angle), (float)Math.Sin(_angle));
            Vector2 spawnPosition = _position + direction * (_radius + _stickLength);
            context.AddComponent(new Bullet(spawnPosition, direction, _bulletSpeed, _bulletRadius, _color));
        }
    }

    public void Render()
    {
        DrawCircleV(_position, _radius, _color);

        Vector2 direction = new((float)Math.Cos(_angle), (float)Math.Sin(_angle));
        Vector2 stickEnd = _position + direction * (_radius + _stickLength);
        Vector2 stickStart = _position + direction * _radius;
        DrawLineEx(stickStart, stickEnd, 3f, _color);
    }

    private class Bullet : IComponent
    {
        private Vector2 _position;
        private readonly Vector2 _direction;
        private readonly float _speed;
        private readonly float _radius;
        private readonly Color _color;

        public Bullet(Vector2 position, Vector2 direction, float speed, float radius, Color color)
        {
            _position = position;
            _direction = direction;
            _speed = speed;
            _radius = radius;
            _color = color;
        }

        public void Update(UpdateContext context)
        {
            _position += _direction * _speed * GetFrameTime();

            bool outOfBounds = _position.X < -50 || _position.X > GetScreenWidth() + 50
                            || _position.Y < -50 || _position.Y > GetScreenHeight() + 50;

            if (outOfBounds)
                context.RemoveComponent(this);
        }

        public void Render()
        {
            DrawCircleV(_position, _radius, _color);
        }
    }
}
