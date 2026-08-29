# Kraken

Spilmotoren vi bygger videre paa i efteraaret 2026.

## Kom i gang

1. Hent koden.
2. Kopier `program.cs.template` til `program.cs`.
3. Skriv `dotnet run`.

Det er det. `program.cs` er din fil - den kommer aldrig i git, saa du kan lave lige praecis
det spil du har lyst til.

```
dotnet run                              # spil alene (og vaer server for andre)
dotnet run -- klient                    # forbind til en andens server
dotnet run -- klient 192.168.1.25 Anna  # spring spoergeskaermene over
```

| Tast | Gor |
|---|---|
| **F3** | 3D-visning til og fra. Proev den! |
| Piletaster / WASD | Bevaeg dig |
| Mellemrum | Knap **A** (fx skub) |
| Venstre shift | Knap **B** (fx dash) |

---

## Verden er 3D

Kraken tegner altid i et 3D-rum, ogsaa selv om dit spil ser 2D ud. Kameraet staar bare og
kigger lige ind i det, saa du kan lade som om det er fladt - lige indtil du vil bruge en
3D-model eller en tekstur, og saa virker det bare.

| | |
|---|---|
| **x** | gaar til hoejre |
| **y** | gaar **OPAD**. Pil op giver +y. |
| **z** | er dit lag. Hoejere z ligger oveni lavere z. |
| **(0, 0, 0)** | er midt paa skaermen |
| **1 world unit** | er 1 pixel til at starte med |

> **NB:** y peger opad nu. I foraaret pegede den nedad. Det er den stoerste aendring fra
> sidste saeson, og den eneste du skal huske paa naar du kigger paa gammel kode.

---

## Sadan ser en komponent ud

Et spil i Kraken er en bunke komponenter. Hver komponent er en klasse i sin egen fil, der
arver fra `Component`. Du overskriver kun de metoder du har brug for.

```csharp
using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Mine;

public class Stjerne : Component
{
    public override string? Credits => "Dit navn";

    public float Size { get; set; } = 20f;
    public Color Color { get; set; } = Color.Gold;
    public float FallSpeed { get; set; } = 100f;

    public override void OnAdded(GameContext context)
        => Collider = Collider.Circle(Size);

    public override void Update(GameContext context)
        => Position -= new Vector3(0, FallSpeed * context.DeltaTime, 0);

    public override void Render()
        => Draw.Ball(Position, Size, Color);
}
```

Og saa bruger du den i `program.cs`:

```csharp
game.Add(new Stjerne { Position = new(0, 300, 0), Size = 30, Color = Color.White });
```

**Du skriver kun det du vil aendre.** Alt andet har allerede en fornuftig vaerdi. Ingen
lange lister af parametre i en constructor - bare krøllede parenteser og de ting du
faktisk gaar op i.

### De fem metoder

| Metode | Hvornaar | Hvad hoerer til her |
|---|---|---|
| `OnAdded` | een gang, naar komponenten kommer med | udregninger, colliders, indlaesning af billeder |
| `Update` | hver frame | al logik: bevaegelse, tastetryk, timere |
| `OnCollision` | naar din collider roerer en andens | hvad der sker ved et sammenstoed |
| `Render` | hver frame | tegning i 3D-verdenen |
| `RenderUI` | hver frame | tekst, score, hjerter - alt i skaermkoordinater |
| `OnRemoved` | een gang, naar komponenten fjernes | oprydning |

**Reglen:** `Render` tegner i verden, `RenderUI` tegner ovenpaa. Bland dem ikke sammen.
Skal du skrive et navn hen over en figur i verden, saa brug `Draw.TextAbove` fra `RenderUI`.

Billeder og lyde maa **foerst** loades i `OnAdded` - foer da er vinduet der ikke endnu.

### Flag du kan saette

| | |
|---|---|
| `Enabled` | `false` slaar baade Update og tegning fra, uden at fjerne komponenten |
| `Credits` | dit navn, vises nederst i spillet |
| `IsBlocking` | `true` saetter alt andet paa pause (menuer, game over) |
| `RunsWhileBlocked` | `true` = koer videre selv om noget andet blokerer (musik, baggrund) |
| `Persistent` | `true` = overlev `context.RemoveAll()` |
| `Tags` | maerkater andre kan finde dig paa: `Tags = { "fjende" }` |
| `Collider` | formen der bruges til kollision |
| `NetworkKind` | navnet serveren sender dig videre under |

