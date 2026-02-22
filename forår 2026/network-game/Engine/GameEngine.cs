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

        if (_mode == GameMode.Client)
        {
            var ip = RunIpEntryScreen();

            if (WindowShouldClose())
            {
                CloseWindow();
                return;
            }

            if (ip != null && !_networking.TryConnect(ip))
            {
                RunErrorScreen($"Could not connect to {ip}");
                CloseWindow();
                return;
            }
        }

        while (!WindowShouldClose())
        {
            foreach (var component in _components)
                component.Update();

            BeginDrawing();
            ClearBackground(_backgroundColor);

            foreach (var component in _components)
                component.Render();

            DrawStatusText();

            EndDrawing();
        }

        if (_mode == GameMode.Server)
            _networking.Stop();

        CloseWindow();
    }

    private void DrawStatusText()
    {
        string text;
        Color color;

        if (_mode == GameMode.Server)
        {
            text = _networking.LocalIp.ToString();
            color = Color.DarkGray;
        }
        else
        {
            text = _networking.IsConnected ? $"Connected to: {_networking.ConnectedIp}" : "Not connected";
            color = _networking.IsConnected ? Color.Green : Color.DarkGray;
        }

        int fontSize = 20;
        int textWidth = MeasureText(text, fontSize);
        DrawText(text, GetScreenWidth() - textWidth - 10, 10, fontSize, color);
    }

    private string? RunIpEntryScreen()
    {
        var input = new System.Text.StringBuilder();

        while (!WindowShouldClose())
        {
            int key;
            while ((key = GetCharPressed()) != 0)
            {
                if (char.IsAsciiDigit((char)key) || key == '.')
                    input.Append((char)key);
            }

            if (IsKeyPressed(KeyboardKey.Backspace) && input.Length > 0)
                input.Remove(input.Length - 1, 1);

            if (IsKeyPressed(KeyboardKey.Enter))
                return input.Length > 0 ? input.ToString() : null;

            BeginDrawing();
            ClearBackground(_backgroundColor);

            DrawText("Enter server IP address:", 100, 160, 24, Color.Black);
            DrawText(input + "_", 100, 195, 24, Color.DarkGray);
            DrawText("Press Enter to connect, or Enter with empty field to skip", 100, 240, 16, Color.Gray);

            EndDrawing();
        }

        return null;
    }

    private void RunErrorScreen(string message)
    {
        while (GetKeyPressed() != 0) { } // drain leftover keypresses

        while (!WindowShouldClose())
        {
            if (GetKeyPressed() != 0)
                break;

            BeginDrawing();
            ClearBackground(_backgroundColor);

            DrawText(message, 100, 160, 24, Color.Red);
            DrawText("Press any key to exit", 100, 200, 16, Color.DarkGray);

            EndDrawing();
        }
    }
}

public interface IComponent
{
    void Update();
    void Render();
}
