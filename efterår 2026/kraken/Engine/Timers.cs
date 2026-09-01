namespace Kraken;

/// <summary>
/// Ting der skal ske senere, uden at du selv skal holde styr paa en float.
///
///   context.After(2f, () =&gt; context.Remove(this));            // om 2 sekunder
///   context.Every(0.5f, () =&gt; context.Add(new HoppeBold()));    // hvert halve sekund
///
/// Every giver dig et handle tilbage, som du kan stoppe igen:
///   var spawner = context.Every(1f, Spawn);
///   spawner.Cancel();
/// </summary>
public class TimerHandle
{
    internal float Remaining;
    internal readonly float Interval;
    internal readonly bool Repeating;
    internal readonly Action Action;
    internal readonly Component? Owner;

    internal bool Cancelled { get; private set; }

    internal TimerHandle(float interval, bool repeating, Action action, Component? owner)
    {
        Remaining = interval;
        Interval = interval;
        Repeating = repeating;
        Action = action;
        Owner = owner;
    }

    /// <summary>Stopper timeren. En timer der allerede er koert faerdig gaar det ikke ud over.</summary>
    public void Cancel() => Cancelled = true;
}

internal class Timers
{
    private readonly List<TimerHandle> _timers = [];
    private readonly List<TimerHandle> _pending = [];

    internal TimerHandle Add(float seconds, bool repeating, Action action, Component? owner)
    {
        var handle = new TimerHandle(MathF.Max(seconds, 0.0001f), repeating, action, owner);
        _pending.Add(handle);
        return handle;
    }

    internal void CancelAllOwnedBy(Component owner)
    {
        foreach (var timer in _timers)
            if (ReferenceEquals(timer.Owner, owner)) timer.Cancel();

        foreach (var timer in _pending)
            if (ReferenceEquals(timer.Owner, owner)) timer.Cancel();
    }

    internal void Tick(float deltaTime, bool blocked)
    {
        _timers.AddRange(_pending);
        _pending.Clear();

        // Baglaens, saa vi trygt kan fjerne undervejs.
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var timer = _timers[i];

            if (timer.Cancelled)
            {
                _timers.RemoveAt(i);
                continue;
            }

            // En timer holder pause naar dens ejer holder pause. Ellers ville fx en kanon
            // blive ved med at skyde bag en startskaerm, hvor kuglerne ikke kan bevaege sig.
            if (blocked && timer.Owner is { RunsWhileBlocked: false }) continue;

            timer.Remaining -= deltaTime;
            if (timer.Remaining > 0f) continue;

            if (timer.Repeating)
                timer.Remaining += timer.Interval;
            else
                _timers.RemoveAt(i);

            timer.Action();
        }
    }
}
