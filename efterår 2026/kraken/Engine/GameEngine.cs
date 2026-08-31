using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Kraken;

/// <summary>
/// Selve motoren. Bygges i program.cs, fyldes med komponenter, og saettes i gang med Run().
///
///   var game = new GameEngine { Title = "Mit spil", Background = Color.RayWhite };
///   game.Add(new Player { Speed = 250 });
///   game.Run();
///
/// Verden er 3D, men kameraet staar som standard og kigger lige ind i xy-planet.
/// x gaar til hoejre, y gaar OPAD, og (0,0,0) er midt paa skaermen.
/// </summary>
public class GameEngine
{
    private readonly List<Component> _components = [];
    private readonly List<Component> _pendingAdd = [];
    private readonly List<Component> _pendingRemove = [];
    private readonly HashSet<Component> _removing = [];
    private readonly List<Component> _scratch = [];
    private readonly HashSet<string> _droppedKinds = [];

    private GameContext? _context;
    private Replication? _replication;

    private bool _debugView;
    private float _savedTilt;
    private float _savedTurn;

    /// <summary>Sekunder siden sidste frame, klippet til MaxDeltaTime. Det er den komponenterne faar.</summary>
    public float DeltaTime { get; private set; }

    /// <summary>Det stoerste tidsspring en frame maa give. Beskytter mod hak og mod den lange foerste frame.</summary>
    public float MaxDeltaTime { get; set; } = 0.1f;

    public string Title { get; set; } = "Kraken";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public Color Background { get; set; } = Color.RayWhite;
    public int TargetFps { get; set; } = 60;

    /// <summary>
    /// Hvor paa skaermen vinduet aabner. Lad den vaere null for at lade Windows bestemme.
    /// God naar du vil teste server og klient ved siden af hinanden paa den samme computer.
    /// </summary>
    public Vector2? WindowPosition { get; set; }

    /// <summary>Server er ogsaa single player. Klient kraever en server at forbinde til.</summary>
    public GameMode Mode { get; set; } = GameMode.Server;

    /// <summary>Navnet der sendes til serveren. Er det tomt, spoerger motoren om det ved opstart.</summary>
    public string PlayerName { get; set; } = "";

    /// <summary>Serverens IP. Er den tom, spoerger motoren om den ved opstart.</summary>
    public string ServerIp { get; set; } = "";

    public GameCamera Camera { get; } = new();

    /// <summary>Lyset i verden. Tomt = alt tegnes fladt som foer. Se komponenten Light.</summary>
    public Lighting Lighting { get; } = new();

    public Networking Networking { get; } = new();
    public EventBus Events { get; } = new();
    public GameState State { get; }

    /// <summary>Klientens opslagsbog over hvad serveren kan finde paa at sende.</summary>
    public NetworkKinds NetworkKinds { get; } = new();

    /// <summary>Hvor mange gange i sekundet serveren sender spillets tilstand ud.</summary>
    public float NetworkRate { get; set; } = 20f;

    internal Timers Timers { get; } = new();
    internal Input LocalInput { get; private set; }
    internal Component? LocalEntity => _replication?.LocalEntity;

    public IEnumerable<Component> Components => _components.Where(c => !_removing.Contains(c));

    public GameEngine() => State = new GameState(Events);

    public T Add<T>(T component) where T : Component
    {
        // Paa en klient er det serveren der ejer alt med et NetworkKind. Tilfoejer man
        // saadan en lokalt, ville man se den to gange - saa den bliver droppet med besked.
        if (Mode == GameMode.Client && component.NetworkKind != null && !component.IsRemote)
        {
            // Kun een besked pr. slags, ellers drukner konsollen hvis det sker hver frame.
            if (_droppedKinds.Add(component.NetworkKind))
                Console.WriteLine($"KLIENT: dropper {component.GetType().Name} ('{component.NetworkKind}') - den kommer fra serveren.");

            return component;
        }

        _pendingAdd.Add(component);
        return component;
    }

    public void Remove(Component component)
    {
        if (_removing.Add(component))
            _pendingRemove.Add(component);
    }

