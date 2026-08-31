using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// En lyskilde. Saa snart der er een i spillet, faar kugler, terninger og modeller en lys
/// side og en skyggeside - og et lille blankt glimt. Uden Light tegnes alt fladt, som foer.
///
///   game.Add(new Light { Position = new(-300, 300, 600) });                 // en paere oppe til venstre
///   game.Add(new Light { Directional = true, Position = new(1, 1, 1) });   // som solen
///   game.Add(new Light { Color = Color.Orange, Intensity = 0.6f });
///
/// Lyset kan flytte sig: saet Position i Update, saa foelger lyset med. Op til 4 ad gangen.
/// Grundlyset (det der rammer skyggesiden) stilles paa game.Lighting.Ambient.
/// </summary>
public class Light : Component
{
    public Color Color { get; set; } = Color.White;

    /// <summary>1 = fuld styrke. Over 1 blaeser farverne ud, under 1 daemper.</summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>True = som solen: samme retning overalt, og Position er retningen. False = som en paere.</summary>
    public bool Directional { get; set; }

    /// <summary>Kun for retningslys: det punkt lyset peger mod.</summary>
    public Vector3 Target { get; set; }

    private readonly LightSource _source = new();

    public override void OnAdded(GameContext context)
    {
        // Standard: en paere oppe til venstre, ude fra siden. Staar lyset lige foran
        // (stor z, lille x/y), rammer det alt forfra - og saa LIGNER det at der intet lys er.
        if (Position == Vector3.Zero) Position = new Vector3(-500, 400, 250);

        Sync();
        context.Lighting.Lights.Add(_source);
    }

    public override void Update(GameContext context) => Sync();

    public override void OnRemoved(GameContext context) => context.Lighting.Lights.Remove(_source);

    private void Sync()
    {
        _source.Position = Position;
        _source.Target = Target;
        _source.Color = Color;
        _source.Intensity = Intensity;
        _source.Directional = Directional;
    }
}
