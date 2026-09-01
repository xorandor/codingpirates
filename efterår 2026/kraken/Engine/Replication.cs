using System.Globalization;
using System.Numerics;
using Raylib_cs;

namespace Kraken;

/// <summary>
/// Skriver den ekstra tilstand en komponent vil have med over netvaerket.
/// Laes den i den SAMME raekkefoelge i ReadState.
/// </summary>
public class StateWriter
{
    internal readonly List<string> Fields = [];

    public void Number(float value) => Fields.Add(value.ToString("0.##", CultureInfo.InvariantCulture));
    public void Whole(int value) => Fields.Add(value.ToString(CultureInfo.InvariantCulture));
    public void Flag(bool value) => Fields.Add(value ? "1" : "0");
    public void Text(string? value) => Fields.Add(value ?? "");
    public void Colour(Color value) => Fields.Add($"{value.R}_{value.G}_{value.B}");
}

/// <summary>Laeser den tilstand serveren sendte. Samme raekkefoelge som i WriteState.</summary>
public class StateReader
{
    private readonly string[] _fields;
    private int _index;

    internal StateReader(string[] fields) => _fields = fields;

    private string Next() => _index < _fields.Length ? _fields[_index++] : "";

    public float Number() => float.TryParse(Next(), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
    public int Whole() => int.TryParse(Next(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
    public bool Flag() => Next() == "1";
    public string Text() => Next();

    public Color Colour()
    {
        var parts = Next().Split('_');
        if (parts.Length < 3) return Color.White;

        return new Color(
            byte.TryParse(parts[0], out byte r) ? r : (byte)255,
            byte.TryParse(parts[1], out byte g) ? g : (byte)255,
            byte.TryParse(parts[2], out byte b) ? b : (byte)255,
            (byte)255);
    }
}

/// <summary>
/// Klientens opslagsbog. Serveren sender et navn - fx "markoer" - og klienten slaar op
/// her for at finde ud af hvad den skal lave for at tegne det.
///
///   game.NetworkKinds.Register("markoer", () =&gt; new Markoer());
///   game.NetworkKinds.Register("hoppebold", () =&gt; new HoppeBold());
///
/// Staar en slags ikke i bogen, tegner klienten en graa kasse med navnet paa i stedet
/// for at gaa ned. Det er MENINGEN: du behoever ikke have den samme kode som serveren
/// for at kunne vaere med i spillet.
/// </summary>
public class NetworkKinds
{
    private readonly Dictionary<string, Func<Component>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string kind, Func<Component> create) => _factories[kind] = create;

    internal Component Create(string kind)
        => _factories.TryGetValue(kind, out var create) ? create() : new UnknownEntity { Kind = kind };
}

/// <summary>Det klienten tegner naar serveren sender en slags den ikke kender.</summary>
public class UnknownEntity : Component
{
    public string Kind { get; set; } = "?";

    public override void Render() => Draw.Cube(Position, new Vector3(30, 30, 30), Color.Gray);

    public override void RenderUI() => Draw.TextAbove(Kind, Position, 25, 14, Color.DarkGray);
}

/// <summary>
/// Motoren i motoren, naar det gaelder netvaerk. Serveren bestemmer alt og sender
/// SPAWN / STATE / DESPAWN ud. Klienten sender kun INPUT og tegner det den faar at vide.
/// </summary>
internal class Replication
{
    private readonly GameEngine _engine;
    private readonly Dictionary<Component, int> _idByComponent = [];
    private readonly Dictionary<int, Component> _componentById = [];
    private readonly Dictionary<string, Input> _inputByPlayer = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Component> _claims = new(StringComparer.OrdinalIgnoreCase);

    private int _nextId = 1;
    private float _sendTimer;
    private float _inputHeartbeat;
    private string _lastSentInput = "";

    // Klientens glidning: serveren sender kun positioner 20 gange i sekundet, men der tegnes
    // 60. Uden noget imellem ville alt hoppe i smaa ryk. Se Bane nederst i filen.
    private readonly Dictionary<Component, Bane> _baner = [];
    private double _klientTid;

    /// <summary>Hvor mange gange i sekundet serveren sender tilstanden ud.</summary>
    internal float Rate { get; set; } = 20f;

    /// <summary>Klientens egen figur, naar serveren har fortalt hvilken det er.</summary>
    internal Component? LocalEntity { get; private set; }

    internal Replication(GameEngine engine) => _engine = engine;

    internal Input InputFor(string playerName)
        => _inputByPlayer.TryGetValue(playerName, out var input) ? input : default;

    /// <summary>Fortaeller en spiller hvilken figur der er deres, saa snart den har et id.</summary>
    internal void ClaimFor(string playerName, Component component) => _claims[playerName] = component;

    // ------------------------------------------------------------------ Server

    internal void OnComponentAdded(Component component)
    {
        if (_engine.ActiveMode != GameMode.Server || component.NetworkKind == null) return;

        int id = _nextId++;
        _idByComponent[component] = id;
        _componentById[id] = component;

        _engine.Networking.BroadcastMessageToClients("SPAWN", id.ToString(), component.NetworkKind);
        _engine.Networking.BroadcastMessageToClients("STATE", StateFields(component, id));
    }

    internal void OnComponentRemoved(Component component)
    {
        _baner.Remove(component);
        if (!_idByComponent.Remove(component, out int id)) return;

        _componentById.Remove(id);

        if (_engine.ActiveMode == GameMode.Server)
            _engine.Networking.BroadcastMessageToClients("DESPAWN", id.ToString());
    }

    internal void ServerTick(float deltaTime)
    {
        foreach (var joined in _engine.Networking.ConsumeAll("JOINED"))
        {
            SendSnapshotTo(joined.Field(0));
            _engine.Events.Publish(new PlayerJoined(joined.Field(0)));
        }

        foreach (var input in _engine.Networking.ConsumeAll("INPUT"))
            if (input.Sender.Length > 0)
                _inputByPlayer[input.Sender] = Input.Unpack(input.Field(0));

        foreach (var gone in _engine.Networking.ConsumeAll("DISCONNECTED"))
        {
            _inputByPlayer.Remove(gone.Field(0));
            _claims.Remove(gone.Field(0));
            _engine.Events.Publish(new PlayerLeft(gone.Field(0)));
        }

        // Listen over hvem der er med, som faelles tekst - saa har klienterne den ogsaa (P-boksen).
        // Kun naar den faktisk har aendret sig, ellers sendes den 20 gange i sekundet.
        string spillere = string.Join(", ", _engine.PlayerRoster());
        if (_engine.Network && spillere != _engine.State.Text("*spillere"))
            _engine.State.SetText("*spillere", spillere);

        SendPendingClaims();

        _sendTimer -= deltaTime;
        if (_sendTimer > 0f) return;
        _sendTimer = 1f / Rate;

        foreach (var (component, id) in _idByComponent)
            _engine.Networking.BroadcastMessageToClients("STATE", StateFields(component, id));

        foreach (var (key, kind, value) in _engine.State.TakeChanges())
            _engine.Networking.BroadcastMessageToClients("GAMESTATE", key, kind, value);
    }

    private void SendPendingClaims()
    {
        if (_claims.Count == 0) return;

        var sent = new List<string>();
        foreach (var (playerName, component) in _claims)
        {
            if (!_idByComponent.TryGetValue(component, out int id)) continue;

            _engine.Networking.SendMessageToPlayer(playerName, "YOUARE", id.ToString());
            sent.Add(playerName);
        }

        foreach (var playerName in sent) _claims.Remove(playerName);
    }

    private void SendSnapshotTo(string playerName)
    {
        foreach (var (component, id) in _idByComponent)
        {
            _engine.Networking.SendMessageToPlayer(playerName, "SPAWN", id.ToString(), component.NetworkKind!);
            _engine.Networking.SendMessageToPlayer(playerName, "STATE", StateFields(component, id));
        }

        foreach (var (key, kind, value) in _engine.State.Snapshot())
            _engine.Networking.SendMessageToPlayer(playerName, "GAMESTATE", key, kind, value);
    }

    private static string[] StateFields(Component component, int id)
    {
        var writer = new StateWriter();
        component.WriteState(writer);

        var fields = new List<string>(4 + writer.Fields.Count)
        {
            id.ToString(CultureInfo.InvariantCulture),
            component.Position.X.ToString("0.##", CultureInfo.InvariantCulture),
            component.Position.Y.ToString("0.##", CultureInfo.InvariantCulture),
            component.Position.Z.ToString("0.##", CultureInfo.InvariantCulture)
        };

        fields.AddRange(writer.Fields);
        return [.. fields];
    }

    // ------------------------------------------------------------------ Klient

    internal void ClientTick(float deltaTime, Input localInput)
    {
        _klientTid += deltaTime;

        foreach (var spawn in _engine.Networking.ConsumeAll("SPAWN"))
        {
            int id = spawn.Number(0);
            if (id == 0 || _componentById.ContainsKey(id)) continue;

            var component = _engine.NetworkKinds.Create(spawn.Field(1));
            component.IsRemote = true;
            _componentById[id] = component;
            _idByComponent[component] = id;
            _engine.Add(component);
        }

        foreach (var state in _engine.Networking.ConsumeAll("STATE"))
        {
            if (!_componentById.TryGetValue(state.Number(0), out var component)) continue;

            var position = new Vector3(
                ParseFloat(state.Field(1)),
                ParseFloat(state.Field(2)),
                ParseFloat(state.Field(3)));

            // Positionen saettes ikke direkte - den laegges paa komponentens bane, og
            // laengere nede glides der hen imod den. Resten af tilstanden gaelder med det samme.
            if (_baner.TryGetValue(component, out var bane))
                bane.Modtag(position, _klientTid);
            else
            {
                _baner[component] = new Bane(position, _klientTid);
                component.Position = position;
            }

            component.ReadState(new StateReader(state.Fields[4..]));
        }

        foreach (var despawn in _engine.Networking.ConsumeAll("DESPAWN"))
        {
            if (!_componentById.TryGetValue(despawn.Number(0), out var component)) continue;

            if (ReferenceEquals(component, LocalEntity)) LocalEntity = null;
            _engine.Remove(component);
        }

        foreach (var change in _engine.Networking.ConsumeAll("GAMESTATE"))
            _engine.State.ApplyRemote(change.Field(0), change.Field(1), change.Field(2));

        var youAre = _engine.Networking.TryConsumeMessage("YOUARE");
        if (youAre != null && _componentById.TryGetValue(youAre.Number(0), out var mine))
            LocalEntity = mine;

        // Tegn to snapshots bagude i tid - saa ligger der altid en kendt position foran OG
        // bagved, og alt glider jaevnt selvom serveren kun sender 20 gange i sekundet.
        double visTid = _klientTid - 2.0 / Rate;
        foreach (var (component, bane) in _baner)
            component.Position = bane.Sample(visTid);

        SendInput(deltaTime, localInput);
    }

    private void SendInput(float deltaTime, Input localInput)
    {
        // Send kun naar der sker noget - plus et livstegn to gange i sekundet,
        // saa serveren ogsaa faar den sidste "nu slap jeg tasten" hvis en pakke gaar tabt.
        _inputHeartbeat -= deltaTime;
        string packed = localInput.Pack();

        if (packed == _lastSentInput && _inputHeartbeat > 0f) return;

        _lastSentInput = packed;
        _inputHeartbeat = 0.5f;
        _engine.Networking.SendMessageToServer("INPUT", packed);
    }

    private static float ParseFloat(string text)
        => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;

    /// <summary>
    /// De seneste positioner serveren har sendt for en komponent, med tidspunkterne de kom
    /// paa. Klienten tegner med vilje ca. 0,1 sekund bagude i tid og glider mellem de to
    /// punkter der ligger paa hver side af det tidspunkt - den klassiske loesning i
    /// multiplayer-spil (snapshot-interpolation): lidt forsinkelse, til gengaeld bloed
    /// bevaegelse. Der gaettes aldrig fremad - kommer der ingen nye positioner, staar
    /// komponenten stille paa den sidste.
    /// </summary>
    private sealed class Bane(Vector3 position, double tid)
    {
        private readonly List<(Vector3 Position, double Tid)> _punkter = [(position, tid)];

        public void Modtag(Vector3 position, double tid)
        {
            // Har der vaeret et laengere hul (pause, netvaerksbump), saa hop i stedet for
            // at glide tvaers over banen i langsom gengivelse.
            if (tid - _punkter[^1].Tid > 0.5) _punkter.Clear();

            _punkter.Add((position, tid));
            if (_punkter.Count > 10) _punkter.RemoveAt(0);
        }

        public Vector3 Sample(double tid)
        {
            if (tid >= _punkter[^1].Tid) return _punkter[^1].Position;
            if (tid <= _punkter[0].Tid) return _punkter[0].Position;

            for (int i = _punkter.Count - 1; i > 0; i--)
            {
                var (fraPosition, fraTid) = _punkter[i - 1];
                if (fraTid > tid) continue;

                var (tilPosition, tilTid) = _punkter[i];
                float t = (float)((tid - fraTid) / (tilTid - fraTid));
                return Vector3.Lerp(fraPosition, tilPosition, t);
            }

            return _punkter[0].Position;
        }
    }
}