    public void RemoveAll<T>() where T : Component
    {
        foreach (var component in _components.OfType<T>().Cast<Component>().ToList())
            Remove(component);
    }

    /// <summary>Fjerner alt der ikke er markeret Persistent.</summary>
    public void RemoveAll()
    {
        foreach (var component in _components.Where(c => !c.Persistent).ToList())
            Remove(component);
    }

    public void Run()
    {
        SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        InitWindow(Width, Height, Title);
        SetTargetFPS(TargetFps);
        InitAudioDevice();

        // raylib klipper alt laengere vaek end 1000 units. Det er for lidt til en dyb baggrund
        // (stjerner, bjerge) naar kameraet selv staar 900 units ude i perspektiv.
        Rlgl.SetClipPlanes(1.0, 20000.0);

        if (WindowPosition is { } position)
            SetWindowPosition((int)position.X, (int)position.Y);

        if (Camera.Height <= 0) Camera.Height = Height;

        _context = new GameContext(this);
        _replication = new Replication(this) { Rate = NetworkRate };

        if (!Connect())
        {
            Shutdown();
            return;
        }

        while (!WindowShouldClose())
        {
            // Klippet, fordi den foerste frame taeller al opstart med (typisk over et sekund),
            // og alt der bevaeger sig ellers ville springe et sekunds vej paa een gang.
            DeltaTime = MathF.Min(GetFrameTime(), MaxDeltaTime);
            float deltaTime = DeltaTime;
            LocalInput = Input.FromKeyboard();

            HandleDebugView();

            bool blocked = _components.Any(c => c.IsBlocking && c.Enabled);

            if (Mode == GameMode.Server) _replication.ServerTick(deltaTime);
            else _replication.ClientTick(deltaTime, LocalInput);

            UpdateComponents(blocked);
            if (!blocked) DispatchCollisions();

            Timers.Tick(deltaTime, blocked);
            FlushPendingChanges();

            RenderFrame();
        }

        Shutdown();
    }

    private void UpdateComponents(bool blocked)
    {
        _scratch.Clear();
        _scratch.AddRange(_components);

        foreach (var component in _scratch)
        {
            if (!component.Enabled || component.IsRemote) continue;
            if (blocked && !component.RunsWhileBlocked) continue;

            _context!.Current = component;
            component.Update(_context);
        }

        _context!.Current = null;
    }

    /// <summary>
    /// Alle-mod-alle sammenligning af de komponenter der har en Collider. Det er ikke
    /// den smarteste maade, men den er let at forstaa og hurtig nok til vores spil.
    /// En komponent der styres af serveren faar aldrig OnCollision - det er serverens job.
    /// </summary>
    private void DispatchCollisions()
    {
        _scratch.Clear();
        foreach (var component in _components)
            if (component.Enabled && component.Collider != null && !_removing.Contains(component))
                _scratch.Add(component);

        for (int i = 0; i < _scratch.Count; i++)
        {
            var a = _scratch[i];

            for (int j = i + 1; j < _scratch.Count; j++)
            {
                var b = _scratch[j];

                if (_removing.Contains(a)) break;
                if (_removing.Contains(b)) continue;
                if (a.IsRemote && b.IsRemote) continue;
                if (!Collider.Overlaps(a.Collider!, a.Position, b.Collider!, b.Position)) continue;

                if (!a.IsRemote)
                {
                    _context!.Current = a;
                    a.OnCollision(b, _context);
                }

                if (!b.IsRemote && !_removing.Contains(b))
                {
                    _context!.Current = b;
                    b.OnCollision(a, _context);
                }
            }
        }

        _context!.Current = null;
    }

