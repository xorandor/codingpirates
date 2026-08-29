# Dine egne komponenter

Alt i denne mappe er **dit**. Det kommer aldrig i git, og det bliver aldrig overskrevet
naar du henter ny kode ned. Du kan lave, aendre og slette lige saa mange filer du vil.

## Sadan laver du en

1. Lav en ny fil her i mappen, fx `Fjende.cs`.
2. Start filen med `namespace Mine;`
3. Lad klassen arve fra `Component`.
4. Brug den i `program.cs`.

Se `Eksempel.cs.template` for en fil du kan kopiere og bygge videre paa.

## Reglerne

- Filen skal ligge i namespace `Mine`.
- En komponent maa gerne bruge `Kraken` (motoren) og `Components` (de faelles komponenter).
- Vil du dele din komponent med de andre, saa sig til - saa flytter vi den over i
  `Components/` og den kommer med i git til alle.

## Egne billeder og lyde

Laeg dem i `Assets/mine/`. Den mappe er ogsaa din alene. Hent dem med
`Assets.Texture("mit-billede.png")` - Kraken kigger i `Assets/mine/` foerst.
