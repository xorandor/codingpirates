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
    private readonly KeyboardKey? _shootKey;
    private readonly MouseButton? _shootButton;
    private readonly float? _autoShootInterval;
    private readonly int _maxBounces;

    private float _angle; // current angle in radians
    private float _autoShootTimer;

    public CircleShooter(Vector2 position, float radius = 10, float stickLength = 30, Color? color = null,
        float rotationSpeed = 3f, float bulletSpeed = 400f, float bulletRadius = 5f,
        KeyboardKey? shootKey = null, MouseButton? shootButton = null,
        float? autoShootIntervalMs = null, int maxBounces = 0)
    {
        _position = position;
        _radius = radius;
        _stickLength = stickLength;
        _color = color ?? Color.Black;
        _rotationSpeed = rotationSpeed;
        _bulletSpeed = bulletSpeed;
        _bulletRadius = bulletRadius;
        _shootKey = shootKey;
        _shootButton = shootButton;
        _autoShootInterval = autoShootIntervalMs / 1000f;
        _maxBounces = maxBounces;

        // Hvis hverken tast, museknap eller autoskyd er valgt, bruges Space som standard
        if (_shootKey == null && _shootButton == null && _autoShootInterval == null)
            _shootKey = KeyboardKey.Space;
    }

    private bool ShouldShoot()
    {
        if (_shootKey != null && IsKeyPressed(_shootKey.Value))
            return true;
        if (_shootButton != null && IsMouseButtonPressed(_shootButton.Value))
            return true;
        if (_autoShootInterval != null)
        {
            _autoShootTimer += GetFrameTime();
            if (_autoShootTimer >= _autoShootInterval.Value)
            {
                _autoShootTimer -= _autoShootInterval.Value;
                return true;
            }
        }
        return false;
    }

    public void Update(UpdateContext context)
    {
        _angle += _rotationSpeed * GetFrameTime();

        if (ShouldShoot())
        {
            Vector2 direction = new((float)Math.Cos(_angle), (float)Math.Sin(_angle));
            Vector2 spawnPosition = _position + direction * (_radius + _stickLength);
            context.AddComponent(new Bullet(spawnPosition, direction, _bulletSpeed, _bulletRadius, _color, _maxBounces));
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

    public class Bullet : IComponent
    {
        private Vector2 _position;
        private Vector2 _direction;
        private readonly float _speed;
        private readonly float _radius;
        private readonly Color _color;
        private readonly int _maxBounces;
        private int _bounceCount;

        public Vector2 Position => _position;
        public float Radius => _radius;

        public Bullet(Vector2 position, Vector2 direction, float speed, float radius, Color color, int maxBounces)
        {
            _position = position;
            _direction = direction;
            _speed = speed;
            _radius = radius;
            _color = color;
            _maxBounces = maxBounces;
        }

        public void Update(UpdateContext context)
        {
            _position += _direction * _speed * GetFrameTime();

            bool bounced = false;

            if (_position.X - _radius < 0 || _position.X + _radius > GetScreenWidth())
            {
                _direction.X = -_direction.X;
                _position.X = Math.Clamp(_position.X, _radius, GetScreenWidth() - _radius);
                bounced = true;
            }

            if (_position.Y - _radius < 0 || _position.Y + _radius > GetScreenHeight())
            {
                _direction.Y = -_direction.Y;
                _position.Y = Math.Clamp(_position.Y, _radius, GetScreenHeight() - _radius);
                bounced = true;
            }

            if (bounced && _maxBounces > 0 && ++_bounceCount >= _maxBounces)
                context.RemoveComponent(this);
        }

        public void Render()
        {
            DrawCircleV(_position, _radius, _color);
        }
    }
}