---

## De fire maader komponenter snakker sammen paa

Det her er hjertet i Kraken. En komponent maa **aldrig** kende en anden komponents type.
Skriver du `if (other is Coin)`, saa kan din komponent ikke bruges i et spil uden mønter -
og dine egne komponenter i `MyComponents/` kan ikke vaere med.

### 1. Kollision

Giv din komponent en `Collider`, saa fortaeller motoren dig naar noget roerer dig.

```csharp
public override void OnAdded(GameContext context) => Collider = Collider.Circle(20);
public override void OnAdded(GameContext context) => Collider = Collider.Box(40, 60);

public override void OnCollision(Component other, GameContext context)
{
    Console.WriteLine($"Jeg ramte noget!");
}
```

Kollision regnes ud i xy-planet - z bliver ignoreret.

### 2. Evner (interfaces)

I stedet for at spoerge "er du en mønt?" spoerger man "kan du samles op?".

| Interface | Betyder | Du skal skrive |
|---|---|---|
| `ICollectable` | jeg kan samles op | `Value` og `OnCollected(...)` |
| `IDamageable` | jeg kan tage skade | `TakeDamage(...)` |
| `IHarmful` | jeg goer skade | `Damage` |
| `IPushable` | jeg kan skubbes vaek | `PushAwayFrom(...)` |

```csharp
public class Diamant : Component, ICollectable
{
    public int Value { get; set; } = 100;

    public void OnCollected(Component collector, GameContext context)
    {
        context.State.Add("score", Value);
        context.Remove(this);
    }
}
```

`Player` samler den op med det samme - uden at nogen har rettet en linje i `Player.cs`.
**Det er hele pointen.**

### 3. Beskeder

Naar noget skal vide besked, men ikke skal roere ved dig.

```csharp
// lyt - typisk i OnAdded. Ryddes op af sig selv naar komponenten fjernes.
context.On<GameOver>(_ => Console.WriteLine("aarh nej"));
context.On<Collected>(e => Console.WriteLine($"nogen fik {e.Value} point"));

// raab
context.Publish(new GameWon());
```

Motorens beskeder: `GameStarted`, `GameOver`, `GameWon`, `Collected`, `Damaged`,
`Healed`, `Died`, `ScoreChanged`, `PlayerJoined`, `PlayerLeft`.

Du maa gerne lave dine egne - en `record` i din egen fil er nok:
```csharp
public record BossVaagnede(Vector3 Hvor);
```

### 4. Faelles hukommelse og maerkater

```csharp
context.State.Add("score", 10);            // laeg 10 point til
int point = context.State.Number("score"); // laes dem igen
context.State.SetFlag("boss-doed", true);

foreach (var fjende in context.FindByTag("fjende"))
    context.Remove(fjende);
```

`Score`-komponenten viser bare det tal der ligger under `"score"`. Derfor kan enhver
komponent give point uden foerst at lede efter den.

### Og saa: timere

```csharp
context.After(2f, () => context.Remove(this));       // om 2 sekunder
var spawner = context.Every(0.5f, () => Spawn());    // hvert halve sekund
spawner.Cancel();                                    // ...indtil videre
```

---

## Tegneredskaber

I `Render()` (3D-verdenen):

```csharp
Draw.Ball(Position, 20, Color.Red);                     // kugle - ser ud som en cirkel
Draw.Cube(Position, new Vector3(40, 40, 40), Color.Blue);
Draw.Rectangle(Position, new Vector2(100, 20), Color.Green);
Draw.Line(startPunkt, slutPunkt, Color.Black);
Draw.Sprite("helt.png", Position, 60);                  // et billede
Draw.Model("skib.glb", Position, scale: 2f);            // en 3D-model
```

I `RenderUI()` (skaermen):

```csharp
Draw.Text("Hej", new Vector2(20, 20), 30, Color.Black);
Draw.TextCentered("GAME OVER", midten, 80, Color.Red);
Draw.TextAbove("Allan", Position, 40, 16, Color.Black);   // over noget i verden
Draw.ToScreen(Position);                                   // verden -> skaerm
```

Du maa ogsaa bruge raylib direkte. Samme regel: 3D-funktionerne i `Render`,
2D-funktionerne i `RenderUI`.

---

## Kameraet

```csharp
game.Camera.Height = 720;              // hvor mange world units der er plads til i hojden
game.Camera.Target = new(500, 0, 0);   // hvad kameraet kigger paa
game.Camera.Tilt = 30;                 // grader - kig lidt oppefra
game.Camera.Perspective = true;        // aegte perspektiv
```