    /// <summary>
    /// Tegning bliver aldrig sat paa pause. En blokerende komponent (fx en startskaerm)
    /// stopper kun Update og kollision, saa verden staar stille bagved i stedet for at forsvinde.
    /// </summary>
    private void RenderFrame()
    {
        var camera = Camera.ToRaylib();
        Draw.Camera = camera;
        Draw.Lighting = Lighting;

        BeginDrawing();
        ClearBackground(Background);

        BeginMode3D(camera);
        Lighting.Begin(camera);

        // Tegn bagfra og frem, saa gennemsigtige sprites blander korrekt.
        // Det er derfor z er dit lag: hoejere z ligger oveni lavere z.
        _scratch.Clear();
        _scratch.AddRange(_components);
        _scratch.Sort((a, b) => a.Position.Z.CompareTo(b.Position.Z));

        foreach (var component in _scratch)
            if (component.Enabled) component.Render();

        if (_debugView) Lighting.Unlit(RenderDebugWorld);

        Lighting.End();
        EndMode3D();

        foreach (var component in _components)
            if (component.Enabled) component.RenderUI();

        RenderEngineUI();

        EndDrawing();
    }


    private void FlushPendingChanges()
    {
        // Loekke, fordi en komponents OnAdded godt maa tilfoeje flere komponenter.
        while (_pendingAdd.Count > 0)
        {
            var adding = _pendingAdd.ToList();
            _pendingAdd.Clear();

            foreach (var component in adding)
            {
                _components.Add(component);
                component.IsInGame = true;
                _replication!.OnComponentAdded(component);

                _context!.Current = component;
                component.OnAdded(_context);
            }

            _context!.Current = null;
        }

        if (_pendingRemove.Count == 0) return;

        var removing = _pendingRemove.ToList();
        _pendingRemove.Clear();

        foreach (var component in removing)
        {
            _removing.Remove(component);
            if (!_components.Remove(component)) continue;

            component.IsInGame = false;
            _replication!.OnComponentRemoved(component);

            _context!.Current = component;
            component.OnRemoved(_context);

            Events.UnsubscribeAll(component);
            Timers.CancelAllOwnedBy(component);
        }

        _context!.Current = null;
    }

    internal Input InputFor(string? playerName)
        => string.IsNullOrEmpty(playerName) ? LocalInput : _replication!.InputFor(playerName);

    /// <summary>Fortaeller en spiller hvilken komponent der er deres figur.</summary>
    public void ClaimForPlayer(string playerName, Component component)
        => _replication!.ClaimFor(playerName, component);

    private bool Connect()
    {
        if (Mode == GameMode.Server)
        {
            Networking.Start();
            FlushPendingChanges();
            return true;
        }

        string? ip = ServerIp.Length > 0
            ? ServerIp
            : RunTextEntryScreen("Skriv serverens IP-adresse:", "Enter for at forbinde", digitsAndDotsOnly: true);

        if (ip == null) return false;

        if (!Networking.TryConnect(ip))
        {
            RunErrorScreen($"Kunne ikke forbinde til {ip}");
            return false;
        }

        string? name = PlayerName.Length > 0
            ? PlayerName
            : RunTextEntryScreen("Skriv dit navn:", "Enter for at komme i gang");

        if (name == null) return false;

        PlayerName = name;
        Networking.SendJoined(name);
        Networking.StartListening();
        FlushPendingChanges();
        return true;
    }

    private void HandleDebugView()
    {
        if (!IsKeyPressed(KeyboardKey.F3)) return;

        _debugView = !_debugView;
        if (_debugView)
        {
            _savedTilt = Camera.Tilt;
            _savedTurn = Camera.Turn;
            Camera.Tilt = 35f;
            Camera.Turn = 25f;
        }
        else
        {
            Camera.Tilt = _savedTilt;
            Camera.Turn = _savedTurn;
        }
    }

    private void RenderDebugWorld()
    {
        float extent = Camera.Height;
        var faint = new Color(150, 150, 150, 70);

        const int slices = 20;
        float spacing = Camera.Height / slices;
        for (int i = -slices; i <= slices; i++)
        {
            float p = i * spacing;
            DrawLine3D(new Vector3(p, -extent, 0), new Vector3(p, extent, 0), faint);
            DrawLine3D(new Vector3(-extent, p, 0), new Vector3(extent, p, 0), faint);
        }

        DrawLine3D(new Vector3(-extent, 0, 0), new Vector3(extent, 0, 0), Color.Red);    // x
        DrawLine3D(new Vector3(0, -extent, 0), new Vector3(0, extent, 0), Color.Green);  // y (opad)
        DrawLine3D(new Vector3(0, 0, -extent), new Vector3(0, 0, extent), Color.Blue);   // z (mod kameraet)
    }

