using System.Numerics;
using Kraken;

namespace Components;

/// <summary>
/// Lader kameraet foelge en figur, saa banen kan vaere stoerre end skaermen.
///
///   game.Add(new CameraFollow());                          // foelger din egen spiller
///   game.Add(new CameraFollow { Tag = "bil", Smoothing = 2f });
///
/// Paa en klient foelger den af sig selv den figur serveren har sagt er din.
/// </summary>
public class CameraFollow : Component
{
    /// <summary>Maerkatet paa det der skal foelges, naar der ikke er en egen figur at bruge.</summary>
    public string Tag { get; set; } = "spiller";

    /// <summary>Hvor blodt kameraet haler ind. Hoejere tal = strammere. 0 = klaeber fast.</summary>
    public float Smoothing { get; set; } = 5f;

    /// <summary>Laas kameraet paa en akse, fx til et sidescrollende spil.</summary>
    public bool FollowX { get; set; } = true;
    public bool FollowY { get; set; } = true;

    public override bool Persistent => true;

    /// <summary>Den komponent der foelges lige nu. Saet den selv hvis du vil bestemme.</summary>
    public Component? Target { get; set; }

    public override void Update(GameContext context)
    {
        var target = Target ?? context.MyEntity ?? context.FindByTag(Tag).FirstOrDefault();
        if (target == null) return;

        var wanted = context.Camera.Target;
        if (FollowX) wanted.X = target.Position.X;
        if (FollowY) wanted.Y = target.Position.Y;

        context.Camera.Target = Smoothing <= 0f
            ? wanted
            : Vector3.Lerp(context.Camera.Target, wanted, MathF.Min(1f, Smoothing * context.DeltaTime));
    }
}
