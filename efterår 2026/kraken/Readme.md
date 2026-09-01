# Kraken

Spilmotoren vi bygger videre paa i efteraaret 2026.

## Kom i gang

1. Hent koden.
2. Kopier en skabelons `program.cs` op i roden - fx fra `GameTemplates/Pong/` eller
   `GameTemplates/Hoppebolde/` (se "Hvad er dit, og hvad er faelles" laengere nede).
3. Skriv `dotnet run`.

Det er det. `program.cs` er din fil - den kommer aldrig i git, saa du kan lave lige praecis
det spil du har lyst til.

```
dotnet run     # og er spillet et netvaerksspil, sporger det selv om du vil vaere vaert
```

| Tast | Gor |
|---|---|
| **F3** | 3D-visning til og fra. Proev den! |
| **P** | Viser og skjuler listen over hvem der er med |
| Piletaster / WASD | Bevaeg dig |
| Mellemrum | Knap **A** (fx skub) |
| Venstre shift | Knap **B** (fx dash) |
| Enter | Knap **Enter** (fx skyd noget nyt ud) |

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

## De tre maader komponenter snakker sammen paa

Det her er hjertet i Kraken. En komponent maa **aldrig** kende en anden komponents type.
Skriver du `if (other is Bold)`, saa kan din komponent kun bruges i spil der har lige
praecis den klasse - og dine egne komponenter i `MyGames/` kan ikke vaere med.

### 1. Kollision

Giv din komponent en `Collider`, saa fortaeller motoren dig naar noget roerer dig.

```csharp
public override void OnAdded(GameContext context) => Collider = Collider.Circle(20);
public override void OnAdded(GameContext context) => Collider = Collider.Box(40, 60);

public override void OnCollision(Component other, GameContext context)
{
    if (other.HasTag("bold"))
        Console.WriteLine("Jeg blev ramt af en bold!");
}
```

Kollision regnes ud i xy-planet - z bliver ignoreret. Vil du vide *hvad* du ramte, saa
spoerg paa maerkatet (`HasTag`) - aldrig paa typen.

### 2. Beskeder

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

### 3. Faelles hukommelse og maerkater

```csharp
context.State.Add("score", 10);            // laeg 10 point til
int point = context.State.Number("score"); // laes dem igen
context.State.SetFlag("boss-doed", true);

foreach (var fjende in context.FindByTag("fjende"))
    context.Remove(fjende);
```

En komponent der viser point, laeser bare tallet under `"score"`. Derfor kan enhver
komponent give point uden foerst at lede efter den der viser dem.

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
Draw.Ball(Position, 20, Color.Red);                     // kugle - en flad cirkel, indtil der er lys
Draw.Cube(Position, new Vector3(40, 40, 40), Color.Blue);
Draw.Rectangle(Position, new Vector2(100, 20), Color.Green);
Draw.Line(startPunkt, slutPunkt, Color.Black);
Draw.Circle(Position, 40, Color.Yellow);                // en ring - god til effekter
Draw.Sprite("helt.png", Position, 60);                  // et billede (faar aldrig lys)
Draw.Model("skib.glb", Position, scale: 2f);            // en 3D-model
Draw.Model("skib.glb", Position, Vector3.UnitY, vinkel, 2f);   // drejet om en akse
Draw.Model("bold", Position, drejning, 13f);            // drejning er en Quaternion, se Lys-afsnittet
Draw.Shaded(minShader, () => Draw.Cube(Position, size, color));  // med din egen shader
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
game.Camera.Perspective = true;        // aegte perspektiv - ting langt vaek bliver smaa
```

Slaar du `Perspective` til, stiller kameraet sig af sig selv saa alt i z = 0 er praecis lige saa
stort som foer. Ting med z under 0 bliver mindre (en stjernehimmel), ting med z over 0 stoerre.
Vil du selv bestemme afstanden, saa saet `game.Camera.Distance`.

Skal kameraet foelge en figur, saa flyt `context.Camera.Target` med i figurens `Update`.

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

Nogle ting kan koden lave selv, uden en fil. De faar et navn og bruges som alt andet:

```csharp
Assets.Checkered("skak", Color.White, Color.Black);      // et ternet billede
Assets.Ball("bold", "skak");                              // en kugle med billedet paa
Draw.Model("bold", Position, drejning, 13f);              // tegn den - 13 er radius
Assets.Shader("shaders/lys.vs", "shaders/glimt.fs");      // en shader fra to filer

