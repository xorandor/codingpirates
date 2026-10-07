using System.Numerics;
using Kraken;

namespace Mine;

/// <summary>
/// Holder styr paa liv, tid og niveau. Ejer "point", "liv", "tid", "niveau" og "rekord" i
/// context.State - saetter dem ved start. Hvert SekunderPrNiveau bliver spillet et trin
/// svaerere (NytNiveau), og naar livene er brugt, sender den GameOver.
/// </summary>
public class Spilstyring : Component
{
    public int StartLiv { get; set; } = 3;
    public float SekunderPrNiveau { get; set; } = 30f;

    private float _tid;
    private int _niveau;
    private bool _slut = true;   // der er ikke noget spil i gang, foer Velkomstskaerm sender GameStarted

    public override void OnAdded(GameContext context)
    {
        context.State.SetNumber("liv", StartLiv);
        context.State.SetNumber("niveau", 1);
        context.State.SetNumber("rekord", Highscore.Bedste());

        context.On<GameStarted>(_ =>
        {
            context.State.SetNumber("point", 0);
            context.State.SetNumber("liv", StartLiv);
            context.State.SetNumber("tid", 0);
            context.State.SetNumber("niveau", 1);
            context.State.SetNumber("rekord", Highscore.Bedste());
            _tid = 0f;
            _niveau = 1;
            _slut = false;
        });

        context.On<SkibRamt>(_ => context.State.Add("liv", -1));
    }

    public override void Update(GameContext context)
    {
        if (_slut) return;

        _tid += context.DeltaTime;
        context.State.SetNumber("tid", (int)_tid);

        int niveau = 1 + (int)(_tid / SekunderPrNiveau);
        if (niveau != _niveau)
        {
            _niveau = niveau;
            context.State.SetNumber("niveau", niveau);
            context.Publish(new NytNiveau(niveau));
            context.Add(new SvaevendeTekst { Position = new Vector3(0, 200, 0), Tekst = $"NIVEAU {niveau}", FontSize = 40, Varighed = 1.6f });
        }

        if (context.State.Number("liv") <= 0)
        {
            _slut = true;
            context.Publish(new GameOver());
        }
    }
}
