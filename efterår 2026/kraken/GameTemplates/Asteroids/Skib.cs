using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Dit rumskib. Pil venstre/hoejre drejer det, pil op giver gas i den retning naesen peger -
/// der er ingen bremse og ingen bak, kun modstanden i rummet. Mellemrum skyder.
/// Flyver du ud over kanten, kommer du ind paa den modsatte side.
/// Opgraderingerne FlereSkud og HurtigereSkud klarer skibet selv - de ryger alle sammen,
/// naar man mister et liv. Se OpgraderingSamlet i Beskeder.cs.
/// </summary>
public class Skib : Component
{
    public float Drejefart { get; set; } = 230f;        // grader per sekund
    public float Acceleration { get; set; } = 520f;
    public float Modstand { get; set; } = 0.7f;         // hvor hurtigt farten doer ud, naar man slipper gassen
    public float MaxFart { get; set; } = 560f;
    public float Stoerrelse { get; set; } = 36f;
    public Color Farve { get; set; } = Color.RayWhite;
    public Color VingeFarve { get; set; } = new(200, 60, 60, 255);
    public Color FlammeFarve { get; set; } = Color.Orange;

    public float SkudFart { get; set; } = 780f;
    public float SkudPrSekund { get; set; } = 3f;
    public float SkudPrSekundPrNiveau { get; set; } = 2f;
    public float SpredningGrader { get; set; } = 12f;
    public int MaxNiveau { get; set; } = 3;

    /// <summary>Hvor laenge skibet er usaarligt (og blinker) efter et liv er mistet.</summary>
    public float UsaarligTid { get; set; } = 2.5f;

    /// <summary>Banens maal. Faste - kameraet er bare et vindue og maa gerne zoome.</summary>
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    private float _vinkel;             // grader, 0 = naesen opad, positiv = mod uret
    private Vector2 _fart;
    private bool _gas;
    private bool _synlig = true;
    private float _usaarlig;
    private float _skudPause;
    private int _skudNiveau = 1;       // 1 = eet skud, 2 = tre, 3 = fem
    private int _fartNiveau = 1;

    /// <summary>Den retning naesen peger.</summary>
    private Vector2 Retning => RetningFor(_vinkel);

    private static Vector2 RetningFor(float grader)
    {
        float rad = grader * MathF.PI / 180f;
        return new Vector2(-MathF.Sin(rad), MathF.Cos(rad));
    }

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Circle(Stoerrelse * 0.4f);
        Tags.Add("skib");

        Assets.Tone("*skib-skud", 1100, 400, 0.05f);
        Assets.Noise("*skib-ramt", 0.5f);
        Assets.Tone("*skib-opgradering", 600, 1400, 0.2f, firkant: false);

        context.On<GameStarted>(_ =>
        {
            _synlig = true;
            Genstart(context);
        });
        context.On<GameOver>(_ => _synlig = false);

        context.On<OpgraderingSamlet>(e =>
        {
            // Klarer selv sine egne to opgraderinger - EkstraLiv er ikke skibets sag.
            if (e.Type == OpgraderingsType.FlereSkud) _skudNiveau = Math.Min(_skudNiveau + 1, MaxNiveau);
            else if (e.Type == OpgraderingsType.HurtigereSkud) _fartNiveau = Math.Min(_fartNiveau + 1, MaxNiveau);
            else return;

            Assets.Play("*skib-opgradering");
            VisNiveauer(context);
        });

