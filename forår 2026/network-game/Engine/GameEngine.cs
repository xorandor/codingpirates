using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine;

public class GameEngine
{
    private readonly List<IComponent> _components = [];
    private readonly Color _backgroundColor;
    private readonly GameMode _mode;
    private readonly Networking _networking = new();

    public GameEngine(Color backgroundColor, GameMode mode)
    {
        _backgroundColor = backgroundColor;
        _mode = mode;
    }

    public void Add(IComponent component)
    {
        _components.Add(component);
    }

    public void Run()
    {
        if (_mode == GameMode.Server)
            _networking.Start();

        var ipText = _networking.LocalIp.ToString();

        while (!WindowShouldClose())
        {
            foreach (var component in _components)
                component.Update();

            BeginDrawing();
            ClearBackground(_backgroundColor);

            foreach (var component in _components)
                component.Render();

            if (_mode == GameMode.Server)
            {
                int fontSize = 20;
                int textWidth = MeasureText(ipText, fontSize);
                DrawText(ipText, GetScreenWidth() - textWidth - 10, 10, fontSize, Color.DarkGray);
            }

            EndDrawing();
        }

        if (_mode == GameMode.Server)
            _networking.Stop();

        CloseWindow();
    }
}

public interface IComponent
{
    void Update();
    void Render();
}
