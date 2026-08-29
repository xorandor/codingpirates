namespace Kraken;

/// <summary>
/// Beskeder mellem komponenter. En komponent raaber ud i rummet, og de der har lyst
/// lytter med - uden at nogen af dem kender hinanden.
///
/// Lyt (typisk i OnAdded):
///   context.Events.Subscribe&lt;Collected&gt;(this, e =&gt; Console.WriteLine("nogen samlede noget op"));
///
/// Raab:
///   context.Events.Publish(new Collected(this, item, 10));
///
/// Giver du "this" med, rydder motoren selv op naar komponenten fjernes.
/// </summary>
public class EventBus
{
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

    private record Subscription(Component? Owner, Delegate Handler);

    /// <summary>Lyt efter en bestemt slags besked. owner er den komponent der lytter.</summary>
    public void Subscribe<T>(Component owner, Action<T> handler) => AddSubscription(owner, handler);

    /// <summary>Lyt uden ejer. Brug den fra program.cs - den bliver aldrig ryddet op af sig selv.</summary>
    public void Subscribe<T>(Action<T> handler) => AddSubscription(null, handler);

    private void AddSubscription<T>(Component? owner, Action<T> handler)
    {
        if (!_subscriptions.TryGetValue(typeof(T), out var list))
            _subscriptions[typeof(T)] = list = [];

        list.Add(new Subscription(owner, handler));
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (_subscriptions.TryGetValue(typeof(T), out var list))
            list.RemoveAll(s => s.Handler.Equals(handler));
    }

    /// <summary>Send en besked ud til alle der lytter efter netop denne type.</summary>
    public void Publish<T>(T message)
    {
        if (!_subscriptions.TryGetValue(typeof(T), out var list) || list.Count == 0)
            return;

        // Kopi, saa en modtager gerne maa tilfoeje eller fjerne lyttere undervejs.
        foreach (var subscription in list.ToArray())
            ((Action<T>)subscription.Handler)(message);
    }

    internal void UnsubscribeAll(Component owner)
    {
        foreach (var list in _subscriptions.Values)
            list.RemoveAll(s => ReferenceEquals(s.Owner, owner));
    }
}

// -----------------------------------------------------------------------------
// De beskeder motoren selv sender. Du maa gerne lave dine egne - en record i din
// egen fil er nok. Men husk at en besked kun kan hoeres af nogen der kender typen,
// saa beskeder der skal krydse mellem faelles og private komponenter hoerer til her.
// -----------------------------------------------------------------------------

/// <summary>Nogen samlede noget op.</summary>
public record Collected(Component Collector, Component Item, int Value);

/// <summary>Nogen tog skade.</summary>
public record Damaged(Component Target, int Amount, Component? Source);

/// <summary>Nogen fik liv eller helbred tilbage.</summary>
public record Healed(Component Target, int Amount);

/// <summary>Nogen doede.</summary>
public record Died(Component Who);

/// <summary>Pointtallet aendrede sig. Key er hvilket tal, fx "score".</summary>
public record ScoreChanged(string Key, int Points, int Change);

/// <summary>Spillet gaar i gang - fx naar startskaermen forsvinder.</summary>
public record GameStarted;

/// <summary>Spillet er tabt.</summary>
public record GameOver;

/// <summary>Spillet er vundet.</summary>
public record GameWon;

/// <summary>En spiller forbandt sig til serveren. Sendes kun paa serveren.</summary>
public record PlayerJoined(string PlayerName);

/// <summary>En spiller forsvandt igen. Sendes kun paa serveren.</summary>
public record PlayerLeft(string PlayerName);
