using System.Numerics;
using Raylib_cs;

namespace Mine;

// Kontrakten mellem klodserne, bolden, battet og power-uppene. Ingen af dem kender
// hinandens type - de publisher og lytter, gennem beskederne herunder. Se README.md.

/// <summary>De fire slags power-ups der kan falde ned fra en odelagt klods. Saet paa PowerUp.Type.</summary>
public enum PowerUpType { UdvidBat, EkstraBold, LangsomBold, EkstraLiv }

/// <summary>Publish naar en klods gaar i stykker. Bruges til point, power-up-chancen og kamerarystet.</summary>
public record KlodsOdelagt(Vector3 Position, Color Farve, int Point);

/// <summary>Publish naar battet samler en power-up op. Bat og Bold reagerer selv paa deres egne typer.</summary>
public record PowerUpSamlet(PowerUpType Type, Vector3 Punkt, Color Farve);

/// <summary>Publish naar den sidste bold falder forbi battet, og der mistes et liv.</summary>
public record LivMistet;

/// <summary>Publish naar sidste klods er ryddet af banen. Signalerer sejr.</summary>
public record KlodserRyddet;
