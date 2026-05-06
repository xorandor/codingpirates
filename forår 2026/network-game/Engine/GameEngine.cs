using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

namespace Engine;

public class GameEngine
{
    private readonly List<IComponent> _components = [];
    private readonly List<IComponent> _pendingAdd = [];
    private readonly List<IComponent> _pendingRemove = [];
    private readonly Color _backgroundColor;
    private readonly GameMode _mode;
    private readonly Networking _networking = new();

    public IEnumerable<IComponent> Components => _components.Except(_pendingRemove);

    public GameEngine(Color backgroundColor, GameMode mode)
    {
        _backgroundColor = backgroundColor;
        _mode = mode;
    }

    public T Add<T>(T component) where T : IComponent
    {
        _pendingAdd.Add(component);
        return component;
    }

    public void Remove(IComponent component)
    {
        _pendingRemove.Add(component);
    }

    public void RemoveAll<T>() where T : IComponent
    {
        foreach (var component in _components.OfType<T>())
            _pendingRemove.Add(component);
    }

    public void Run()
    {
        if (_mode == GameMode.Server)
            _networking.Start();

        if (_mode == GameMode.Client)
        {
            var ip = RunIpEntryScreen();

            if (WindowShouldClose()) { CloseWindow(); return; }

            if (ip != null)
            {
                if (!_networking.TryConnect(ip))
                {
                    RunErrorScreen($"Could not connect to {ip}");
                    CloseWindow();
                    return;
                }

                var name = RunNameEntryScreen();

                if (WindowShouldClose()) { CloseWindow(); return; }

                _networking.SendJoined(name);
                _networking.StartListening();
            }
        }

        var context = new UpdateContext(_networking, _mode,
            add => _pendingAdd.Add(add),
            remove => _pendingRemove.Add(remove),
            () => _components);

        while (!WindowShouldClose())
        {
            foreach (var component in _components)
                component.Update(context);

            foreach (var component in _pendingAdd)
                _components.Add(component);
            _pendingAdd.Clear();

            var removing = _pendingRemove.ToList();
            _pendingRemove.Clear();
            foreach (var component in removing)
            {
                component.OnRemoved(context);
                _components.Remove(component);
            }

            BeginDrawing();
            ClearBackground(_backgroundColor);

            foreach (var component in _components)
                component.Render();

            DrawStatusText();
            DrawFPS(10, 10);

            if (_mode == GameMode.Server)
                DrawPlayerList();

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

        int fontSize = 40;
        int textWidth = MeasureText(text, fontSize);
        DrawText(text, GetScreenWidth() - textWidth - 10, 10, fontSize, color);
    }

    private void DrawPlayerList()
    {
        var players = _networking.ConnectedPlayers;
        int fontSize = 18;
        int lineHeight = fontSize + 4;
        int x = 10;
        int totalLines = 1 + players.Count;
        int startY = GetScreenHeight() - totalLines * lineHeight - 10;

        DrawText("Players:", x, startY, fontSize, Color.Black);
        for (int i = 0; i < players.Count; i++)
            DrawText(players[i], x, startY + (i + 1) * lineHeight, fontSize, Color.DarkGray);
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

    private string RunNameEntryScreen()
    {
        while (GetKeyPressed() != 0) { } // drain leftover keypresses

        var input = new System.Text.StringBuilder();

        while (!WindowShouldClose())
        {
            int key;
            while ((key = GetCharPressed()) != 0)
            {
                if (key >= 32 && key < 127)
                    input.Append((char)key);
            }

            if (IsKeyPressed(KeyboardKey.Backspace) && input.Length > 0)
                input.Remove(input.Length - 1, 1);

            if (IsKeyPressed(KeyboardKey.Enter) && input.Length > 0)
                return input.ToString();

            BeginDrawing();
            ClearBackground(_backgroundColor);

            DrawText("Enter your name:", 100, 160, 24, Color.Black);
            DrawText(input + "_", 100, 195, 24, Color.DarkGray);
            DrawText("Press Enter to confirm", 100, 240, 16, Color.Gray);

            EndDrawing();
        }

        return "";
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
    void Update(UpdateContext context);
    void Render();
    void OnRemoved(UpdateContext context) { }
}

public class UpdateContext
{
    public Networking Networking { get; }
    public GameMode Mode { get; }

    private readonly Action<IComponent> _add;
    private readonly Action<IComponent> _remove;
    private readonly Func<IEnumerable<IComponent>> _getComponents;

    public UpdateContext(Networking networking, GameMode mode, Action<IComponent> add, Action<IComponent> remove, Func<IEnumerable<IComponent>> getComponents)
    {
        Networking = networking;
        Mode = mode;
        _add = add;
        _remove = remove;
        _getComponents = getComponents;
    }

    public void AddComponent(IComponent component) => _add(component);
    public void RemoveComponent(IComponent component) => _remove(component);
    public void RemoveAllComponents<T>() where T : IComponent
    {
        foreach (var component in _getComponents().OfType<T>())
            _remove(component);
    }
    public IEnumerable<T> GetComponents<T>() => _getComponents().OfType<T>();
}

public enum GameMode
{
    Client,
    Server
}
