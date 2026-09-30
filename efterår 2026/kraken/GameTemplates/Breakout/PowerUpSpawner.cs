using Kraken;

namespace Mine;

/// <summary>
/// Lytter efter odelagte klodser og lader nogle af dem tabe en tilfaeldig power-up.
/// Kender hverken Klods eller Bat - kun beskeden KlodsOdelagt.
/// </summary>
public class PowerUpSpawner : Component
{
    /// <summary>Chancen for at en odelagt klods taber en power-up. 0.18 = ca. hver femte.</summary>
    public float Chance { get; set; } = 0.18f;
    public float Fart { get; set; } = 160f;

    public override void OnAdded(GameContext context) => context.On<KlodsOdelagt>(e =>
    {
        if (Random.Shared.NextSingle() > Chance) return;

        var typer = Enum.GetValues<PowerUpType>();
        var type = typer[Random.Shared.Next(typer.Length)];
        context.Add(new PowerUp { Position = e.Position, Type = type, Fart = Fart });
    });
}