Skal kameraet foelge en figur, saa brug komponenten `CameraFollow`.

---

## Billeder, modeller og lyde

Laeg dem i `Assets/` og hent dem med navn:

```csharp
Draw.Sprite("coin.png", Position, 40);
Assets.Play("bing.wav");
```

Dine **egne** filer laegger du i `Assets/mine/`. Den mappe er din alene og kommer aldrig i
git. Kraken kigger der foerst.

Ser du et lyseroedt/sort ternet felt i stedet for dit billede, er filen ikke fundet -
tjek navnet og hvilken mappe den ligger i.

---

## Netvaerk

`dotnet run` starter dig som server. Andre forbinder med `dotnet run -- klient` og skriver
din IP-adresse - den staar oppe i hoejre hjoerne af dit vindue.

Du behoever ikke goere noget saerligt for at spille alene. **Server er ogsaa single player.**

### Sadan virker det

Serveren bestemmer alt. Klienten sender kun sine tastetryk og tegner det den faar at vide.

1. Giv din komponent et `NetworkKind` - saa sender serveren den videre til alle.
2. Fortael klienten hvad den skal lave, naar den hoerer det navn:

```csharp
game.NetworkKinds.Register("moent", () => new Coin());
game.NetworkKinds.Register("spiller", () => new Player());
```

3. Skal der mere end Position med over, saa skriv det ned:

```csharp
public override void WriteState(StateWriter state)
{
    state.Colour(Color);
    state.Number(Radius);
}

public override void ReadState(StateReader state)
{
    Color = state.Colour();
    Radius = state.Number();
}
```

**Klienten behoever ikke have den samme kode som serveren.** Kender den ikke et navn,
tegner den en graa kasse med navnet paa - saa kan man stadig spille med. Det er derfor du
og din sidemand godt kan have hver jeres version af spillet og alligevel spille sammen.

Vil du give hver spiller sin egen figur, saa brug `NetworkPlayers`. Se
`Network protocol.md` for alle beskederne.

---

## Komponentkataloget

### StaticText
Tekst der bliver liggende paa skaermen.
```csharp
game.Add(new StaticText { Text = "Mit spil", ScreenPosition = new(20, 20), FontSize = 40 });
game.Add(new StaticText { Text = "MIDT", ScreenPosition = new(640, 360), Centered = true });
```

### FloatingText
Tekst der flyver rundt og hopper paa kanterne.
```csharp
game.Add(new FloatingText { Text = "Saa er vi i gang!", AngleDegrees = 30, Speed = 200 });
```

### Score
Viser et tal fra `context.State`.
```csharp
game.Add(new Score());
game.Add(new Score { Key = "liv", Label = "Liv", ScreenPosition = new(20, 80) });
```

### HighScore
Husker det hojeste pointtal - ogsaa efter at spillet er lukket. Ligger i en tekstfil.
```csharp
game.Add(new HighScore());
game.Add(new HighScore { FileName = "banerekord.txt", Label = "Banerekord" });
```

### TagCounter
Taeller hvor mange ting der har et bestemt maerkat. *(Erstatter foraarets `CoinCounter`
og `CircleShooterBallCounter`.)*
```csharp
game.Add(new TagCounter { Tag = "moent", Label = "Monter tilbage" });
game.Add(new TagCounter { Tag = "fjende", Label = "Fjender", ScreenPosition = new(20, 120) });
```

### Player
En spiller. Mellemrum skubber, venstre shift dasher.
```csharp
game.Add(new Player { Speed = 300, Color = Color.Blue, Name = "Mig" });
game.Add(new Player { Sprite = "helt.png", Size = 60, MaxLives = 5, PushRadius = 0 });
```
Samler alt op der er `ICollectable`, tager skade af alt der er `IHarmful`, og skubber alt
der er `IPushable`.

### ArrowKeyControlledBall
Den simplest mulige figur. God at kigge paa naar man vil se hvor lidt der skal til.
```csharp
game.Add(new ArrowKeyControlledBall { Speed = 300, Color = Color.Lime });
```

### Coin
En moent der kan samles op.
```csharp
game.Add(new Coin { Position = new(120, 80, 0), Value = 10 });
game.Add(new Coin { Position = new(-200, 0, 0), Radius = 25, Sprite = "coin.png" });
```

