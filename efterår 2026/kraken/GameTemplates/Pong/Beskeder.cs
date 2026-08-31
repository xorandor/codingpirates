using System.Numerics;
using Kraken;

namespace Mine;

// Kontrakten mellem din bold og resten af Pong-pakken. Bolden kender ingen af de andre
// komponenter - den publisher kun beskeder og baerer en enkelt evne. Se README.md.

/// <summary>
/// Noget der bevaeger sig i en retning. Din bold skal implementere den, saa computerens
/// bat kan se om bolden er paa vej mod det - uden at kende din boldtype.
/// </summary>
public interface IHarRetning
{
    Vector2 Retning { get; }
}

/// <summary>Publish den hver gang bolden serves paa ny. Computeren traekker sin sigtefejl om.</summary>
public record BoldenServes;

/// <summary>Publish den naar nogen scorer. TilHvem er "venstre" eller "hoejre". Kameraet rykker.</summary>
public record Maal(string TilHvem);

/// <summary>Publish den naar bolden rammer et bat. Battet blinker, og der kommer en ring i Punkt.</summary>
public record BatRamt(Component Bat, Vector3 Punkt);
