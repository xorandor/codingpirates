using System.Numerics;
using Kraken;

namespace Mine;

/// <summary>
/// Rykker i kameraet naar en klods gaar i stykker (lille rysten) eller et liv mistes
/// (stoerre rysten): et tilfaeldigt skub der falder til ro, og et vip paa nogle grader.
/// </summary>
public class KameraRyk : Component
{
    public float Styrke { get; set; } = 6f;
    public float StyrkeLivMistet { get; set; } = 18f;
    public float VipGrader { get; set; } = 3f;
    public float Varighed { get; set; } = 0.25f;

    private float _tid;
    private float _aktivStyrke;

    public override void OnAdded(GameContext context)
    {
        context.On<KlodsOdelagt>(_ => Ryst(Styrke));
        context.On<LivMistet>(_ => Ryst(StyrkeLivMistet));
    }

    private void Ryst(float styrke)
    {
        _tid = Varighed;
        _aktivStyrke = styrke;
    }

    public override void Update(GameContext context)
    {
        if (_tid <= 0f) return;

        _tid -= context.DeltaTime;
        float andel = MathF.Max(_tid / Varighed, 0f);

        var r = Random.Shared;
        context.Camera.Target = new Vector3(
            (r.NextSingle() * 2f - 1f) * _aktivStyrke * andel,
            (r.NextSingle() * 2f - 1f) * _aktivStyrke * andel,
            0);
        context.Camera.Tilt = MathF.Sin(andel * MathF.PI) * VipGrader;

        if (_tid <= 0f)
        {
            context.Camera.Target = Vector3.Zero;
            context.Camera.Tilt = 0f;
        }
    }
}
