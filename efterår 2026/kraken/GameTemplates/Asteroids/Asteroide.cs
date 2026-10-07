using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En asteroide. Driver gennem rummet, snurrer, og kommer ind paa den modsatte side naar den
/// flyver ud. Rammes den af et skud, gaar den i stykker: en stor bliver til to mellemstore,
/// en mellemstor til to smaa, og en lille forsvinder. Asteroider stoeder ogsaa ind i hinanden
/// og preller af - det er den med det laveste nummer, der ordner sammenstoedet for begge.
/// Kender hverken skibet eller skuddene - kun maerkatet "skud".
/// </summary>
public class Asteroide : Component
{
    /// <summary>3 = stor, 2 = mellem, 1 = lille.</summary>
    public int Stoerrelse { get; set; } = 3;

    /// <summary>Retning OG hastighed i eet: hvor langt den flytter sig per sekund.</summary>
    public Vector2 Fart { get; set; }

    public Color Farve { get; set; } = new(150, 140, 125, 255);
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    /// <summary>Hvor meget hurtigere stumperne flyver end den asteroide, de kom fra.</summary>
    public float StumpFartFaktor { get; set; } = 1.25f;

    public float Radius => Stoerrelse switch { 3 => 58f, 2 => 34f, _ => 19f };
    public int Point => Stoerrelse switch { 3 => 20, 2 => 50, _ => 100 };

    private static int _naesteNummer;
    private int _nummer;
    private float _drejning;
    private float _drejefart;
    private Vector3 _akse;
    private readonly List<(Vector3 Sted, Vector3 Maal, Color Farve)> _klumper = [];

    public override void OnAdded(GameContext context)
    {
        _nummer = _naesteNummer++;
        Collider ??= Collider.Circle(Radius * 0.9f);
        Tags.Add("asteroide");

        var r = Random.Shared;
        _drejefart = 20f + r.NextSingle() * 70f;
        _akse = Vector3.Normalize(new Vector3(r.NextSingle() - 0.5f, r.NextSingle() - 0.5f, r.NextSingle() - 0.5f));

        // En klippe lavet af 4-6 terninger, der sidder lidt skaevt paa hinanden. Ingen filer.
        int antal = 4 + r.Next(3);
        for (int i = 0; i < antal; i++)
        {
            float spredning = i == 0 ? 0f : Radius * 0.45f;
            var sted = new Vector3((r.NextSingle() * 2f - 1f) * spredning, (r.NextSingle() * 2f - 1f) * spredning, (r.NextSingle() * 2f - 1f) * spredning * 0.5f);
            float maal = Radius * (i == 0 ? 1.2f : 0.6f + r.NextSingle() * 0.5f);
            byte lys = (byte)(r.Next(-20, 21));
            var farve = new Color((byte)Math.Clamp(Farve.R + lys, 0, 255), (byte)Math.Clamp(Farve.G + lys, 0, 255), (byte)Math.Clamp(Farve.B + lys, 0, 255), (byte)255);
            _klumper.Add((sted, new Vector3(maal), farve));
        }

        Assets.Noise("*asteroide-stor", 0.35f);
        Assets.Noise("*asteroide-lille", 0.15f);
        Assets.Tone("*asteroide-bump", 160, 90, 0.06f, firkant: false);
    }

    public override void Update(GameContext context)
    {
        Position += new Vector3(Fart.X, Fart.Y, 0) * context.DeltaTime;
        Position = Kant.Wrap(Position, BaneBredde, BaneHoejde, Radius);
        _drejning += _drejefart * context.DeltaTime;
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (other.HasTag("skud")) Ramt(context);
        else if (other is Asteroide anden && _nummer < anden._nummer) Stoed(anden);
    }

    /// <summary>Ramt af et skud: point, lyd, eksplosion - og to stumper, hvis den var stor nok.</summary>
    private void Ramt(GameContext context)
    {
        context.State.Add("point", Point);
        Assets.Play(Stoerrelse == 1 ? "*asteroide-lille" : "*asteroide-stor", 0.7f);
        context.Add(new Eksplosion { Position = Position, Antal = 6 + Stoerrelse * 5, Farve = Farve, Spredning = Radius });
        context.Publish(new AsteroideSkudt(Position, Stoerrelse, Point));

        if (Stoerrelse > 1)
        {
            float fart = Fart.Length() * StumpFartFaktor + 20f;
            float vinkel = MathF.Atan2(Fart.Y, Fart.X);
            float drej = (25f + Random.Shared.NextSingle() * 35f) * MathF.PI / 180f;

            LavStump(context, vinkel + drej, fart);
            LavStump(context, vinkel - drej, fart);
        }

        context.Remove(this);
    }

    private void LavStump(GameContext context, float vinkel, float fart)
    {
        var retning = new Vector2(MathF.Cos(vinkel), MathF.Sin(vinkel));
        context.Add(new Asteroide
        {
            Stoerrelse = Stoerrelse - 1,
            Position = Position + new Vector3(retning.X, retning.Y, 0) * Radius * 0.4f,
            Fart = retning * fart,
            Farve = Farve,
            BaneBredde = BaneBredde,
            BaneHoejde = BaneHoejde,
            StumpFartFaktor = StumpFartFaktor
        });
    }

    /// <summary>
    /// To asteroider preller af paa hinanden som billardkugler. De tunge (store) skubber mere
    /// end de lette. Bagefter skubbes de fra hinanden, saa de ikke haenger fast i hinanden.
    /// </summary>
    private void Stoed(Asteroide anden)
    {
        var fraMigTilAnden = new Vector2(anden.Position.X - Position.X, anden.Position.Y - Position.Y);
        float afstand = fraMigTilAnden.Length();
        var n = afstand > 0.001f ? fraMigTilAnden / afstand : new Vector2(1, 0);

        float m1 = Radius * Radius;              // "vaegt" - en stor vejer meget mere end en lille
        float m2 = anden.Radius * anden.Radius;

        float modHinanden = Vector2.Dot(Fart - anden.Fart, n);
        if (modHinanden > 0f)
        {
            // Kun hvis de er paa vej IND i hinanden - ellers er stoedet allerede sket.
            float stoed = 2f * modHinanden / (m1 + m2);
            Fart -= stoed * m2 * n;
            anden.Fart += stoed * m1 * n;
            Assets.Play("*asteroide-bump", 0.4f);
        }

        float overlap = Collider!.Width / 2f + anden.Collider!.Width / 2f - afstand;
        if (overlap > 0f)
        {
            Position -= new Vector3(n.X, n.Y, 0) * overlap * m2 / (m1 + m2);
            anden.Position += new Vector3(n.X, n.Y, 0) * overlap * m1 / (m1 + m2);
        }
    }

    public override void Render()
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(Position.X, Position.Y, Position.Z);
        Rlgl.Rotatef(_drejning, _akse.X, _akse.Y, _akse.Z);

        foreach (var (sted, maal, farve) in _klumper)
            Draw.Cube(sted, maal, farve);

        Rlgl.PopMatrix();
    }
}
