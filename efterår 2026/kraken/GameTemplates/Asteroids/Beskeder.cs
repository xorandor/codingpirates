using System.Numerics;
using Raylib_cs;

namespace Mine;

// Kontrakten mellem skibet, skuddene, asteroiderne og opgraderingerne. Ingen af dem kender
// hinandens type - de publisher og lytter, gennem beskederne herunder. Se README.md.

/// <summary>De tre slags opgraderinger en asteroide kan tabe. Saet paa Opgradering.Type.</summary>
public enum OpgraderingsType { EkstraLiv, FlereSkud, HurtigereSkud }

/// <summary>Publish naar et skud rammer en asteroide, og den gaar i stykker. Bruges til drops, rystelse og lyd.</summary>
public record AsteroideSkudt(Vector3 Position, int Stoerrelse, int Point);

/// <summary>Publish naar skibet rammer en asteroide. Spilstyring traekker et liv fra.</summary>
public record SkibRamt(Vector3 Position);

/// <summary>Publish naar skibet samler en opgradering op. Skib og OpgraderingsEffekter reagerer paa hver deres typer.</summary>
public record OpgraderingSamlet(OpgraderingsType Type, Vector3 Punkt, Color Farve);

/// <summary>Publish af Spilstyring hver gang spillet bliver et trin svaerere.</summary>
public record NytNiveau(int Niveau);

/// <summary>Publish af NavneSkaerm naar navnet er gemt paa listen. Velkomstskaerm viser saa listen igen.</summary>
public record HighscoreGemt;
