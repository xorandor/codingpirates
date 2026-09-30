using Kraken;

namespace Mine;

/// <summary>
/// De power-up-effekter der ikke hoerer hjemme paa eet bestemt objekt: en ekstra bold, eller
/// et ekstra liv. UdvidBat klarer Bat selv, LangsomBold klarer Bold selv - se Beskeder.cs.
/// Viser ogsaa en kort tekst for alle fire slags, saa man kan se hvad man lige har samlet op.
/// </summary>
public class PowerUpEffekter : Component
{
    public override void OnAdded(GameContext context) => context.On<PowerUpSamlet>(e =>
    {
        context.Add(new PowerUpBesked { Position = e.Punkt, Tekst = Besked(e.Type), Farve = e.Farve });

        switch (e.Type)
        {
            case PowerUpType.EkstraBold:
                var forlaeg = context.Find<Bold>().FirstOrDefault();
                if (forlaeg != null) context.Add(forlaeg.Kopi());
                break;

            case PowerUpType.EkstraLiv:
                context.State.Add("liv", 1);
                break;
        }
    });

    private static string Besked(PowerUpType type) => type switch
    {
        PowerUpType.UdvidBat => "BREDERE BAT!",
        PowerUpType.EkstraBold => "EKSTRA BOLD!",
        PowerUpType.LangsomBold => "LANGSOM BOLD!",
        PowerUpType.EkstraLiv => "EKSTRA LIV!",
        _ => ""
    };
}
