using System.Numerics;

namespace Kraken;

/// <summary>
/// Basen for alt der lever i spillet. Arv fra denne klasse, og overskriv kun
/// de metoder du har brug for.
///
/// Livscyklus:
///   OnAdded     -> een gang, lige efter komponenten kommer med i spillet.
///                  Vinduet er aabent her, saa det er foerste sted du maa loade assets.
///   Update      -> een gang pr. frame. Al logik hoerer til her.
///   OnCollision -> naar din Collider roerer en anden komponents Collider.
///   Render      -> tegner i 3D-verdenen (x/y/z i world units, y peger opad).
///   RenderUI    -> tegner ovenpaa, i skaermkoordinater (0,0 er oeverste venstre hjoerne).
///   OnRemoved   -> een gang, naar komponenten fjernes igen.
/// </summary>
public abstract class Component
{
    /// <summary>Placering i verden. y peger OPAD. z er dit "lag" - hoejere z er taettere paa kameraet.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Saet til false for at slaa baade Update og Render fra uden at fjerne komponenten.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// True saa laenge komponenten er med i spillet. Bliver false naar den er fjernet.
    /// God til at tjekke om noget man gemmer en henvisning til stadig findes.
    /// </summary>
    public bool IsInGame { get; internal set; }

    /// <summary>
    /// Giv komponenten et eller flere maerkater, saa andre kan finde den uden at kende dens type:
    ///   game.Add(new Zombie { Tags = { "fjende" } });
    ///   foreach (var f in context.FindByTag("fjende")) ...
    /// </summary>
    public HashSet<string> Tags { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasTag(string tag) => Tags.Contains(tag);

    /// <summary>
    /// Formen motoren bruger til at opdage sammenstoed. null betyder at komponenten
    /// ikke kolliderer med noget:
    ///   Collider = Collider.Circle(20);
    ///   Collider = Collider.Box(40, 60);
    /// </summary>
    public Collider? Collider { get; set; }

    /// <summary>Navn(e) paa den/dem der har skrevet komponenten. Vises nederst paa skaermen.</summary>
    public virtual string? Credits => null;

    /// <summary>Naar true saettes alt andet paa pause (til menuer, game over-skaerme osv.).</summary>
    public virtual bool IsBlocking => false;

    /// <summary>
    /// Naar true bliver komponenten ved med at koere, ogsaa mens noget andet blokerer.
    /// Brug den til ting der skal leve videre bag en menu - musik, netvaerk, en baggrund
    /// der bliver ved med at rulle. Blokerende komponenter koerer altid.
    /// </summary>
    public virtual bool RunsWhileBlocked => IsBlocking;

    /// <summary>Naar true overlever komponenten et kald til RemoveAll().</summary>
    public virtual bool Persistent => false;

    // ------------------------------------------------------------- Netvaerk

    /// <summary>
    /// Navnet paa den slags ting komponenten er, fx "spiller" eller "moent".
    /// Er den null, findes komponenten kun paa den maskine der har lavet den.
    /// Er den sat, sender serveren komponenten videre til alle klienter.
    /// Se Readme for hvordan klienten laerer at tegne den.
    /// </summary>
    public virtual string? NetworkKind => null;

    /// <summary>
    /// True paa en klient, naar komponenten styres af serveren. Motoren kalder ikke
    /// Update paa den - dens Position kommer fra serveren. Render og RenderUI koerer som normalt.
    /// </summary>
    public bool IsRemote { get; internal set; }

    /// <summary>Ekstra tilstand ud over Position, som serveren skal sende til klienterne.</summary>
    public virtual void WriteState(StateWriter state) { }

    /// <summary>Modtager den ekstra tilstand serveren sendte. Koerer kun paa klienter.</summary>
    public virtual void ReadState(StateReader state) { }

    // ---------------------------------------------------------- Livscyklus

    public virtual void OnAdded(GameContext context) { }
    public virtual void Update(GameContext context) { }
    public virtual void OnCollision(Component other, GameContext context) { }
    public virtual void Render() { }
    public virtual void RenderUI() { }
    public virtual void OnRemoved(GameContext context) { }
}

// -----------------------------------------------------------------------------
// De faelles evner. En komponent der kan samles op, tage skade eller goere skade,
// implementerer et af disse interfaces. Saa kan andre komponenter bruge den uden
// nogensinde at kende dens rigtige type - og uden at filerne skal ligge samme sted.
// -----------------------------------------------------------------------------

/// <summary>Noget der kan samles op, fx en moent eller en power-up.</summary>
public interface ICollectable
{
    /// <summary>Hvor mange point det er vaerd. 0 hvis det ikke giver point.</summary>
    int Value { get; }

    /// <summary>Kaldes af motoren naar nogen samler den op. Her fjerner du typisk dig selv.</summary>
    void OnCollected(Component collector, GameContext context);
}

/// <summary>Noget der kan tage skade, fx en spiller eller en fjende.</summary>
public interface IDamageable
{
    void TakeDamage(int amount, Component source, GameContext context);
}

/// <summary>Noget der goer skade paa det, det rammer, fx en kugle eller en pigget bold.</summary>
public interface IHarmful
{
    int Damage { get; }
}

/// <summary>Noget der kan skubbes vaek, fx en kugle man afvaerger.</summary>
public interface IPushable
{
    void PushAwayFrom(Vector3 source);
}
