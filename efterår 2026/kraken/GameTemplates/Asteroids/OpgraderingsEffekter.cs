using Kraken;

namespace Mine;

/// <summary>
/// De opgraderinger der ikke hoerer hjemme paa skibet: et ekstra liv. FlereSkud og
/// HurtigereSkud klarer Skib selv - se Beskeder.cs. Viser ogsaa en kort tekst for alle
/// tre slags, saa man kan se hvad man lige har samlet op.
/// </summary>
public class OpgraderingsEffekter : Component
{
    public override void OnAdded(GameContext context) => context.On<OpgraderingSamlet>(e =>
    {
        context.Add(new SvaevendeTekst { Position = e.Punkt, Tekst = Opgradering.TekstFor(e.Type) + "!", Farve = e.Farve });

        if (e.Type == OpgraderingsType.EkstraLiv)
        {
            context.State.Add("liv", 1);
            context.Publish(new Healed(this, 1));   // SoundEffects spiller sin "helet"-lyd paa den
        }
    });
}
