using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Bolden. Den holder selv styr paa retning og fart, giver point, og serves igen af sig selv.
/// </summary>
public class Bold : Component, IHarRetning
{
    public float Stoerrelse { get; set; } = 26f;
    public float StartFart { get; set; } = 400f;
    public float MaxFart { get; set; } = 800f;
    public float FartFaktor { get; set; } = 1.05f;
    public float MaxUdgangsvinkel { get; set; } = 50f;
    public float MaxServVinkel { get; set; } = 35f;
    public float ServPause { get; set; } = 1f;
    public Color Farve { get; set; } = Color.RayWhite;
    public Color FarveTern { get; set; } = new(40, 40, 40, 255);

    /// <summary>Banens maal. Faste - kameraet er bare et vindue og maa gerne zoome.</summary>
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    /// <summary>1 = ruller praecis som en rigtig kugle. Mindre tal ruller langsommere, saa moenstret kan ses.</summary>
    public float Rullefaktor { get; set; } = 0.4f;

    public Vector2 Retning { get; private set; }

    private float _fart;
    private bool _venter;
    private int _servMod = 1;   // +1 = mod hoejre, -1 = mod venstre
    private Quaternion _drejning = Quaternion.Identity;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Circle(Stoerrelse / 2f);
        Tags.Add("bold");

        // Kuglen, dens ternede overflade og dens lyde laves af koden - ingen filer noedvendige.
        Assets.Checkered("bold-tern", Farve, FarveTern, cells: 6);
        Assets.Ball("bold", "bold-tern");
        Assets.Tone("*serv", 440, 660, 0.06f);
        Assets.Tone("*bat", 500, 900, 0.06f);
        Assets.Tone("*vaeg", 300, 220, 0.05f);
        Assets.Noise("*maal", 0.35f);

        context.On<GameStarted>(_ =>
        {
            _servMod = Random.Shared.Next(2) == 0 ? -1 : 1;
            Serv(context);
        });

        Serv(context);
    }

    private void Serv(GameContext context)
    {
        Position = Vector3.Zero;
        Retning = Vector2.Zero;
        _fart = StartFart;
        _venter = true;
        context.Publish(new BoldenServes());

        context.After(ServPause, () =>
        {
            float vinkel = (Random.Shared.NextSingle() * 2f - 1f) * MaxServVinkel * MathF.PI / 180f;
            Retning = new Vector2(MathF.Cos(vinkel) * _servMod, MathF.Sin(vinkel));
            _venter = false;
            Assets.Play("*serv");
        });
    }

    public override void Update(GameContext context)
    {
        if (_venter) return;

        Position += new Vector3(Retning.X, Retning.Y, 0) * _fart * context.DeltaTime;
        Rul(context.DeltaTime);

        // Top og bund: vend y. Kun hvis den er paa vej UD, ellers saetter den sig fast i kanten.
        float kant = BaneHoejde / 2f - Stoerrelse / 2f;
        if ((Position.Y > kant && Retning.Y > 0) || (Position.Y < -kant && Retning.Y < 0))
        {
            Retning = Retning with { Y = -Retning.Y };
            Assets.Play("*vaeg");
        }

        // Maal: forbi en af siderne.
        float maalstreg = BaneBredde / 2f;
        if (Position.X > maalstreg) Point(context, "venstre", servMod: 1);
        else if (Position.X < -maalstreg) Point(context, "hoejre", servMod: -1);
    }

    private void Point(GameContext context, string tilHvem, int servMod)
    {
        context.State.Add(tilHvem, 1);
        Assets.Play("*maal");
        context.Publish(new Maal(tilHvem));
        _servMod = servMod;   // serves mod den der lukkede maalet ind
        Serv(context);
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (_venter || !other.HasTag("bat")) return;

        // Kun hvis bolden er paa vej IND i battet. Ellers vendes den to gange og sejler igennem.
        bool batTilHoejre = other.Position.X > Position.X;
        if (batTilHoejre ? Retning.X < 0 : Retning.X > 0) return;

        // Hvor paa battet ramte vi? -1 i bunden, +1 i toppen. Det bestemmer vinklen ud.
        float batHalvHoejde = other.Collider!.Height / 2f;
        float traef = Math.Clamp((Position.Y - other.Position.Y) / batHalvHoejde, -1f, 1f);
        float vinkel = traef * MaxUdgangsvinkel * MathF.PI / 180f;
        float xRetning = batTilHoejre ? -1f : 1f;

        Retning = new Vector2(MathF.Cos(vinkel) * xRetning, MathF.Sin(vinkel));
        _fart = MathF.Min(_fart * FartFaktor, MaxFart);

        // Fortael verden det - battet blinker og saetter selv en ring. Tonen stiger
        // med farten, saa man kan HOERE duellen spidse til.
        Assets.Play("*bat", 1f, _fart / StartFart);
        var punkt = Position with { X = other.Position.X + other.Collider!.Width / 2f * xRetning };
        context.Publish(new BatRamt(other, punkt));

        // Skub bolden fri af battet, saa den ikke rammer igen naeste frame.
        float fri = other.Collider!.Width / 2f + Stoerrelse / 2f + 1f;
        Position = Position with { X = other.Position.X + fri * xRetning };
    }

    /// <summary>
    /// Faar kuglen til at rulle som om den koerer paa en flade der vender mod kameraet.
    /// Rulleaksen staar vinkelret paa retningen, og vinklen foelger den tilbagelagte vej.
    /// </summary>
    private void Rul(float dt)
    {
        float radius = Stoerrelse / 2f;
        float vinkel = _fart * dt / radius * Rullefaktor;             // radianer
        var akse = Vector3.Normalize(new Vector3(-Retning.Y, Retning.X, 0));
        _drejning = Quaternion.Concatenate(_drejning, Quaternion.CreateFromAxisAngle(akse, vinkel));
    }

    public override void Render() => Draw.Model("bold", Position, _drejning, Stoerrelse / 2f);
}

