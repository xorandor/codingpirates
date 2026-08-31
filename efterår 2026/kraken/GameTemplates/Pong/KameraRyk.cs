using System.Numerics;
using Kraken;

namespace Mine;

/// <summary>
/// Rykker i kameraet naar nogen scorer: et lille tilfaeldigt skub der falder til ro,
/// og et vip paa nogle grader. Med stjernerne bagved kan man se parallaksen i rykket.
/// </summary>
public class KameraRyk : Component
{
    public float Styrke { get; set; } = 14f;
    public float VipGrader { get; set; } = 4f;
    public float Varighed { get; set; } = 0.35f;

    private float _tid;

    public override void OnAdded(GameContext context) => context.On<Maal>(_ => _tid = Varighed);

    public override void Update(GameContext context)
    {
        if (_tid <= 0f) return;

        _tid -= context.DeltaTime;
        float andel = MathF.Max(_tid / Varighed, 0f);              // 1 -> 0

        var r = Random.Shared;
        context.Camera.Target = new Vector3(
            (r.NextSingle() * 2f - 1f) * Styrke * andel,
            (r.NextSingle() * 2f - 1f) * Styrke * andel,
            0);
        context.Camera.Tilt = MathF.Sin(andel * MathF.PI) * VipGrader;

        if (_tid <= 0f)
        {
            context.Camera.Target = Vector3.Zero;
            context.Camera.Tilt = 0f;
        }
    }
}
