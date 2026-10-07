# Asteroids

Skyd asteroiderne i stykker og overlev saa laenge du kan. Pil venstre/hoejre drejer skibet,
pil op giver gas i den retning naesen peger (der er ingen bak og ingen bremse!), mellemrum skyder.
Flyver du ud over kanten, kommer du ind paa den modsatte side. Store asteroider gaar i to
mellemstore, mellemstore i to smaa - og asteroiderne stoeder ind i hinanden og preller af.
Det bliver svaerere for hvert halve minut. Highscore-spil: naar du doer, skriver du dit navn,
og listen staar paa startskaermen. 3D-skib, klippestykker, lys, stjernehimmel og lyd - alt
sammen lavet af kode, ingen filer.

## Opgraderinger

Nogle asteroider taber noget naar de skydes. Saml det op med skibet - men skynd dig, det blinker
og forsvinder efter nogle sekunder.

- **Lyseroed (+1 LIV)** - et ekstra liv.
- **Orange (FLERE SKUD)** - tre skud i en vifte, og naeste gang fem.
- **Blaa (HURTIGERE)** - skibet skyder hurtigere. Kan samles tre gange.

Mister du et liv, mister du ogsaa alle dine skud-opgraderinger. Saa pas paa.

Highscore-listen gemmes i `Assets/mine/asteroids-highscore.txt` - din egen, den kommer ikke i git.

## Saadan faar du spillet

1. Kopier HELE `Asteroids`-mappen ind i `MyGames/` - i **Stifinder** eller terminalen,
   ALDRIG inde fra Visual Studio. (VS aendrer projektfilen naar den kopierer, og saa
   kompilerer dit spil bare ikke, uden fejl.)
2. Lav en fil `program.cs` i roden af `kraken/` (eller ret den du har) med een linje:

   ```csharp
   AsteroidsGame.Run();
   ```

   Selve spillet - alt det der bliver lagt i motoren - staar i `MyGames/Asteroids/AsteroidsGame.cs`.
3. `dotnet run`

Eller nemmest: bed Claude - "kopier Asteroids-skabelonen ind som mit spil".

Ret ALDRIG i skabelonen her i mappen - kopier den ud, og rod saa alt det du vil.

Har du allerede Pong eller Breakout i `MyGames/`, kan de godt ligge der samtidig - Asteroids
bruger sine egne klassenavne.

## Proev at aendre...

- Skibets `Drejefart`, `Acceleration` og `Modstand` - saet `Modstand = 0` og flyv som i det rigtige rum.
- `AsteroideSpawner.StartAntal = 8` - start midt i stormen. Eller `MinPause = 0.2f` - vanvid.
- `OpgraderingSpawner.Chance = 0.5f` - opgraderinger overalt.
- Point per stoerrelse (`Asteroide.Point`) - eller giv de smaa 500, saa det er dem der er guld vaerd.
- Asteroidernes farve - en roed klippeplanet? `Farve` i `Asteroide`, eller saet den per asteroide i `AsteroideSpawner.Spawn`.
- `Skib.MaxNiveau = 5` - og udvid viften i `Skib.Skyd` til 7 og 9 skud.
- Lad opgraderinger overleve et mistet liv (kig i `Skib.Genstart`).
- Skud der kommer ind paa den anden side ligesom skibet (kig i `Skud.Update` og `Kant`).
- En ny opgradering, fx et skjold eller en bombe der rydder skaermen: udvid `OpgraderingsType`
  i `Beskeder.cs`, giv den farve og tekst i `Opgradering`, og lad `Skib` eller
  `OpgraderingsEffekter` lytte efter den.
- En UFO der flyver forbi og skyder efter dig. En ny komponent med maerkatet "asteroide" rammer
  skibet uden at skibet skal aendres.
- Tryk F3 midt i et spil.
- Slet `Asteroide.cs` og skriv din egen asteroide helt forfra. Kontrakten staar i `Beskeder.cs`.
