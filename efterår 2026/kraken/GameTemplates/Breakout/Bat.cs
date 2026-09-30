using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// Battet i bunden. Styres med pil venstre/hoejre. Bliver bredere en overgang naar det
/// samler en UdvidBat-power-up op - se PowerUpSamlet i Beskeder.cs.
/// </summary>
public class Bat : Component
{
    public float Fart { get; set; } = 620f;
    public float Bredde { get; set; } = 140f;
    public float Hoejde { get; set; } = 22f;
    public float Dybde { get; set; } = 30f;
    public Color Farve { get; set; } = Color.SkyBlue;

    /// <summary>Farven battet blinker i, naar bolden rammer det.</summary>
    public Color BlinkFarve { get; set; } = new(255, 235, 120, 255);
    public float BlinkTid { get; set; } = 0.1f;

    /// <summary>Banens bredde. Fast - kameraet er bare et vindue og maa gerne zoome.</summary>
    public float BaneBredde { get; set; } = 1280f;

    /// <summary>Hvor laenge UdvidBat-effekten varer.</summary>
    public float UdvidVarighed { get; set; } = 8f;

    private float _grundBredde;
    private float _blink;
    private float _udvidetTid;

    public override void OnAdded(GameContext context)
    {
        _grundBredde = Bredde;
        Collider ??= Collider.Box(Bredde, Hoejde);
        Tags.Add("bat");

        context.On<PowerUpSamlet>(e =>
        {
            if (e.Type != PowerUpType.UdvidBat) return;
            Bredde = _grundBredde * 1.6f;
            Collider = Collider.Box(Bredde, Hoejde);
            _udvidetTid = UdvidVarighed;
        });
    }

    public override void Update(GameContext context)
    {
        if (_blink > 0f) _blink -= context.DeltaTime;

        if (_udvidetTid > 0f)
        {
            _udvidetTid -= context.DeltaTime;
            if (_udvidetTid <= 0f)
            {
                Bredde = _grundBredde;
                Collider = Collider.Box(Bredde, Hoejde);
            }
        }

        Position += new Vector3(context.Input.Direction.X * Fart * context.DeltaTime, 0, 0);

        float graense = BaneBredde / 2f - Bredde / 2f;
        Position = Position with { X = Math.Clamp(Position.X, -graense, graense) };
    }

    // Battet opdager selv naar bolden rammer det - ingen besked noedvendig for det.
    public override void OnCollision(Component other, GameContext context)
    {
        if (other.HasTag("bold")) _blink = BlinkTid;
    }

    public override void Render()
    {
        var farve = _blink > 0f ? BlinkFarve : Farve;
        Draw.Cube(Position, new Vector3(Bredde, Hoejde, Dybde), farve);
    }

    // Et lille ur over battet, der taeller ned mens UdvidBat-effekten er aktiv.
    public override void RenderUI()
    {
        if (_udvidetTid <= 0f) return;

        var skaerm = Draw.ToScreen(Position + new Vector3(0, Hoejde * 1.6f, 0));
        PowerUpUr.Tegn(skaerm, _udvidetTid / UdvidVarighed, Color.SkyBlue);
    }
}
