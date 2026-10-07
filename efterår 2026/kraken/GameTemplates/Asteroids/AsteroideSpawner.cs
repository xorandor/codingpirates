using System.Numerics;
using Kraken;

namespace Mine;

/// <summary>
/// Sender nye asteroider ind fra kanten af skaermen - oftere og hurtigere for hvert niveau.
/// Lytter efter NytNiveau fra Spilstyring, og rydder banen naar et nyt spil starter.
/// </summary>
public class AsteroideSpawner : Component
{
    /// <summary>Asteroider paa banen fra start.</summary>
    public int StartAntal { get; set; } = 3;

    /// <summary>Sekunder mellem nye asteroider paa niveau 1, og hvor meget pausen falder per niveau.</summary>
    public float StartPause { get; set; } = 3f;
    public float PausePrNiveau { get; set; } = 0.3f;
    public float MinPause { get; set; } = 0.6f;

    /// <summary>Hvor hurtigt nye asteroider flyver paa niveau 1, og hvor meget mere per niveau.</summary>
    public float StartFart { get; set; } = 80f;
    public float FartPrNiveau { get; set; } = 18f;

    /// <summary>Loft, saa skaermen ikke drukner. Smaa stumper taeller ogsaa.</summary>
    public int MaxAntal { get; set; } = 45;

    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    private float _naeste;
    private int _niveau = 1;

    public override void OnAdded(GameContext context)
    {
        context.On<GameStarted>(_ => Nulstil(context));
        context.On<NytNiveau>(e => _niveau = e.Niveau);
        Nulstil(context);
    }

    private void Nulstil(GameContext context)
    {
        context.RemoveAll<Asteroide>();
        context.RemoveAll<Skud>();
        context.RemoveAll<Opgradering>();
        _niveau = 1;
        _naeste = StartPause;

        for (int i = 0; i < StartAntal; i++) Spawn(context, indenfor: true);
    }

    public override void Update(GameContext context)
    {
        _naeste -= context.DeltaTime;
        if (_naeste > 0f) return;

        _naeste = MathF.Max(MinPause, StartPause - (_niveau - 1) * PausePrNiveau);
        if (context.Find<Asteroide>().Count() < MaxAntal) Spawn(context);
    }

    /// <summary>
    /// En ny asteroide lige uden for en tilfaeldig kant, paa vej ind mod midten af banen.
    /// Med indenfor = true (de foerste ved start) lander den i stedet et sted paa banen -
    /// bare ikke lige oven i skibet i midten.
    /// </summary>
    private void Spawn(GameContext context, bool indenfor = false)
    {
        var r = Random.Shared;
        float halvB = BaneBredde / 2f, halvH = BaneHoejde / 2f;

        Vector3 start;
        if (indenfor)
        {
            do start = new Vector3((r.NextSingle() * 2f - 1f) * halvB * 0.85f, (r.NextSingle() * 2f - 1f) * halvH * 0.85f, 0);
            while (start.Length() < 220f);
        }
        else
        {
            start = r.Next(4) switch
            {
                0 => new Vector3(-halvB - 60f, (r.NextSingle() * 2f - 1f) * halvH, 0),   // venstre
                1 => new Vector3(halvB + 60f, (r.NextSingle() * 2f - 1f) * halvH, 0),    // hoejre
                2 => new Vector3((r.NextSingle() * 2f - 1f) * halvB, halvH + 60f, 0),    // top
                _ => new Vector3((r.NextSingle() * 2f - 1f) * halvB, -halvH - 60f, 0)    // bund
            };
        }

        // Sigter mod et tilfaeldigt punkt i den inderste del af banen - aldrig lige ud af skaermen igen.
        var maal = new Vector3((r.NextSingle() * 2f - 1f) * halvB * 0.6f, (r.NextSingle() * 2f - 1f) * halvH * 0.6f, 0);
        var retning = Vector3.Normalize(maal - start);

        float fart = (StartFart + (_niveau - 1) * FartPrNiveau) * (0.7f + r.NextSingle() * 0.6f);
        int stoerrelse = r.NextSingle() < 0.75f ? 3 : 2;

        context.Add(new Asteroide
        {
            Position = start,
            Fart = new Vector2(retning.X, retning.Y) * fart,
            Stoerrelse = stoerrelse,
            BaneBredde = BaneBredde,
            BaneHoejde = BaneHoejde
        });
    }
}
