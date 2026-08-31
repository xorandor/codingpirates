using Kraken;

namespace Components;

/// <summary>
/// Lydeffekter paa spillets beskeder. Tilfoej den, og der er lyd naar nogen samler noget op,
/// tager skade, faar liv, doer, vinder, taber eller kommer med. Alle de indbyggede lyde er
/// lavet af koden - ingen filer noedvendige.
///
///   game.Add(new SoundEffects());
///   game.Add(new SoundEffects { Collected = "min-plink.wav" });   // brug din egen fil
///   game.Add(new SoundEffects { Damaged = "" });                  // slaa en enkelt lyd fra
///
/// Komponenten lytter kun paa beskeder - den kender ingen andre komponenter. Det betyder
/// ogsaa at dine EGNE komponenter faar lyd gratis, hvis de publisher de samme beskeder.
/// I netvaerksspil spilles lyden der hvor logikken koerer - altsaa paa serveren.
/// </summary>
public class SoundEffects : Component
{
    public string Collected { get; set; } = "*samlet";
    public string Damaged { get; set; } = "*av";
    public string Healed { get; set; } = "*helet";
    public string Died { get; set; } = "*doed";
    public string GameOver { get; set; } = "*tabt";
    public string GameWon { get; set; } = "*vundet";
    public string PlayerJoined { get; set; } = "*hej";

    /// <summary>Styrken paa alle lydene. 0 er stille, 1 er fuld.</summary>
    public float Volume { get; set; } = 1f;

    public override bool Persistent => true;

    public override void OnAdded(GameContext context)
    {
        // De indbyggede lyde laves kun hvis de stadig er valgt.
        if (Collected == "*samlet") Assets.Tone("*samlet", 880, 1567, 0.09f);
        if (Damaged == "*av") Assets.Tone("*av", 220, 110, 0.16f);
        if (Healed == "*helet") Assets.Tone("*helet", 523, 784, 0.12f, firkant: false);
        if (Died == "*doed") Assets.Tone("*doed", 392, 65, 0.4f);
        if (GameOver == "*tabt") Assets.Tone("*tabt", 294, 73, 0.6f);
        if (GameWon == "*vundet") Assets.Tone("*vundet", 523, 1046, 0.35f);
        if (PlayerJoined == "*hej") Assets.Tone("*hej", 659, 880, 0.1f, firkant: false);

        context.On<Collected>(_ => Spil(Collected));
        context.On<Damaged>(_ => Spil(Damaged));
        context.On<Healed>(_ => Spil(Healed));
        context.On<Died>(_ => Spil(Died));
        context.On<GameOver>(_ => Spil(GameOver));
        context.On<GameWon>(_ => Spil(GameWon));
        context.On<PlayerJoined>(_ => Spil(PlayerJoined));
    }

    private void Spil(string navn)
    {
        if (navn.Length > 0) Assets.Play(navn, Volume);
    }
}