Assets.Tone("plink", 880, 1567, 0.09f);                  // en tone der glider op og doer ud
Assets.Tone("brum", 220, 110, 0.2f, firkant: false);     // nedad og bloedere
Assets.Noise("bang", 0.3f);                               // et sus - eksplosion, maal
Assets.Play("plink");
Assets.Play("plink", 0.5f, 2f);                           // halv styrke, en oktav op
```

Lav dem i `OnAdded`, ikke i en feltinitialisering - vinduet skal vaere aabent foerst.
En lyd der mangler paa disken crasher ikke - den bliver til stilhed og en besked i konsollen.

---

## Lys

Uden lys tegnes alt fladt: en kugle er en cirkel, en terning er en firkant. Saet **een**
`Light` i spillet, og kugler, terninger og modeller faar en lys side, en skyggeside og et
lille blankt glimt. Billeder (`Draw.Sprite`), linjer og ringe roeres ikke - de er flade.

```csharp
game.Add(new Light());                                            // en paere oppe til venstre
game.Add(new Light { Position = new(400, 300, 600), Color = Color.Orange, Intensity = 0.7f });
game.Add(new Light { Directional = true, Position = new(1, 1, 1) });   // som solen

game.Lighting.Ambient = new Color(40, 40, 60, 255);   // grundlyset - moerkere = dybere skygger
game.Lighting.Shininess = 64;                          // hvor skarpt glimtet er. 0 slaar det fra
```

Op til fire lys ad gangen. Et lys er en almindelig komponent, saa det kan flytte sig: saet
`Position` i `Update`, og lyset foelger med.

**Din egen shader.** Vil du have noget til at glimte, pulsere eller skifte farve, saa kopier
`Assets/shaders/glimt.fs`, ret i de sidste linjer, og tegn med `Draw.Shaded`. Lyset saettes
paa din shader af sig selv, saa laenge du beholder de uniforms der staar oeverst i filen.

```csharp
var shader = Assets.Shader("shaders/lys.vs", "shaders/min.fs");
Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "time"), (float)Raylib.GetTime(), ShaderUniformDataType.Float);
Draw.Shaded(shader, () => Draw.Cube(Position, size, color));
```

**En kugle der ruller.** Byg drejningen op over tid med en `Quaternion`, og giv den til
`Draw.Model`. Rulleaksen staar vinkelret paa retningen:

```csharp
var akse = Vector3.Normalize(new Vector3(-retning.Y, retning.X, 0));
_drejning = Quaternion.Concatenate(_drejning, Quaternion.CreateFromAxisAngle(akse, fart * dt / radius));
Draw.Model("bold", Position, _drejning, radius);
```

---

## Netvaerk

Et spil bliver et netvaerksspil med een linje:

```csharp
var game = new GameEngine { Title = "Mit spil", Network = true };
```

Naar spillet starter, viser det alle de spil der er i gang paa netvaerket - og oeverst i
listen staar "Start dit eget spil". Ingen skal taste adresser eller noget som helst.

**Vil du vaere med i et spil?**
1. Find spillet i listen (det staar med navn og hvem der er vaert) og tryk Enter.
2. Har spillet en kode, saa tast den - vaerten kan se koden i sit eget hjoerne.
3. Skriv dit navn. Faerdig.

**Vil du vaere vaert?** Vaelg "Start dit eget spil" oeverst. Saa giver du spillet et navn,
en kode hvis du vil have en, og dit eget navn. Vaerten spiller selv med - vaertens navn
staar oeverst paa spillerlisten.

Uden `Network = true` er spillet helt lokalt: ingen skaerme, intet netvaerk.

Vil du laase dit spil fast med en kode fra koden af, saa saet `game.Password = "blaa-banan"`
- ellers sporger vaert-skaermen selv.

**Kan de andre ikke se dit spil i listen?** Foerste gang du er vaert, sporger Windows
Firewall om lov - sig ja til baade private og offentlige netvaerk. Og alle skal vaere paa
det samme netvaerk.

Du behoever ikke goere noget saerligt for at spille alene. **Vaerten er ogsaa single player.**

### Sadan virker det

Serveren bestemmer alt. Klienten sender kun sine tastetryk og tegner det den faar at vide.

1. Giv din komponent et `NetworkKind` - saa sender serveren den videre til alle.
2. Fortael klienten hvad den skal lave, naar den hoerer det navn:

```csharp
game.NetworkKinds.Register("markoer", () => new Markoer());
game.NetworkKinds.Register("hoppebold", () => new HoppeBold());
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

Vil du give hver spiller sin egen figur, saa lyt paa `PlayerJoined`/`PlayerLeft` -
`MarkoerPerSpiller` i `GameTemplates/Hoppebolde/` viser praecis hvordan. Se
`Network protocol.md` for alle beskederne.

---

## Komponentkataloget

