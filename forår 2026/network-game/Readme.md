Når du henter koden, mangler bevist filen `program.cs`. Denne fil skal du selv tilføje. Her er et eksempel på denne fil:

```csharp
using Components;
using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

var mode = args.Contains("server") ? GameMode.Server : GameMode.Client;

InitWindow(1000, 600, "network game");
SetTargetFPS(60);

var game = new GameEngine(Color.RayWhite, mode);

// Composition: add your components here
game.Add(new StaticText("Velkommen til mit nye spil", new Vector2(400, 0), Color.Magenta));
game.Add(new FloatingText("Så er vi i gang!", 120, 200));
game.Add(new NetworkBallManager(speed: 300, showNames: true));

game.Run();
```

---

## Komponenter

Her er eksempler på alle tilgængelige komponenter og hvordan de bruges.

---

### StaticText
Viser en tekst på en fast position på skærmen.

```csharp
// Vis en titel øverst på skærmen
game.Add(new StaticText("Mit spil", new Vector2(400, 10), Color.DarkBlue));

// Med større skrifttype
game.Add(new StaticText("GAME OVER", new Vector2(300, 250), Color.Red, fontSize: 60));
```

---

### FloatingText
Tekst der flyver rundt og bouncer på kanten af skærmen.

```csharp
// Tekst der flyver i 120 graders vinkel med hastighed 200
game.Add(new FloatingText("Hej verden!", 120, 200));

// Tekst der flyver lodret ned
game.Add(new FloatingText("Fald!", 270, 150));
```

---

### ArrowKeyControlledBall
En bold der styres med piletasterne. Kan bruges både med og uden netværk.

```csharp
// En simpel bold uden netværk
game.Add(new ArrowKeyControlledBall(
    position: new Vector2(500, 300),
    speed: 250,
    radius: 20,
    color: Color.Blue
));

// Med navn vist over bolden
game.Add(new ArrowKeyControlledBall(
    position: new Vector2(500, 300),
    speed: 250,
    radius: 20,
    color: Color.Green,
    name: "Spiller 1",
    showName: true
));
```

---

### NetworkBallManager
Håndterer automatisk en bold per spiller over netværket. Hver spiller styrer sin egen bold med piletasterne.

```csharp
// Grundlæggende multiplayer-bold manager
game.Add(new NetworkBallManager(speed: 300));

// Med navne vist over boldene
game.Add(new NetworkBallManager(speed: 250, showNames: true));
```

---

### Player
En spiller der styres med WASD. Understøtter liv, møntindsamling og push-evne.

```csharp
// En simpel spiller midt på skærmen
var player = new Player(
    position: new Vector2(500, 300),
    speed: 200,
    radius: 20,
    color: Color.Blue
);
game.Add(player);

// Spiller med 3 liv, begrænset til skærmen, og push-evne med mellemrum
var player = new Player(
    position: new Vector2(500, 300),
    speed: 200,
    radius: 20,
    color: Color.Green,
    constrainToScreen: true,
    maxLives: 3,
    pushKey: KeyboardKey.Space,
    pushRadius: 120f,
    pushCooldown: 2f
);
game.Add(player);

// Reager på events
player.OnCoinCollected += (sender, coin) => score.Points += 10;
player.OnPlayerDied += (sender, e) => Console.WriteLine("Spilleren døde!");
```

---

### Coin
En mønt der kan samles op. Bruges typisk sammen med `Player` og `CoinCounter`.

```csharp
// En mønt på en fast position
game.Add(new Coin(new Vector2(300, 200)));

// En større mønt med egne farver
game.Add(new Coin(
    position: new Vector2(600, 400),
    radius: 20f,
    innerColor: Color.Orange,
    outerColor: Color.Yellow
));
```

---

### CoinCounter
Viser antallet af mønter der aktuelt er på skærmen.

```csharp
// Vis antal mønter øverst til venstre
game.Add(new CoinCounter(new Vector2(10, 10)));

// Med større skrift og rød farve
game.Add(new CoinCounter(new Vector2(10, 10), fontSize: 40, color: Color.Red));
```

