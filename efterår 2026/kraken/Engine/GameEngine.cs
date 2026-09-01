using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Kraken;

/// <summary>
/// Selve motoren. Bygges i program.cs, fyldes med komponenter, og saettes i gang med Run().
///
///   var game = new GameEngine { Title = "Mit spil", Background = Color.RayWhite };
///   game.Add(new StaticText { Text = "Mit spil" });
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
    private bool _showPlayers = true;
    private string _connectedTitle = "";
    private FoundGame? _valgtSpil;
    private readonly Discovery _discovery = new();
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

    /// <summary>
    /// true = spillet kan spilles over netvaerket: ved opstart ser spilleren listen af spil
    /// paa netvaerket, med "Start dit eget spil" oeverst. false (standard) = helt lokalt spil
    /// - motoren roerer slet ikke netvaerket, og ingen skaerme sporger om noget.
    /// </summary>
    public bool Network { get; set; }

    /// <summary>
    /// Fast rolle i netvaerkstilstand - springer startskaermen over. KUN til test
    /// (dotnet run -- klient 127.0.0.1 Bot). Lad den vaere null, saa sporger motoren spilleren.
    /// </summary>
    public GameMode? Mode { get; set; }

    /// <summary>Den rolle spillet faktisk koerer i. Afgjort ved opstart.</summary>
    internal GameMode ActiveMode { get; private set; } = GameMode.Server;

    /// <summary>
    /// Koden paa spillet. Paa en vaert: den klienter skal taste (tom = aabent spil, og den
    /// interaktive vaert-skaerm sporger selv). Paa en klient: koden der sendes med (til test).
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Spillerens eget navn - baade paa en klient og hos en vaert. Er det tomt, spoerger
    /// motoren om det ved opstart. Vaertens navn staar oeverst paa spillerlisten.
    /// </summary>
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
        // Tjekket for om en klient maa faa komponenten sker foerst i FlushPendingChanges -
        // for naar program.cs kalder Add, er rollen (vaert/klient) maaske ikke valgt endnu.
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

        if (!ResolveRole())
        {
            Shutdown();
            return;
        }

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
            if (IsKeyPressed(KeyboardKey.P)) _showPlayers = !_showPlayers;

            bool blocked = _components.Any(c => c.IsBlocking && c.Enabled);

            if (ActiveMode == GameMode.Server) _replication.ServerTick(deltaTime);
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
                // Paa en klient er det serveren der ejer alt med et NetworkKind. Tilfoejer man
                // saadan en lokalt, ville man se den to gange - saa den bliver droppet med besked.
                if (ActiveMode == GameMode.Client && component.NetworkKind != null && !component.IsRemote)
                {
                    // Kun een besked pr. slags, ellers drukner konsollen hvis det sker hver frame.
                    if (_droppedKinds.Add(component.NetworkKind))
                        Console.WriteLine($"KLIENT: dropper {component.GetType().Name} ('{component.NetworkKind}') - den kommer fra serveren.");

                    continue;
                }

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

    /// <summary>Alle der er med: vaerten selv foerst (hvis den har et navn), saa klienterne.</summary>
    internal List<string> PlayerRoster()
    {
        var navne = new List<string>();
        if (PlayerName.Length > 0) navne.Add(PlayerName);
        navne.AddRange(Networking.ConnectedPlayers);
        return navne;
    }

    /// <summary>Fortaeller en spiller hvilken komponent der er deres figur.</summary>
    public void ClaimForPlayer(string playerName, Component component)
        => _replication!.ClaimFor(playerName, component);

    /// <summary>
    /// Afgoer hvilken rolle spillet koerer i. Uden netvaerk: altid lokal vaert, ingen skaerme.
    /// Med netvaerk: startskaermen viser spillene paa netvaerket med "Start dit eget spil"
    /// oeverst. En interaktiv vaert bliver derefter spurgt om spillets navn, en eventuel
    /// kode og sit eget navn. Den faste rolle (Mode/ServerIp) er kun til test.
    /// </summary>
    private bool ResolveRole()
    {
        if (!Network)
        {
            ActiveMode = GameMode.Server;
            return true;
        }

        GameMode? role = Mode;
        if (role == null && ServerIp.Length > 0) role = GameMode.Client;   // test-genvejen

        bool interactive = role == null;

        if (role != GameMode.Server && ServerIp.Length == 0)
        {
            if (!RunStartScreen(medVaertsvalg: role == null, out _valgtSpil)) return false;
            role ??= _valgtSpil == null ? GameMode.Server : GameMode.Client;
        }

        ActiveMode = role!.Value;

        if (ActiveMode == GameMode.Server && interactive)
        {
            string? navn = RunTextEntryScreen($"Hvad skal spillet hedde paa netvaerket? (Enter = '{Title}')",
                "Det er navnet de andre finder dig paa", allowEmpty: true);
            if (navn == null) return false;
            if (navn.Length > 0) Title = navn;
            SetWindowTitle(Title);

            if (Password.Length == 0)   // en kode sat i program.cs bliver staaende
            {
                string? kode = RunTextEntryScreen("Saet en kode paa spillet (Enter = ingen kode):",
                    "Kun dem der kender koden kan vaere med", allowEmpty: true);
                if (kode == null) return false;
                Password = kode;
            }

            if (PlayerName.Length == 0)
            {
                string? mitNavn = RunTextEntryScreen("Skriv dit navn:", "Det staar paa spillerlisten");
                if (mitNavn == null) return false;
                PlayerName = mitNavn;
            }
        }

        return true;
    }

    private bool Connect()
    {
        if (!Network)
        {
            FlushPendingChanges();
            return true;
        }

        if (ActiveMode == GameMode.Server)
        {
            Networking.Password = Password;
            Networking.Start();
            _discovery.StartResponder(() => Title, () => Networking.ConnectedPlayers.Count, () => Password.Length > 0);
            FlushPendingChanges();
            return true;
        }

        // Klient. Testgenvejen (ServerIp sat) springer soegningen over - ellers er spillet
        // allerede valgt paa startskaermen, og der tastes kode hvis det har en.
        string ip;
        string navnPaaSpillet;

        if (ServerIp.Length > 0)
        {
            ip = ServerIp;
            navnPaaSpillet = ip;
        }
        else
        {
            if (_valgtSpil == null) return false;

            ip = _valgtSpil.Ip;
            navnPaaSpillet = _valgtSpil.Title;
            _connectedTitle = _valgtSpil.Title;

            if (_valgtSpil.Locked && Password.Length == 0)
            {
                string? kode = RunTextEntryScreen("Skriv koden til spillet:", "Vaerten kender koden");
                if (kode == null) return false;
                Password = kode;
            }
        }

        if (!Networking.TryConnect(ip))
        {
            RunErrorScreen($"Kunne ikke forbinde til {navnPaaSpillet}");
            return false;
        }

        string? name = PlayerName.Length > 0
            ? PlayerName
            : RunTextEntryScreen("Skriv dit navn:", "Enter for at komme i gang");

        if (name == null) return false;
        PlayerName = name;

        Networking.SendJoined(name, Password);
        Networking.StartListening();

        string? afvist = WaitForWelcome();
        if (afvist != null)
        {
            RunErrorScreen(afvist);
            return false;
        }

        FlushPendingChanges();
        return true;
    }

    /// <summary>Venter paa serverens WELCOME. null = velkommen; ellers en dansk fejlbesked.</summary>
    private string? WaitForWelcome()
    {
        double frist = GetTime() + 3.0;

        while (!WindowShouldClose() && GetTime() < frist)
        {
            if (Networking.TryConsumeMessage("WELCOME") != null) return null;

            var denied = Networking.TryConsumeMessage("DENIED");
            if (denied != null) return $"Du kom ikke med: {denied.Field(0)}";

            BeginDrawing();
            ClearBackground(Background);
            Draw.Text("Forbinder ...", new Vector2(100, 160), 24, Color.DarkGray);
            EndDrawing();
        }

        return "Serveren svarede ikke";
    }

    /// <summary>
    /// Startskaermen: soeger efter spil paa netvaerket og viser dem i en liste, med
    /// "Start dit eget spil" som oeverste valg. Ingen IP-adresser nogen steder - spil
    /// kendes paa navn og vaert. Returnerer false hvis vinduet lukkes undervejs;
    /// ellers ligger det valgte spil i valgt (null = spilleren vil selv vaere vaert).
    /// </summary>
    private bool RunStartScreen(bool medVaertsvalg, out FoundGame? valgt)
    {
        valgt = null;
        while (GetKeyPressed() != 0) { }
        using var soegning = new DiscoveryClient();
        int markeret = 0;
        bool klar = false;   // et Enter fra skaermen foer maa ikke taelle med

        while (!WindowShouldClose())
        {
            soegning.Tick(GetTime());
            var spil = soegning.Games;
            int raekker = spil.Count + (medVaertsvalg ? 1 : 0);

            if (IsKeyPressed(KeyboardKey.Up) || IsKeyPressed(KeyboardKey.W)) markeret--;
            if (IsKeyPressed(KeyboardKey.Down) || IsKeyPressed(KeyboardKey.S)) markeret++;
            markeret = Math.Clamp(markeret, 0, Math.Max(raekker - 1, 0));

            if (klar && IsKeyPressed(KeyboardKey.Enter) && raekker > 0)
            {
                int spilIndex = medVaertsvalg ? markeret - 1 : markeret;
                if (spilIndex < 0) return true;   // "Start dit eget spil"
                valgt = spil[spilIndex];
                return true;
            }

            BeginDrawing();
            ClearBackground(Background);

            float y = 120;
            if (medVaertsvalg)
            {
                bool er = markeret == 0;
                Draw.Text((er ? "> " : "  ") + "Start dit eget spil", new Vector2(100, y), 24,
                    er ? Color.DarkBlue : Color.DarkGray);
                y += 64;
            }

            Draw.Text("Spil paa netvaerket:", new Vector2(100, y), 24, Color.Black);
            y += 50;

            if (spil.Count == 0)
                Draw.Text("Ingen spil fundet endnu ...", new Vector2(100, y), 20, Color.DarkGray);

            for (int i = 0; i < spil.Count; i++)
            {
                var fundet = spil[i];
                bool er = (medVaertsvalg ? i + 1 : i) == markeret;
                string raekke = $"{fundet.Title}   hos {fundet.Host}   {fundet.Players} spillere{(fundet.Locked ? "   (kode)" : "")}";
                Draw.Text((er ? "> " : "  ") + raekke, new Vector2(100, y + i * 30), 20,
                    er ? Color.DarkBlue : Color.DarkGray);
            }

            Draw.Text("Pil op/ned, Enter for at vaelge", new Vector2(100, GetScreenHeight() - 60f), 16, Color.Gray);
            EndDrawing();
            klar = true;
        }

        return false;
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
        RenderPlayerList();
        RenderCredits();

        if (_debugView)
            Draw.Text("F3: debug-visning (roed = x, groen = y, blaa = z)", new Vector2(10, 34), 18, Color.DarkGray);
    }

    private void RenderConnectionStatus()
    {
        // Ingen IP-adresser nogen steder. Vaerten ser sit spils navn (og koden, saa den kan
        // siges hoejt i lokalet); klienten ser navnet paa det spil den er med i.
        if (!Network) return;

        string text = ActiveMode == GameMode.Server
            ? Title
            : Networking.IsConnected
                ? $"Forbundet til: {(_connectedTitle.Length > 0 ? _connectedTitle : "spillet")}"
                : "Ikke forbundet";

        Color color = ActiveMode == GameMode.Server ? Color.DarkGray
            : Networking.IsConnected ? Color.Green : Color.DarkGray;

        const int fontSize = 30;
        int width = MeasureText(text, fontSize);
        Draw.Text(text, new Vector2(GetScreenWidth() - width - 10, 10), fontSize, color);

        if (ActiveMode == GameMode.Server && Password.Length > 0)
        {
            string kode = $"Kode: {Password}";
            int kodeWidth = MeasureText(kode, 18);
            Draw.Text(kode, new Vector2(GetScreenWidth() - kodeWidth - 10, 14 + fontSize), 18, Color.DarkGray);
        }
    }

    /// <summary>
    /// Boksen med hvem der er med. P viser og skjuler den. Vaerten kender selv listen;
    /// klienterne faar den tilsendt gennem spillets faelles hukommelse (*spillere).
    /// </summary>
    private void RenderPlayerList()
    {
        if (!Network || !_showPlayers) return;

        List<string> navne = ActiveMode == GameMode.Server
            ? PlayerRoster()
            : [.. State.Text("*spillere").Split(", ", StringSplitOptions.RemoveEmptyEntries)];

        const int fontSize = 18;
        const int lineHeight = fontSize + 6;
        int hoejde = (Math.Max(navne.Count, 1) + 1) * lineHeight + 14;
        int y = GetScreenHeight() - hoejde - 34;

        DrawRectangle(6, y - 8, 250, hoejde, new Color(0, 0, 0, 80));
        Draw.Text("Spillere (P skjuler):", new Vector2(14, y), fontSize, Color.RayWhite);

        if (navne.Count == 0)
            Draw.Text("(ingen endnu)", new Vector2(14, y + lineHeight), fontSize, Color.LightGray);

        for (int i = 0; i < navne.Count; i++)
            Draw.Text(navne[i], new Vector2(14, y + (i + 1) * lineHeight), fontSize, Color.RayWhite);
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
    private string? RunTextEntryScreen(string prompt, string hint, bool allowEmpty = false)
    {
        while (GetKeyPressed() != 0) { } // toem gamle tastetryk
        var input = new System.Text.StringBuilder();
        bool klar = false;   // et Enter fra skaermen foer maa ikke taelle med

        while (!WindowShouldClose())
        {
            int key;
            while ((key = GetCharPressed()) != 0)
            {
                if (key is >= 32 and < 127) input.Append((char)key);
            }

            if (IsKeyPressed(KeyboardKey.Backspace) && input.Length > 0)
                input.Remove(input.Length - 1, 1);

            if (klar && IsKeyPressed(KeyboardKey.Enter) && (allowEmpty || input.Length > 0))
                return input.ToString();

            BeginDrawing();
            ClearBackground(Background);
            Draw.Text(prompt, new Vector2(100, 160), 24, Color.Black);
            Draw.Text(input + "_", new Vector2(100, 200), 24, Color.DarkGray);
            Draw.Text(hint, new Vector2(100, 250), 16, Color.Gray);
            EndDrawing();
            klar = true;
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
        _discovery.Stop();
        if (ActiveMode == GameMode.Server) Networking.Stop();
        Assets.UnloadAll();
        CloseAudioDevice();
        CloseWindow();
    }
}
