using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

/// <summary>
/// En klods i muren. Skal rammes Slag gange foer den gaar i stykker - saet den til mere end 1
/// for en klods der skal have et par tag foer den falder. Kender hverken bolden eller battet -
/// den maerker bare naar noget med maerkatet "bold" rammer den.
/// </summary>
public class Klods : Component
{
    public float Bredde { get; set; } = 96f;
    public float Hoejde { get; set; } = 30f;
    public Color Farve { get; set; } = Color.Red;
    public int Point { get; set; } = 10;
    public int Slag { get; set; } = 1;

    private int _tilbage;
    private float _blink;

    public override void OnAdded(GameContext context)
    {
        _tilbage = Slag;
        Collider ??= Collider.Box(Bredde, Hoejde);
        Tags.Add("klods");

        Assets.Tone("*klods", 700, 500, 0.04f);
        Assets.Tone("*klods-i-stykker", 600, 200, 0.08f);
    }

    public override void Update(GameContext context)
    {
        if (_blink > 0f) _blink -= context.DeltaTime;
    }

    public override void OnCollision(Component other, GameContext context)
    {
        if (!other.HasTag("bold")) return;

        _tilbage--;
        _blink = 0.08f;

        if (_tilbage > 0)
        {
            Assets.Play("*klods");
            return;
        }

        context.State.Add("point", Point);
        Assets.Play("*klods-i-stykker");
        context.Add(new Traefring { Position = Position, Farve = Farve });
        context.Publish(new KlodsOdelagt(Position, Farve, Point));
        context.Remove(this);
    }

    public override void Render()
    {
        var farve = _blink > 0f ? Color.White : Farve;
        Draw.Cube(Position, new Vector3(Bredde, Hoejde, 26f), farve);
    }
}
