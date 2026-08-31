namespace Kraken;

/// <summary>
/// Alt en komponent kan naa udenfor sig selv. Bliver givet med til OnAdded, Update,
/// OnCollision og OnRemoved.
/// </summary>
public class GameContext
{
    private readonly GameEngine _engine;

    internal GameContext(GameEngine engine) => _engine = engine;

    /// <summary>
    /// Den komponent motoren er i gang med lige nu. Bruges til at give timere og
    /// besked-abonnementer en ejer, saa de rydder op af sig selv.
    /// </summary>
    internal Component? Current { get; set; }

    /// <summary>Er vi server (og dermed ogsaa single player) eller klient?</summary>
    public GameMode Mode => _engine.Mode;

    /// <summary>True naar denne maskine bestemmer hvad der sker. Falsk paa en klient.</summary>
    public bool IsServer => _engine.Mode == GameMode.Server;

    /// <summary>Netvaerket. Se 'Network protocol.md' for hvilke beskeder der findes.</summary>
    public Networking Networking => _engine.Networking;

    /// <summary>Kameraet. Flyt Target for at foelge en spiller, skru paa Height for at zoome.</summary>
    public GameCamera Camera => _engine.Camera;

    /// <summary>Lyset. Normalt roerer du det ikke - komponenten Light laegger sig selv i det.</summary>
    public Lighting Lighting => _engine.Lighting;

    /// <summary>Spillets faelles hukommelse - point, liv, hvilket bane man er paa.</summary>
    public GameState State => _engine.State;

    /// <summary>Beskeder mellem komponenter.</summary>
    public EventBus Events => _engine.Events;

    /// <summary>Sekunder siden sidste frame. Gang altid hastigheder med denne.</summary>
    public float DeltaTime => _engine.DeltaTime;

    /// <summary>
    /// Paa en klient: den figur serveren har sagt er din. null indtil serveren har svaret,
    /// og altid null paa serveren. God til at lade kameraet foelge en selv.
    /// </summary>
    public Component? MyEntity => _engine.LocalEntity;

    /// <summary>Dit eget spillernavn. Tomt naar man koerer som server.</summary>
    public string MyName => _engine.PlayerName;

    // -------------------------------------------------------------- Komponenter

    /// <summary>Komponenten kommer med i spillet efter denne frame.</summary>
    public T Add<T>(T component) where T : Component => _engine.Add(component);

    /// <summary>Komponenten forsvinder fra spillet efter denne frame.</summary>
    public void Remove(Component component) => _engine.Remove(component);

    public void RemoveAll<T>() where T : Component => _engine.RemoveAll<T>();

    /// <summary>Fjerner alt der ikke er markeret Persistent. Bruges naar spillet startes forfra.</summary>
    public void RemoveAll() => _engine.RemoveAll();

    /// <summary>Alle komponenter af en bestemt type, fx context.Find&lt;Coin&gt;().</summary>
    public IEnumerable<T> Find<T>() => _engine.Components.OfType<T>();

    /// <summary>Den foerste komponent af en bestemt type, eller null hvis der ikke er nogen.</summary>
    public T? FindFirst<T>() where T : class => _engine.Components.OfType<T>().FirstOrDefault();

    /// <summary>Alle komponenter med et bestemt maerkat, fx context.FindByTag("fjende").</summary>
    public IEnumerable<Component> FindByTag(string tag) => _engine.Components.Where(c => c.HasTag(tag));

    // ------------------------------------------------------------------ Timere

    /// <summary>Koer noget een gang om et stykke tid.</summary>
    public TimerHandle After(float seconds, Action action) => _engine.Timers.Add(seconds, false, action, Current);

    /// <summary>Koer noget igen og igen med faste mellemrum.</summary>
    public TimerHandle Every(float seconds, Action action) => _engine.Timers.Add(seconds, true, action, Current);

    // ---------------------------------------------------------------- Beskeder

    /// <summary>Lyt efter en besked. Abonnementet forsvinder af sig selv naar komponenten fjernes.</summary>
    public void On<T>(Action<T> handler)
    {
        if (Current != null) Events.Subscribe(Current, handler);
        else Events.Subscribe(handler);
    }

    /// <summary>Send en besked ud til alle der lytter.</summary>
    public void Publish<T>(T message) => Events.Publish(message);

    // ------------------------------------------------------------------- Input

    /// <summary>Tastaturet paa denne maskine.</summary>
    public Input Input => _engine.LocalInput;

    /// <summary>
    /// Input fra en bestemt spiller. Giv null eller "" med for at faa tastaturet her paa maskinen.
    /// Er spilleren ikke forbundet, faar du et input hvor der ikke trykkes paa noget.
    /// </summary>
    public Input InputFor(string? playerName) => _engine.InputFor(playerName);

    /// <summary>
    /// Fortaeller en spiller over netvaerket hvilken komponent der er deres figur.
    /// Klienten kan saa finde den igen som context.MyEntity. Virker kun paa serveren.
    /// </summary>
    public void ClaimForPlayer(string playerName, Component component)
        => _engine.ClaimForPlayer(playerName, component);
}

public enum GameMode
{
    /// <summary>Kigger paa en server. Sender input, tegner det serveren siger.</summary>
    Client,

    /// <summary>Bestemmer alt. Dette er ogsaa den tilstand et single player-spil koerer i.</summary>
    Server
}
