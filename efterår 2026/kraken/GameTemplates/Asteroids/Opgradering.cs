using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En opgradering, der falder ud af en skudt asteroide. Driver langsomt rundt, blinker naar den
/// er ved at forsvinde, og giver sin effekt naar skibet samler den op - se OpgraderingsType i
/// Beskeder.cs. Kender ikke Skib, kun maerkatet "skib".
/// </summary>
public class Opgradering : Component
{
    public OpgraderingsType Type { get; set; }
    public Vector2 Fart { get; set; }
    public float Stoerrelse { get; set; } = 26f;

    /// <summary>Sekunder foer den forsvinder igen. De sidste 3 sekunder blinker den.</summary>
    public float Levetid { get; set; } = 9f;

    public float BaneBredde { get; set; } = 1280f;
    public float BaneHoejde { get; set; } = 720f;

    private float _tid;

    public override void OnAdded(GameContext context)
    {
        Collider ??= Collider.Box(Stoerrelse, Stoerrelse);
        Tags.Add("opgradering");
        Assets.Tone("*opgradering-samlet", 500, 1300, 0.15f, firkant: false);

        context.After(Levetid, () => context.Remove(this));
    }

    public override void Update(GameContext context)
    {
        _tid += context.DeltaTime;
        Position += new Vector3(Fart.X, Fart.Y, 0) * context.DeltaTime;
        Position = Kant.Wrap(Position, BaneBredde, BaneHoejde, Stoerrelse);
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (!other.HasTag("skib")) return;

        Assets.Play("*opgradering-samlet");
        context.Publish(new OpgraderingSamlet(Type, Position, FarveFor(Type)));
        context.Remove(this);
    }

    public override void Render()
    {
        // Blinker de sidste 3 sekunder: hver anden ottendedel sekund tegnes den ikke.
        if (Levetid - _tid < 3f && (int)(_tid * 8f) % 2 == 0) return;

        float vinkel = (float)(Raylib.GetTime() * 120.0 % 360.0);

        Rlgl.PushMatrix();
        Rlgl.Translatef(Position.X, Position.Y, Position.Z);
        Rlgl.Rotatef(vinkel, 0f, 1f, 0f);
        Draw.Cube(Vector3.Zero, new Vector3(Stoerrelse, Stoerrelse, Stoerrelse), FarveFor(Type));
        Rlgl.PopMatrix();
    }

    public override void RenderUI()
        => Draw.TextAbove(TekstFor(Type), Position, Stoerrelse, 16, FarveFor(Type));

    /// <summary>Farven for hver slags - bruges baade her og i beskeden, saa teksten kan matche.</summary>
    public static Color FarveFor(OpgraderingsType type) => type switch
    {
        OpgraderingsType.EkstraLiv => Color.Pink,
        OpgraderingsType.FlereSkud => Color.Orange,
        OpgraderingsType.HurtigereSkud => Color.SkyBlue,
        _ => Color.White
    };

    public static string TekstFor(OpgraderingsType type) => type switch
    {
        OpgraderingsType.EkstraLiv => "+1 LIV",
        OpgraderingsType.FlereSkud => "FLERE SKUD",
        OpgraderingsType.HurtigereSkud => "HURTIGERE",
        _ => ""
    };
}
