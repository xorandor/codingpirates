using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
namespace Components;

public class StartScreen : IComponent
{
    private readonly string _title;
    private readonly string _subtitle;
    private readonly Color _overlayColor;
    private readonly Color _titleColor;
    private readonly Color _subtitleColor;
    private bool _done;

    public bool IsBlocking => !_done;

    public StartScreen(string title, string subtitle = "Tryk Enter eller Mellemrum for at starte",
        Color? overlayColor = null, Color? titleColor = null, Color? subtitleColor = null)
    {
        _title = title;
        _subtitle = subtitle;
        _overlayColor = overlayColor ?? new Color(0, 0, 0, 180);
        _titleColor = titleColor ?? Color.White;
        _subtitleColor = subtitleColor ?? Color.LightGray;
    }

    public void Update(UpdateContext context)
    {
        if (_done) return;

        if (IsKeyPressed(KeyboardKey.Enter) || IsKeyPressed(KeyboardKey.Space))
        {
            _done = true;
            context.RemoveComponent(this);
        }
    }

    public void Render()
    {
        if (_done) return;

        int w = GetScreenWidth();
        int h = GetScreenHeight();

        DrawRectangle(0, 0, w, h, _overlayColor);

        int titleSize = 80;
        int titleWidth = MeasureText(_title, titleSize);
        DrawText(_title, (w - titleWidth) / 2, h / 2 - 80, titleSize, _titleColor);

        int subSize = 24;
        int subWidth = MeasureText(_subtitle, subSize);
        DrawText(_subtitle, (w - subWidth) / 2, h / 2 + 40, subSize, _subtitleColor);
    }
}