### FallingCoin
En moent der falder ned og forsvinder. God til konfetti.
```csharp
game.Add(new FallingCoin { Position = new(0, 400, 0), Speed = 250, Collectable = true });
```

### CircleShooter
En kanon der drejer og skyder. Kuglerne goer skade og kan skubbes vaek.
```csharp
game.Add(new CircleShooter { Position = new(0, 0, 0), AutoShootEverySeconds = 1f });
game.Add(new CircleShooter { ShootKey = KeyboardKey.Space, MaxBounces = 3, BulletSpeed = 500 });
```

### PowerBuffExtraHealth
Et hjerte der giver et ekstra liv.
```csharp
game.Add(new PowerBuffExtraHealth { Position = new(200, -100, 0), Lives = 2 });
```

### PowerBuffCoinMagnet
En magnet der traekker monter til sig i et stykke tid.
```csharp
game.Add(new PowerBuffCoinMagnet { Position = new(-150, 100, 0), Duration = 8f });
```

### StartScreen
Startskaerm der blokerer indtil man trykker Enter. Sender `GameStarted` ud.
```csharp
game.Add(new StartScreen { Title = "MOENTJAGT" });
```

### GameOverScreen
Kommer frem af sig selv naar nogen sender `GameOver` ud.
```csharp
game.Add(new GameOverScreen());
game.Add(new GameOverScreen { Message = "AV!", RestartsGame = false });
```

### WinCondition
Erklaerer spillet vundet naar pointtallet er hojt nok, og lader det regne med monter.
```csharp
game.Add(new WinCondition { ScoreToWin = 100 });
```

### CameraFollow
Lader kameraet foelge din figur, saa banen kan vaere storre end skaermen.
```csharp
game.Add(new CameraFollow());
game.Add(new CameraFollow { Tag = "bil", Smoothing = 2f, FollowY = false });
```

### NetworkPlayers
Giver hver spiller der forbinder sig deres egen figur. Koerer kun paa serveren.
```csharp
game.Add(new NetworkPlayers());
game.Add(new NetworkPlayers { CreatePlayer = () => new Player { Speed = 400, MaxLives = 5 } });
```

---

## Hvad er dit, og hvad er faelles

| Mappe | Hvem ejer den |
|---|---|
| `Engine/` | motoren. Rort kun i faellesskab. |
| `Components/` | faelles komponenter. Kommer i git til alle. |
| `MyComponents/` | **dine egne komponenter.** Kommer aldrig i git. |
| `Assets/mine/` | **dine egne billeder og lyde.** Kommer aldrig i git. |
| `program.cs` | **dit spil.** Kommer aldrig i git. |

Vil du se hvordan noget saa ud i foraaret, ligger hele det gamle projekt stadig i
`forår 2026/network-game/`. Men lad vaere med at rette i det - den saeson er slut.

Fordi dine filer ikke ligger i git, kan du aldrig komme til at oedelaegge noget for de
andre - og de kan aldrig komme til at overskrive dit. Naar du henter ny kode ned, bliver
dine egne filer liggende praecis som de var.

Har du lavet noget de andre skal have? Sig til, saa flytter vi det over i `Components/`.

Se `MyComponents/README.md` for hvordan du kommer i gang med din foerste egen komponent.

---

## Kom du fra foraaret?

| Foraar 2026 | Kraken |
|---|---|
| `y` peger nedad | `y` peger **opad** |
| `(0,0)` oeverst til venstre | `(0,0,0)` midt paa skaermen |
| `IComponent` | `Component` (en klasse - arv fra den) |
| `new Player(pos, 200, 20, Color.Blue, ...)` | `new Player { Speed = 200, Color = Color.Blue }` |
| `UpdateContext` | `GameContext` |
| `context.AddComponent(x)` | `context.Add(x)` |
| `context.GetComponents<T>()` | `context.Find<T>()` |
| `Render()` tegner alt | `Render()` = verden, `RenderUI()` = skaermen |
| afstandstjek i hver komponent | `Collider` + `OnCollision` |
| `event EventHandler<Coin>` | `context.On<T>()` / `context.Publish(...)` |
| `score.Points += 10` | `context.State.Add("score", 10)` |
| `CoinCounter`, `CircleShooterBallCounter` | `TagCounter` |
| `ManualScoreCounter` | `Score` med sin egen `Key` |
| `NetworkBallManager` | `NetworkPlayers` |
| klienten flyttede sig selv | serveren bestemmer, klienten foelger med |
