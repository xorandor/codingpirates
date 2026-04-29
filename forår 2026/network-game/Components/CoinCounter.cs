using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class CoinCounter : IComponent
{
    private readonly Vector2 _position;
    private readonly int _fontSize;
    private readonly Color _color;

    public int Count { get; private set; }

    public CoinCounter(Vector2 position, int fontSize = 30, Color? color = null)
    {
        _position = position;
        _fontSize = fontSize;
        _color = color ?? Color.Black;
    }

    public void Update(UpdateContext context)
    {
        Count = context.GetComponents<Coin>().Count();
    }

    public void Render()
    {
        DrawText($"Coins: {Count}", (int)_position.X, (int)_position.Y, _fontSize, _color);
    }
}
