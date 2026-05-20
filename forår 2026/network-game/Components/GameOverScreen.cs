using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Components;

public class GameOverScreen : IComponent
{
    private readonly string _message;
    private readonly string _subtitle;
    private readonly Color _overlayColor;
    private readonly Color _messageColor;
    private readonly Color _subtitleColor;

    public bool IsVisible { get; private set; }

    public GameOverScreen(string message = "GAME OVER", string subtitle = "Tryk Escape for at afslutte",
        Color? overlayColor = null, Color? messageColor = null, Color? subtitleColor = null)
    {
        _message = message;
        _subtitle = subtitle;
        _overlayColor = overlayColor ?? new Color(0, 0, 0, 200);
        _messageColor = messageColor ?? Color.Red;
        _subtitleColor = subtitleColor ?? Color.LightGray;
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;

    public void Update(UpdateContext context) { }

    public void Render()
    {
        if (!IsVisible) return;

        int w = GetScreenWidth();
        int h = GetScreenHeight();

        DrawRectangle(0, 0, w, h, _overlayColor);

        int msgSize = 100;
        int msgWidth = MeasureText(_message, msgSize);
        DrawText(_message, (w - msgWidth) / 2, h / 2 - 90, msgSize, _messageColor);

        int subSize = 24;
        int subWidth = MeasureText(_subtitle, subSize);
        DrawText(_subtitle, (w - subWidth) / 2, h / 2 + 40, subSize, _subtitleColor);
    }
}
