using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Bolden. Studser paa vaegge, bat og klodser. Falder den forbi battet mistes et liv (eller,
/// er der flere bolde i spil paa grund af en EkstraBold-power-up, forsvinder den bare).
/// </summary>
public class Bold : Component
{
    public float Stoerrelse { get; set; } = 24f;
    public float StartFart { get; set; } = 420f;
    public float MaxFart { get; set; } = 780f;
    public float FartFaktor { get; set; } = 1.03f;
    public float MaxUdgangsvinkel { get; set; } = 55f;
    public float MaxServVinkel { get; set; } = 25f;
    public float ServPause { get; set; } = 0.8f;
    public Color Farve { get; set; } = Color.RayWhite;
    public Color FarveTern { get; set; } = new(40, 40, 40, 255);

    /// <summary>Banens maal. Faste - kameraet er bare et vindue og maa gerne zoome.</summary>
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    /// <summary>1 = ruller praecis som en rigtig kugle. Mindre tal ruller langsommere.</summary>
    public float Rullefaktor { get; set; } = 0.4f;

    /// <summary>Hvor laenge LangsomBold-effekten varer.</summary>
    public float LangsomVarighed { get; set; } = 6f;

    /// <summary>Saet begge for at spawne bolden allerede i fart - bruges af Kopi() til EkstraBold.</summary>
    public Vector3? StartPosition { get; set; }
    public Vector2? StartRetning { get; set; }

    private float _fart;
    private bool _venter;
    private Vector2 _retning;
    private Quaternion _drejning = Quaternion.Identity;
    private readonly Queue<Vector3> _spor = new();
    private const int SporLaengde = 10;
    private const float LangsomFaktor = 0.6f;
    private float _langsomTid;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Circle(Stoerrelse / 2f);
        Tags.Add("bold");

        // Kuglen og dens ternede overflade laves af koden - ingen filer noedvendige.
        Assets.Checkered("bold-tern", Farve, FarveTern, cells: 6);
        Assets.Ball("bold", "bold-tern");
        Assets.Tone("*serv", 440, 660, 0.06f);
        Assets.Tone("*vaeg", 300, 220, 0.05f);

        context.On<GameStarted>(_ => Serv(context));
        context.On<PowerUpSamlet>(e =>
        {
            // Klarer selv sin LangsomBold-effekt - den taler kun om sig selv.
            if (e.Type != PowerUpType.LangsomBold) return;

            if (_langsomTid <= 0f) _fart *= LangsomFaktor;   // ikke gange igen hvis den allerede er aktiv
            _langsomTid = LangsomVarighed;
        });

        if (StartPosition is { } pos && StartRetning is { } retning)
        {
            Position = pos;
            _retning = retning;
            _fart = StartFart;
            _venter = false;
        }
        else
        {
            Serv(context);
        }
    }

    private void Serv(GameContext context)
    {
        var bat = context.FindByTag("bat").FirstOrDefault();
        Position = (bat?.Position ?? Vector3.Zero) + new Vector3(0, Stoerrelse, 0);
        _retning = Vector2.Zero;
        _fart = StartFart;
        _venter = true;
        _spor.Clear();

        context.After(ServPause, () =>
        {
            float vinkelFraLodret = (Random.Shared.NextSingle() * 2f - 1f) * MaxServVinkel;
            float vinkelRad = vinkelFraLodret * MathF.PI / 180f;
            _retning = new Vector2(MathF.Sin(vinkelRad), MathF.Cos(vinkelRad));
            _venter = false;
            Assets.Play("*serv");
        });
    }

    public override void Update(GameContext context)
    {
        if (_venter)
        {
            // Ligger og venter paa serven - foelger battet, saa man kan sigte.
            var bat = context.FindByTag("bat").FirstOrDefault();
            if (bat != null) Position = Position with { X = bat.Position.X };
            return;
        }

        if (_langsomTid > 0f)
        {
            _langsomTid -= context.DeltaTime;
            if (_langsomTid <= 0f) _fart = MathF.Min(_fart / LangsomFaktor, MaxFart);
        }

        Position += new Vector3(_retning.X, _retning.Y, 0) * _fart * context.DeltaTime;
        Rul(context.DeltaTime);

        _spor.Enqueue(Position);
        while (_spor.Count > SporLaengde) _spor.Dequeue();

        // Venstre og hoejre vaeg.
        float sideKant = BaneBredde / 2f - Stoerrelse / 2f;
        if ((Position.X > sideKant && _retning.X > 0) || (Position.X < -sideKant && _retning.X < 0))
        {
            _retning = _retning with { X = -_retning.X };
            Assets.Play("*vaeg");
        }

        // Toppen.
        float topKant = BaneHoejde / 2f - Stoerrelse / 2f;
        if (Position.Y > topKant && _retning.Y > 0)
        {
            _retning = _retning with { Y = -_retning.Y };
            Assets.Play("*vaeg");
        }

        // Forbi battet, langt under banen.
        if (Position.Y < -BaneHoejde / 2f - Stoerrelse) Mistet(context);
    }

    private void Mistet(GameContext context)
    {
        bool sidsteBold = !context.Find<Bold>().Any(b => !ReferenceEquals(b, this));

        if (!sidsteBold)
        {
            context.Remove(this);   // andre bolde er stadig i spil - ingen straf
            return;
        }

        context.State.Add("liv", -1);
        context.Publish(new LivMistet());

        // Samme bold, forfra fra battet - IKKE Kopi(), den ville genbruge den doedelige position.
        // Er der ingen liv tilbage, fjerner vi den IKKE - saa mister den sit GameStarted-
        // abonnement, og der kommer aldrig en bold igen naar man trykker Enter for at spille om.
        // Den bliver i stedet liggende usynligt under banen, indtil Slutskaerm blokerer spillet.
        if (context.State.Number("liv") > 0) Serv(context);
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (_venter) return;

        if (other.HasTag("bat")) StudFraBat(other);
        else if (other.HasTag("klods")) StudFraKlods(other);
    }

    /// <summary>
    /// Hvor paa battet ramte vi? -1 i venstre kant, +1 i hoejre. Det bestemmer vinklen ud,
    /// ligesom Pongs bat - bare drejet 90 grader, for her er det op i stedet for til siden.
    /// </summary>
    private void StudFraBat(Component bat)
    {
        if (_retning.Y > 0) return;   // kun naar bolden er paa vej NED i battet

        float halvBredde = bat.Collider!.Width / 2f;
        float traef = Math.Clamp((Position.X - bat.Position.X) / halvBredde, -1f, 1f);
        float vinkelRad = traef * MaxUdgangsvinkel * MathF.PI / 180f;

        _retning = new Vector2(MathF.Sin(vinkelRad), MathF.Cos(vinkelRad));
        _fart = MathF.Min(_fart * FartFaktor, MaxFart);

        // Skub bolden fri af battet, saa den ikke rammer igen naeste frame.
        Position = Position with { Y = bat.Position.Y + bat.Collider!.Height / 2f + Stoerrelse / 2f + 1f };
    }

    /// <summary>
    /// Studser af en klods. Ser paa hvilken led (x eller y) bolden er mindst inde i klodsen,
    /// og vender retningen paa den led - saa en flad top-ramning vender y, en side-ramning x.
    /// </summary>
    private void StudFraKlods(Component klods)
    {
        float dx = Position.X - klods.Position.X;
        float dy = Position.Y - klods.Position.Y;
        float halvBredde = klods.Collider!.Width / 2f + Stoerrelse / 2f;
        float halvHoejde = klods.Collider!.Height / 2f + Stoerrelse / 2f;

        float overlapX = halvBredde - MathF.Abs(dx);
        float overlapY = halvHoejde - MathF.Abs(dy);

        if (overlapX < overlapY)
        {
            _retning = _retning with { X = -_retning.X };
            Position = Position with { X = klods.Position.X + MathF.Sign(dx) * halvBredde };
        }
        else
        {
            _retning = _retning with { Y = -_retning.Y };
            Position = Position with { Y = klods.Position.Y + MathF.Sign(dy) * halvHoejde };
        }
    }

    /// <summary>En ekstra bold, samme sted og fart som denne, men med en let drejet retning.</summary>
    public Bold Kopi()
    {
        float vinkel = MathF.Atan2(_retning.X, _retning.Y) + (Random.Shared.NextSingle() * 0.6f - 0.3f);
        var nyRetning = new Vector2(MathF.Sin(vinkel), MathF.Cos(vinkel));

        return new Bold
        {
            BaneBredde = BaneBredde, BaneHoejde = BaneHoejde, Stoerrelse = Stoerrelse,
            StartFart = _fart, MaxFart = MaxFart, FartFaktor = FartFaktor,
            MaxUdgangsvinkel = MaxUdgangsvinkel, MaxServVinkel = MaxServVinkel, ServPause = ServPause,
            Farve = Farve, FarveTern = FarveTern, Rullefaktor = Rullefaktor, LangsomVarighed = LangsomVarighed,
            StartPosition = Position, StartRetning = nyRetning
        };
    }

    /// <summary>Faar kuglen til at rulle som om den koerer paa en flade der vender mod kameraet.</summary>
    private void Rul(float dt)
    {
        float radius = Stoerrelse / 2f;
        float vinkel = _fart * dt / radius * Rullefaktor;
        var akse = Vector3.Normalize(new Vector3(-_retning.Y, _retning.X, 0));
        _drejning = Quaternion.Concatenate(_drejning, Quaternion.CreateFromAxisAngle(akse, vinkel));
    }

    public override void Render()
    {
        int i = 0;
        foreach (var punkt in _spor)
        {
            float andel = (float)i++ / SporLaengde;
            var farve = Farve with { A = (byte)(90 * andel) };
            Draw.Ball(punkt - new Vector3(0, 0, 1), Stoerrelse / 2f * (0.3f + 0.5f * andel), farve);
        }

        Draw.Model("bold", Position, _drejning, Stoerrelse / 2f);
    }

    // Et lille ur over bolden, der taeller ned mens LangsomBold-effekten er aktiv.
    public override void RenderUI()
    {
        if (_langsomTid <= 0f) return;

        var skaerm = Draw.ToScreen(Position + new Vector3(0, Stoerrelse, 0));
        PowerUpUr.Tegn(skaerm, _langsomTid / LangsomVarighed, Color.Green);
    }
}
