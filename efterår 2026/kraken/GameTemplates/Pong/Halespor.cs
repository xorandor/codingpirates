using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En kometsvans efter noget der bevaeger sig. Husker de sidste positioner og tegner
/// falmende kugler i dem. Foelger det foerste den finder med maerkatet.
///   game.Add(new Halespor { Tag = "bold" });
/// </summary>
public class Halespor : Component
{
    public string Tag { get; set; } = "bold";
    public int Laengde { get; set; } = 14;
    public Color Farve { get; set; } = new(180, 200, 255, 255);

    private readonly Queue<Vector3> _spor = new();
    private float _radius = 8f;

    public override void Update(GameContext context)
    {
        var maal = context.FindByTag(Tag).FirstOrDefault();
        if (maal == null) { _spor.Clear(); return; }

        // Sprang den langt (ny serv)? Saa start svansen forfra i stedet for at traekke en streg.
        if (_spor.Count > 0 && Vector3.Distance(_spor.Last(), maal.Position) > 100f) _spor.Clear();

        _spor.Enqueue(maal.Position);
        while (_spor.Count > Laengde) _spor.Dequeue();

        _radius = maal.Collider is { } c ? c.Width / 2f : 8f;
        Position = maal.Position - new Vector3(0, 0, 1);   // lige bag - saa tegnes svansen foer bolden
    }

    public override void Render()
    {
        int i = 0;
        foreach (var punkt in _spor)
        {
            float andel = (float)i++ / Laengde;                       // 0 aeldst, 1 nyest
            var farve = Farve with { A = (byte)(140 * andel) };
            Draw.Ball(punkt - new Vector3(0, 0, 1), _radius * (0.25f + 0.6f * andel), farve);
        }
    }
}
