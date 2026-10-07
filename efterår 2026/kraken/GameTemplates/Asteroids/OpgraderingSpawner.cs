using System.Numerics;
using Kraken;

namespace Mine;

/// <summary>
/// Lytter efter skudte asteroider og lader nogle af dem tabe en opgradering.
/// Kender hverken Asteroide eller Skib - kun beskeden AsteroideSkudt.
/// </summary>
public class OpgraderingSpawner : Component
{
    /// <summary>Chancen for at en skudt asteroide taber noget. 0.12 = ca. hver ottende.</summary>
    public float Chance { get; set; } = 0.12f;

    /// <summary>Hvor tit det bliver et ekstra liv i stedet for en skud-opgradering. 0.25 = hver fjerde.</summary>
    public float LivAndel { get; set; } = 0.25f;

    public float Fart { get; set; } = 40f;
    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    public override void OnAdded(GameContext context) => context.On<AsteroideSkudt>(e =>
    {
        var r = Random.Shared;
        if (r.NextSingle() > Chance) return;

        var type = r.NextSingle() < LivAndel
            ? OpgraderingsType.EkstraLiv
            : r.Next(2) == 0 ? OpgraderingsType.FlereSkud : OpgraderingsType.HurtigereSkud;

        float vinkel = r.NextSingle() * MathF.PI * 2f;
        context.Add(new Opgradering
        {
            Position = e.Position,
            Type = type,
            Fart = new Vector2(MathF.Cos(vinkel), MathF.Sin(vinkel)) * Fart,
            BaneBredde = BaneBredde,
            BaneHoejde = BaneHoejde
        });
    });
}