---

### FallingCoin
En mønt der falder ned over skærmen. Bruges automatisk af `WinCondition` som vindende effekt.

```csharp
// En faldende mønt fra toppen af skærmen
game.Add(new FallingCoin(new Vector2(300, 0), speed: 200));

// Med større radius
game.Add(new FallingCoin(new Vector2(500, -20), speed: 300, radius: 20f));
```

---

### Score
Viser en score på skærmen. `Points` sættes manuelt fra andre komponenter.

```csharp
var score = new Score(new Vector2(10, 10));
game.Add(score);

// Opdater scoren et andet sted, f.eks. i et event
player.OnCoinCollected += (sender, coin) => score.Points += 10;

// Med eget label og farve
var score = new Score(new Vector2(10, 10), fontSize: 40, color: Color.DarkGreen, label: "Point");
game.Add(score);
```

---

### ManualScoreCounter
Som `Score`, men beregnet til at blive styret helt manuelt uden events.

```csharp
var counter = new ManualScoreCounter(new Vector2(10, 50), label: "Liv");
game.Add(counter);

// Skift værdien direkte
counter.Score = 3;
```

---

### HighScore
Husker den højeste score der er opnået i spillets levetid. Brug `Submit()` for at opdatere.

```csharp
var highScore = new HighScore(new Vector2(10, 50));
game.Add(highScore);

// Send den nuværende score som kandidat til high score
player.OnPlayerDied += (sender, e) => highScore.Submit(score.Points);

// Med eget label
var highScore = new HighScore(new Vector2(10, 50), fontSize: 30, color: Color.Purple, label: "Rekord");
game.Add(highScore);
```

---

### CircleShooter
En cirkel med en kanon der kan skyde kugler. Kanonen roterer automatisk. Skyd med tast, mus eller automatisk.

```csharp
// Skyd med mellemrum
game.Add(new CircleShooter(new Vector2(500, 300), shootKey: KeyboardKey.Space));

// Skyd med venstre musetast
game.Add(new CircleShooter(new Vector2(500, 300), shootButton: MouseButton.Left));

// Skyd automatisk hvert 500 ms, med bouncing kugler
game.Add(new CircleShooter(
    position: new Vector2(500, 300),
    radius: 20,
    stickLength: 40,
    color: Color.DarkBlue,
    rotationSpeed: 2f,
    bulletSpeed: 500f,
    bulletRadius: 8f,
    autoShootIntervalMs: 500f,
    maxBounces: 3
));
```

---

### CircleShooterBallCounter
Viser antallet af kugler fra `CircleShooter` der aktuelt flyver rundt på skærmen.

```csharp
// Vis antal kugler øverst til venstre
game.Add(new CircleShooterBallCounter(new Vector2(10, 10)));

// Med større skrift
game.Add(new CircleShooterBallCounter(new Vector2(10, 10), fontSize: 40, color: Color.DarkRed));
```

---

### PowerBuffCoinMagnet
Et power-up objekt der giver spilleren en magnet-evne til at tiltrække mønter, når den samles op.

```csharp
// En magnet power-up midt på banen
game.Add(new PowerBuffCoinMagnet(new Vector2(400, 300)));

// Med 10 sekunders varighed og stor rækkevidde
game.Add(new PowerBuffCoinMagnet(
    position: new Vector2(400, 300),
    duration: 10f,
    pullRadius: 300f,
    radius: 25f
));
```

---

### PowerBuffExtraHealth
Et power-up objekt der giver spilleren et ekstra liv, når den samles op.

```csharp
// Et hjerte power-up på skærmen
game.Add(new PowerBuffExtraHealth(new Vector2(200, 150)));

// Med større størrelse
game.Add(new PowerBuffExtraHealth(new Vector2(200, 150), radius: 30f));
```

---

### WinCondition
Holder øje med `Score` og aktiverer en vinde-animation når en bestemt pointgrænse nås.

```csharp
// Vind når scoren når 100
game.Add(new WinCondition(scoreToWin: 100));

// Vind når scoren når 50
game.Add(new WinCondition(scoreToWin: 50));
```