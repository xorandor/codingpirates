# Breakout

Sla muren af klodser i stykker med en studsende bold, foer du mister alle dine liv.
Nogle klodser taber en power-up naar de gaar i stykker - saml den op med battet.
Rullende 3D-bold, lys, stjernehimmel og lyd - alt sammen lavet af kode, ingen filer.

## Power-ups

- **Blaa (UdvidBat)** - battet bliver bredere en overgang.
- **Oranje (EkstraBold)** - endnu en bold kommer i spil.
- **Groen (LangsomBold)** - bolden bliver langsommere (den speeder selv op igen, naar den rammer battet et par gange).
- **Lyseroed (EkstraLiv)** - et ekstra liv.

## Saadan faar du spillet

1. Kopier HELE `Breakout`-mappen ind i `MyGames/` - i **Stifinder** eller terminalen,
   ALDRIG inde fra Visual Studio. (VS aendrer projektfilen naar den kopierer, og saa
   kompilerer dit spil bare ikke, uden fejl.)
2. Lav en fil `program.cs` i roden af `kraken/` (eller ret den du har) med een linje:

   ```csharp
   BreakoutGame.Run();
   ```

   Selve spillet - alt det der bliver lagt i motoren - staar i `MyGames/Breakout/BreakoutGame.cs`.
3. `dotnet run`

Eller nemmest: bed Claude - "kopier Breakout-skabelonen ind som mit spil".

Ret ALDRIG i skabelonen her i mappen - kopier den ud, og rod saa alt det du vil.

## Proev at aendre...

- Farverne og pointene per raekke: `KlodsBane.Farver` og `KlodsBane.PointPrRaekke`.
- `KlodsBane.Raekker = 8` eller `Kolonner = 14` - stoerre mur.
- En raekke klodser der skal rammes to gange: giv dem `Slag = 2` i `KlodsBane.Byg`.
- `PowerUpSpawner.Chance = 0.4f` - power-ups regner ned.
- Din egen power-up-type: udvid `PowerUpType` i `Beskeder.cs`, og lad enten `Bat` eller `Bold`
  lytte efter den (ligesom UdvidBat og LangsomBold goer det selv).
- Boldens `FartFaktor = 1.1f` - saa eskalerer farten hurtigere for hvert baet-slag.
- En raekke klodser der bevaeger sig frem og tilbage (kig i `Klods.Update`).
- Tryk F3 midt i et spil.
- Slet `Bold.cs` og skriv din egen bold helt forfra. Kontrakten staar i `Beskeder.cs`.
