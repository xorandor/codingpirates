namespace Kraken;

/// <summary>
/// Spillets faelles hukommelse. Alle komponenter kan laese og skrive her, saa man
/// slipper for at lede efter den komponent der tilfaeldigvis gemmer pointtallet.
///
///   context.State.Add("score", 10);
///   int point = context.State.Number("score");
///   context.State.SetFlag("gameover", true);
///
/// Score-komponenten viser bare det tal der ligger under "score".
/// </summary>
public class GameState
{
    private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _changed = new(StringComparer.OrdinalIgnoreCase);
    private readonly EventBus _events;

    internal GameState(EventBus events) => _events = events;

    /// <summary>Laeser et tal. Findes noeglen ikke, faar du 0.</summary>
    public int Number(string key) => Get(key, 0);

    /// <summary>Saetter et tal.</summary>
    public void SetNumber(string key, int value)
    {
        int previous = Number(key);
        if (previous == value) return;

        Store(key, value);
        _events.Publish(new ScoreChanged(key, value, value - previous));
    }

    /// <summary>Laegger til (eller traekker fra, hvis amount er negativ).</summary>
    public void Add(string key, int amount) => SetNumber(key, Number(key) + amount);

    /// <summary>Laeser et ja/nej. Findes noeglen ikke, faar du false.</summary>
    public bool Flag(string key) => Get(key, false);

    public void SetFlag(string key, bool value) => Store(key, value);

    /// <summary>Laeser en tekst. Findes noeglen ikke, faar du "".</summary>
    public string Text(string key) => Get(key, "");

    public void SetText(string key, string value) => Store(key, value);

    /// <summary>Laeser hvad som helst.</summary>
    public T Get<T>(string key, T fallback = default!)
        => _values.TryGetValue(key, out var value) && value is T typed ? typed : fallback;

    /// <summary>
    /// Gemmer hvad som helst. NB: kun tal, ja/nej og tekst bliver sendt videre til
    /// klienterne over netvaerket - alt andet bliver liggende paa den maskine det blev sat.
    /// </summary>
    public void Set<T>(string key, T value) => _values[key] = value!;

    public void Clear(string key)
    {
        if (_values.Remove(key)) _changed.Add(key);
    }

    /// <summary>Toemmer det hele. Bruges typisk naar et spil startes forfra.</summary>
    public void Clear()
    {
        foreach (var key in _values.Keys) _changed.Add(key);
        _values.Clear();
    }

    // --------------------------------------------------------------- Netvaerk

    private void Store(string key, object value)
    {
        _values[key] = value;
        _changed.Add(key);
    }

    /// <summary>Alt der har aendret sig siden sidst, klar til at blive sendt ud. Toemmer listen.</summary>
    internal List<(string Key, string Kind, string Value)> TakeChanges()
    {
        var changes = new List<(string, string, string)>();

        foreach (var key in _changed)
            if (Describe(key) is { } change) changes.Add(change);

        _changed.Clear();
        return changes;
    }

    /// <summary>Hele tilstanden, til en klient der lige er kommet med.</summary>
    internal List<(string Key, string Kind, string Value)> Snapshot()
    {
        var snapshot = new List<(string, string, string)>();

        foreach (var key in _values.Keys)
            if (Describe(key) is { } change) snapshot.Add(change);

        return snapshot;
    }

    private (string, string, string)? Describe(string key) => _values.GetValueOrDefault(key) switch
    {
        int number => (key, "n", number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        bool flag => (key, "b", flag ? "1" : "0"),
        string text => (key, "s", text),
        null => (key, "x", ""),          // noeglen er blevet slettet
        _ => null                        // Set<T> med noget andet - sendes ikke
    };

    /// <summary>Tager imod en aendring fra serveren. Markerer ikke noget som aendret.</summary>
    internal void ApplyRemote(string key, string kind, string value)
    {
        switch (kind)
        {
            case "n":
                int previous = Number(key);
                int parsed = int.TryParse(value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int n) ? n : 0;
                _values[key] = parsed;
                if (parsed != previous) _events.Publish(new ScoreChanged(key, parsed, parsed - previous));
                break;

            case "b": _values[key] = value == "1"; break;
            case "s": _values[key] = value; break;
            case "x": _values.Remove(key); break;
        }
    }
}