        Genstart(context);
    }

    /// <summary>Tilbage i midten, stille, uden opgraderinger - og usaarlig et oejeblik.</summary>
    private void Genstart(GameContext context)
    {
        Position = Vector3.Zero;
        _fart = Vector2.Zero;
        _vinkel = 0f;
        _gas = false;
        _usaarlig = UsaarligTid;
        _skudNiveau = 1;
        _fartNiveau = 1;
        VisNiveauer(context);
    }

    // Pointtaelleren viser niveauerne - den laeser dem her fra.
    private void VisNiveauer(GameContext context)
    {
        context.State.SetNumber("skud-niveau", _skudNiveau);
        context.State.SetNumber("fart-niveau", _fartNiveau);
    }

    public override void Update(GameContext context)
    {
        if (!_synlig) return;

        var input = context.Input;
        float dt = context.DeltaTime;

        if (input.Left) _vinkel += Drejefart * dt;
        if (input.Right) _vinkel -= Drejefart * dt;

        _gas = input.Up;
        if (_gas) _fart += Retning * Acceleration * dt;

        // Rummet bremser lidt, saa man ikke glider for evigt.
        _fart *= MathF.Max(0f, 1f - Modstand * dt);
        if (_fart.Length() > MaxFart) _fart = Vector2.Normalize(_fart) * MaxFart;

        Position += new Vector3(_fart.X, _fart.Y, 0) * dt;
        Position = Kant.Wrap(Position, BaneBredde, BaneHoejde, Stoerrelse / 2f);

        if (_usaarlig > 0f) _usaarlig -= dt;

        _skudPause -= dt;
        if (input.A && _skudPause <= 0f)
        {
            Skyd(context);
            _skudPause = 1f / (SkudPrSekund + (_fartNiveau - 1) * SkudPrSekundPrNiveau);
        }
    }

    /// <summary>Et, tre eller fem skud i en vifte, alt efter skudniveauet.</summary>
    private void Skyd(GameContext context)
    {
        int antal = _skudNiveau switch { 1 => 1, 2 => 3, _ => 5 };
        float foerste = -(antal - 1) / 2f * SpredningGrader;

        for (int i = 0; i < antal; i++)
        {
            var retning = RetningFor(_vinkel + foerste + i * SpredningGrader);
            context.Add(new Skud
            {
                Position = Position + new Vector3(retning.X, retning.Y, 0) * Stoerrelse * 0.6f,
                Retning = retning,
                Fart = SkudFart,
                BaneBredde = BaneBredde,
                BaneHoejde = BaneHoejde
            });
        }

        Assets.Play("*skib-skud", 0.5f);
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (!_synlig || _usaarlig > 0f || !other.HasTag("asteroide")) return;

        Assets.Play("*skib-ramt");
        context.Add(new Eksplosion { Position = Position, Antal = 30, Farve = Farve });
        context.Publish(new SkibRamt(Position));
        Genstart(context);
    }

    public override void Render()
    {
        if (!_synlig) return;

        // Blinker mens det er usaarligt: tegnes kun hver anden tiendedel sekund.
        if (_usaarlig > 0f && (int)(_usaarlig * 10f) % 2 == 0) return;

        float s = Stoerrelse;

        // Alt tegnes som om skibet peger opad fra (0,0) - matrixen flytter og drejer det hen hvor det er.
        Rlgl.PushMatrix();
        Rlgl.Translatef(Position.X, Position.Y, Position.Z);
        Rlgl.Rotatef(_vinkel, 0f, 0f, 1f);

        Draw.Cube(new Vector3(0, s * 0.1f, 0), new Vector3(s * 0.3f, s, s * 0.3f), Farve);                   // kroppen
        Draw.Cube(new Vector3(0, -s * 0.25f, 0), new Vector3(s * 1.1f, s * 0.3f, s * 0.12f), VingeFarve);    // vingerne
        Draw.Cube(new Vector3(0, s * 0.15f, s * 0.18f), new Vector3(s * 0.18f, s * 0.3f, s * 0.15f), Color.SkyBlue); // cockpit

        if (_gas)
        {
            float flakker = 0.6f + Random.Shared.NextSingle() * 0.6f;
            Draw.Cube(new Vector3(0, -s * 0.55f - s * 0.2f * flakker, 0), new Vector3(s * 0.22f, s * 0.45f * flakker, s * 0.2f), FlammeFarve);
        }

        Rlgl.PopMatrix();
    }
}