### StaticText
Tekst der bliver liggende paa skaermen.
```csharp
game.Add(new StaticText { Text = "Mit spil", ScreenPosition = new(20, 20), FontSize = 40 });
game.Add(new StaticText { Text = "MIDT", ScreenPosition = new(640, 360), Centered = true });
```

### StartScreen
Startskaerm der blokerer indtil man trykker Enter. Sender `GameStarted` ud.
```csharp
game.Add(new StartScreen { Title = "PONG", Subtitle = "Tryk Enter for at starte" });
```

### Light
En lyskilde. Saa snart der er een, faar kugler, terninger og modeller lys og skygge. Se afsnittet Lys.
```csharp
game.Add(new Light());
game.Add(new Light { Position = new(400, 300, 600), Color = Color.Orange, Intensity = 0.7f });
```

### SoundEffects
Lyd paa spillets beskeder: samle op, tage skade, faa liv, doe, vinde, tabe, komme med.
Lydene er lavet af koden - ingen filer. Dine egne komponenter faar lyd gratis, hvis de
publisher de samme beskeder.
```csharp
game.Add(new SoundEffects());
game.Add(new SoundEffects { Collected = "min-plink.wav", Damaged = "" });   // egen fil / slaaet fra
```

Kataloget er med vilje lille - det er skabelonerne i `GameTemplates/` der viser hele spil,
og resten bygger du selv i `MyGames/`.

---

## Hvad er dit, og hvad er faelles

| Mappe | Hvem ejer den |
|---|---|
| `Engine/` | motoren. Rort kun i faellesskab. |
| `Components/` | faelles komponenter. Kommer i git til alle. |
| `GameTemplates/` | hele spil som skabeloner. Kopieres ud - rettes ALDRIG direkte. |
| `MyGames/` | **dine egne komponenter.** Kommer aldrig i git. |
| `Assets/mine/` | **dine egne billeder og lyde.** Kommer aldrig i git. |
| `program.cs` | **dit spil.** Kommer aldrig i git. |

Vil du have et helt spil at starte fra, saa kig i `GameTemplates/` - fx `GameTemplates/Pong/`.
Saadan kopierer du en skabelon ud (staar ogsaa i skabelonens egen README):

1. Kopier HELE skabelon-mappen ind i `MyGames/` - i **Stifinder** eller terminalen, ALDRIG
   inde fra Visual Studio. (VS aendrer projektfilen naar den kopierer, og saa kompilerer dit
   spil bare ikke, uden fejl.)
2. Kopier saa dens `program.cs` op i roden af `kraken/`, oven i din egen. Kopien der bliver
   tilbage i `MyGames/` goer ingen skade - motoren ignorerer den.
3. `dotnet run`. Nu er hele spillet DIT - aendr alt.

Ret aldrig i selve skabelonen. Hver skabelons README har ogsaa ideer til hvad du kan aendre.

Vil du se hvordan noget saa ud i foraaret, ligger hele det gamle projekt stadig i
`forår 2026/network-game/`. Men lad vaere med at rette i det - den saeson er slut.

Fordi dine filer ikke ligger i git, kan du aldrig komme til at oedelaegge noget for de
andre - og de kan aldrig komme til at overskrive dit. Naar du henter ny kode ned, bliver
dine egne filer liggende praecis som de var.

Har du lavet noget de andre skal have? Sig til, saa flytter vi det over i `Components/`.

Se `MyGames/README.md` for hvordan du kommer i gang med din foerste egen komponent.

---

## Kom du fra foraaret?

| Foraar 2026 | Kraken |
|---|---|
| `y` peger nedad | `y` peger **opad** |
| `(0,0)` oeverst til venstre | `(0,0,0)` midt paa skaermen |
| `IComponent` | `Component` (en klasse - arv fra den) |
| `new Player(pos, 200, 20, Color.Blue, ...)` | egenskaber, ikke konstruktorer: `new Markoer { Fart = 200 }` |
| `UpdateContext` | `GameContext` |
| `context.AddComponent(x)` | `context.Add(x)` |
| `context.GetComponents<T>()` | `context.Find<T>()` |
| `Render()` tegner alt | `Render()` = verden, `RenderUI()` = skaermen |
| afstandstjek i hver komponent | `Collider` + `OnCollision` |
| `event EventHandler<Coin>` | `context.On<T>()` / `context.Publish(...)` |
| `score.Points += 10` | `context.State.Add("score", 10)` |
| `CoinCounter`, `ManualScoreCounter` | laes tallet i `context.State` og tegn det selv |
| `NetworkBallManager` | lyt paa `PlayerJoined`/`PlayerLeft` (se Hoppebolde) |
| klienten flyttede sig selv | serveren bestemmer, klienten foelger med |
