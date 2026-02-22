using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Engine;

public class GameLoop
{
    private readonly List<IComponent> _components = [];
    private readonly Color _backgroundColor;

    public GameLoop(Color backgroundColor)
    {
        _backgroundColor = backgroundColor;
    }

    public void Add(IComponent component)
    {
        _components.Add(component);
    }

    public void Run()
    {
        while (!WindowShouldClose())
        {
            foreach (var component in _components)
                component.Update();

            BeginDrawing();
            ClearBackground(_backgroundColor);

            foreach (var component in _components)
                component.Render();

            EndDrawing();
        }

        CloseWindow();
    }
}
