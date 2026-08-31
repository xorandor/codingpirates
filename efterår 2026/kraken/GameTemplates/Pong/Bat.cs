using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

public enum Styring { Tastatur, Computer }

/// <summary>
/// Et bat. Det samme bat bruges i begge sider - Styring afgoer om det er dig eller computeren.
///
///   game.Add(new Bat { Position = new(-600, 0, 0), Styring = Styring.Tastatur, Fart = 520 });
///   game.Add(new Bat { Position = new(600, 0, 0), Styring = Styring.Computer, Fart = 420 });
///
/// Battet kender ikke boldens type. Det laeser dens retning gennem IHarRetning, og naar
/// bolden publisher BatRamt, blinker battet og saetter en ring hvor den ramte.
/// </summary>
public class Bat : Component
{
    public Styring Styring { get; set; } = Styring.Tastatur;
    public float Fart { get; set; } = 500f;
    public float Bredde { get; set; } = 18f;
    public float Hoejde { get; set; } = 110f;

    /// <summary>Hvor dybt battet er, ind i skaermen. Ses kun med F3 - eller med perspektiv.</summary>
    public float Dybde { get; set; } = 40f;
    public Color Farve { get; set; } = Color.RayWhite;

    /// <summary>Banens hoejde. Fast - kameraet er bare et vindue og maa gerne zoome.</summary>
    public float BaneHoejde { get; set; } = 720f;

    /// <summary>Computeren: hvor mange pixels den sigter forkert. Traekkes om ved hver serv.</summary>
    public float Sigtefejl { get; set; } = 25f;

    /// <summary>Farven battet blinker i, naar bolden rammer det.</summary>
    public Color BlinkFarve { get; set; } = new(255, 235, 120, 255);
    public float BlinkTid { get; set; } = 0.12f;

    /// <summary>Sekunder mellem to glimt der loeber hen over battet. 0 slaar glimtet fra.</summary>
    public float GlimtPeriode { get; set; } = 2.2f;

    private Vector3 _start;
    private float _fejl;
    private float _blink;
    private Shader _glimt;
    private int _locTime, _locCenter, _locSize, _locPeriod;

    public override void OnAdded(GameContext context)
    {
        _start = Position;
        Collider ??= Collider.Box(Bredde, Hoejde);
        Tags.Add("bat");
        Tags.Add(Position.X < 0 ? "venstre" : "hoejre");

        // Glimt-shaderen: lys.fs plus et baand der loeber hen over. Se Assets/shaders/glimt.fs.
        _glimt = Assets.Shader("shaders/lys.vs", "shaders/glimt.fs");
        _locTime = Raylib.GetShaderLocation(_glimt, "time");
        _locCenter = Raylib.GetShaderLocation(_glimt, "center");
        _locSize = Raylib.GetShaderLocation(_glimt, "size");
        _locPeriod = Raylib.GetShaderLocation(_glimt, "period");

        context.On<GameStarted>(_ => Position = _start);
        context.On<BoldenServes>(_ => _fejl = Random.Shared.NextSingle() * 2f * Sigtefejl - Sigtefejl);
        context.On<BatRamt>(e =>
        {
            if (!ReferenceEquals(e.Bat, this)) return;
            _blink = BlinkTid;
            context.Add(new Traefring { Position = e.Punkt });
        });
    }

    public override void Update(GameContext context)
    {
        if (_blink > 0f) _blink -= context.DeltaTime;

        float retning = Styring == Styring.Tastatur
            ? context.Input.Direction.Y
            : ComputerRetning(context);

        Position += new Vector3(0, retning * Fart * context.DeltaTime, 0);

        // Hold battet indenfor banen.
        float graense = BaneHoejde / 2f - Hoejde / 2f;
        Position = Position with { Y = Math.Clamp(Position.Y, -graense, graense) };
    }

    /// <summary>
    /// Computeren foelger kun bolden naar den er paa vej mod den. Ellers glider den
    /// tilbage mod midten. Det giver dig luft, og faar den til at se ud som om den taenker.
    /// </summary>
    private float ComputerRetning(GameContext context)
    {
        var bold = context.FindByTag("bold").FirstOrDefault();
        if (bold == null) return 0f;

        // Paa vej mod mig = retningen fra bolden hen til mig har samme fortegn som boldens fart.
        bool paaVejModMig = MathF.Sign(Position.X - bold.Position.X) == MathF.Sign(BoldRetningX(bold));
        float maal = paaVejModMig ? bold.Position.Y + _fejl : 0f;

        float afstand = maal - Position.Y;
        if (MathF.Abs(afstand) < 6f) return 0f;   // taet nok - staa stille i stedet for at sitre
        return MathF.Sign(afstand);
    }

    // Battet kender ikke boldens type - kun evnen "har en retning".
    private static float BoldRetningX(Component bold) => bold is IHarRetning r ? r.Retning.X : 0f;

    public override void Render()
    {
        var farve = _blink > 0f ? BlinkFarve : Farve;
        var size = new Vector3(Bredde, Hoejde, Dybde);

        if (GlimtPeriode <= 0f)
        {
            Draw.Cube(Position, size, farve);
            return;
        }

        Raylib.SetShaderValue(_glimt, _locTime, (float)Raylib.GetTime(), ShaderUniformDataType.Float);
        Raylib.SetShaderValue(_glimt, _locCenter, Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(_glimt, _locSize, new Vector2(Bredde, Hoejde), ShaderUniformDataType.Vec2);
        Raylib.SetShaderValue(_glimt, _locPeriod, GlimtPeriode, ShaderUniformDataType.Float);
        Draw.Shaded(_glimt, () => Draw.Cube(Position, size, farve));
    }
}
