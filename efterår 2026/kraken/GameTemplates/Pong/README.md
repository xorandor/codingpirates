# Pong

Det klassiske Pong: du styrer venstre bat med pil op/ned, computeren har hoejre bat.
Foerste til 11 vinder. Rullende 3D-bold, lys, stjernehimmel, glimt paa battene og lyd -
alt sammen lavet af kode, ingen filer.

## Saadan faar du spillet

1. Kopier `program.cs` herfra op i roden af `kraken/` - oven i den der ligger der.
2. Kopier alle de andre `.cs`-filer herfra ind i `MyComponents/`.
3. `dotnet run`

Eller bed Claude: "kopier Pong-skabelonen ind som mit spil".

Ret ALDRIG i skabelonen her i mappen - kopier den ud, og rod saa alt det du vil.

## Proev at aendre...

- Farverne: battenes `Farve`, boldens tern, stjernernes `Antal`.
- Goer computeren lettere at slaa: skru ned for `Fart` eller op for `Sigtefejl` paa hoejre bat.
- `PointForAtVinde = 3` - hurtige dueller.
- Boldens `FartFaktor = 1.15f` - saa eskalerer det HURTIGT.
- Tryk F3 midt i en duel.
- Lad bolden blive mindre for hvert slag (kig i `Bold.OnCollision`).
- To bolde i spil paa een gang. Hvad sker der med kometsvansen - og hvorfor?
- En power-up midt paa banen der goer dit bat dobbelt saa hoejt naar bolden rammer den.
- Giv hoejre bat `Styring = Styring.Tastatur` og find ud af hvorfor to spillere paa eet
  tastatur ikke virker som haabet. (Tip: begge bat laeser det SAMME `context.Input`.)
- Slet `Bold.cs` og skriv din egen bold helt forfra. Kontrakten staar i `Beskeder.cs`.