    private void RenderEngineUI()
    {
        DrawFPS(10, 10);
        RenderConnectionStatus();
        if (Mode == GameMode.Server) RenderPlayerList();
        RenderCredits();

        if (_debugView)
            Draw.Text("F3: debug-visning (roed = x, groen = y, blaa = z)", new Vector2(10, 34), 18, Color.DarkGray);
    }

    private void RenderConnectionStatus()
    {
        string text = Mode == GameMode.Server
            ? Networking.LocalIp.ToString()
            : Networking.IsConnected ? $"Forbundet til: {Networking.ConnectedIp}" : "Ikke forbundet";

        Color color = Mode == GameMode.Server ? Color.DarkGray
            : Networking.IsConnected ? Color.Green : Color.DarkGray;

        const int fontSize = 30;
        int width = MeasureText(text, fontSize);
        Draw.Text(text, new Vector2(GetScreenWidth() - width - 10, 10), fontSize, color);
    }

    private void RenderPlayerList()
    {
        var players = Networking.ConnectedPlayers;
        if (players.Count == 0) return;

        const int fontSize = 18;
        const int lineHeight = fontSize + 4;
        int startY = GetScreenHeight() - (players.Count + 1) * lineHeight - 40;

        Draw.Text("Spillere:", new Vector2(10, startY), fontSize, Color.Black);
        for (int i = 0; i < players.Count; i++)
            Draw.Text(players[i], new Vector2(10, startY + (i + 1) * lineHeight), fontSize, Color.DarkGray);
    }

    private void RenderCredits()
    {
        var credits = _components
            .Select(c => c.Credits)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        if (credits.Count == 0) return;

        const int fontSize = 18;
        Draw.Text($"Kodet af: {string.Join(", ", credits)}",
            new Vector2(10, GetScreenHeight() - fontSize - 10), fontSize, Color.DarkGray);
    }

    /// <summary>Returnerer null hvis brugeren lukker vinduet undervejs.</summary>
    private string? RunTextEntryScreen(string prompt, string hint, bool digitsAndDotsOnly = false)
    {
        while (GetKeyPressed() != 0) { } // toem gamle tastetryk
        var input = new System.Text.StringBuilder();

        while (!WindowShouldClose())
        {
            int key;
            while ((key = GetCharPressed()) != 0)
            {
                char c = (char)key;
                bool allowed = digitsAndDotsOnly ? char.IsAsciiDigit(c) || c == '.' : key is >= 32 and < 127;
                if (allowed) input.Append(c);
            }

            if (IsKeyPressed(KeyboardKey.Backspace) && input.Length > 0)
                input.Remove(input.Length - 1, 1);

            if (IsKeyPressed(KeyboardKey.Enter) && input.Length > 0)
                return input.ToString();

            BeginDrawing();
            ClearBackground(Background);
            Draw.Text(prompt, new Vector2(100, 160), 24, Color.Black);
            Draw.Text(input + "_", new Vector2(100, 200), 24, Color.DarkGray);
            Draw.Text(hint, new Vector2(100, 250), 16, Color.Gray);
            EndDrawing();
        }

        return null;
    }

    private void RunErrorScreen(string message)
    {
        while (GetKeyPressed() != 0) { }

        while (!WindowShouldClose())
        {
            if (GetKeyPressed() != 0) return;

            BeginDrawing();
            ClearBackground(Background);
            Draw.Text(message, new Vector2(100, 160), 24, Color.Red);
            Draw.Text("Tryk paa en tast for at afslutte", new Vector2(100, 200), 16, Color.DarkGray);
            EndDrawing();
        }
    }

    private void Shutdown()
    {
        if (Mode == GameMode.Server) Networking.Stop();
        Assets.UnloadAll();
        CloseAudioDevice();
        CloseWindow();
    }
}
