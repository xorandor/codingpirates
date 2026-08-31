# Netvaerksprotokollen

## Reglerne

Alt der gaar over netvaerket er ren tekst, saa man kan laese med:

- **Kodning:** UTF-8
- **Een besked = een linje**, afsluttet med `\n`
- **Felter adskilles med semikolon** `;`
- **Foerste felt er kommandoen**, altid med STORE BOGSTAVER
- **Ukendte kommandoer skal ignoreres** - modtageren maa aldrig gaa ned af en besked den ikke forstaar
- Semikolon og linjeskift inde i et felt bliver lavet om til `_` inden afsendelse

Vil du se beskederne mens du spiller, saa saet `game.Networking.LogMessages = true`.

## Modellen: serveren bestemmer

```
   KLIENT                                     SERVER
     |                                          |
     |------------- JOINED;Anna --------------->|   "jeg vil gerne vaere med"
     |<------------ SPAWN / STATE --------------|   alt der findes lige nu
     |<------------ YOUARE;7 -------------------|   "nummer 7 er dig"
     |                                          |
     |------------- INPUT;101000 -------------->|   "jeg holder op og venstre nede"
     |                                          |   serveren flytter figuren
     |<------------ STATE;7;120;40;0;... -------|   "her staar nummer 7 nu"
```

Klienten regner **ingenting** ud selv. Den sender sine tastetryk og tegner det den faar
at vide. Derfor behoever klienten ikke have den samme kode som serveren - den skal bare
vide hvad den skal tegne for et givet `kind`. Kender den det ikke, tegner motoren en graa
kasse med navnet paa.

## Beskeder klient → server

| Kommando | Format | Betydning |
|---|---|---|
| `JOINED` | `JOINED;<spillernavn>` | Jeg vil vaere med. Sendes een gang lige efter forbindelsen. |
| `INPUT` | `INPUT;<6 tegn>` | Hvilke knapper der holdes nede: op, ned, venstre, hoejre, A, B - `1` for nede, `0` for oppe. `INPUT;100010` = op + A. Sendes naar noget aendrer sig, og mindst hvert halve sekund. |
| `MESSAGE` | `MESSAGE;<tekst>` | Fri tekst. Bruges ikke af motoren - den er til dine egne paafund. |

## Beskeder server → klient

| Kommando | Format | Betydning |
|---|---|---|
| `SPAWN` | `SPAWN;<id>;<kind>` | Der er kommet en ny ting i spillet. `kind` er komponentens `NetworkKind`, fx `moent`. |
| `STATE` | `STATE;<id>;<x>;<y>;<z>[;<ekstra>...]` | Sådan ser tingen ud nu. x/y/z er Position. Felterne efter er det komponenten selv skrev i `WriteState`. Sendes 20 gange i sekundet. |
| `DESPAWN` | `DESPAWN;<id>` | Tingen findes ikke laengere - fjern den. |
| `YOUARE` | `YOUARE;<id>` | Den ting er din figur. Klienten kan finde den igen som `context.MyEntity`. |
| `GAMESTATE` | `GAMESTATE;<noegle>;<type>;<vaerdi>` | Et tal, ja/nej eller en tekst fra `context.State` har aendret sig. `type` er `n` (tal), `b` (ja/nej), `s` (tekst) eller `x` (slettet). |

## Beskeder kun inde i serveren

| Kommando | Format | Betydning |
|---|---|---|
| `DISCONNECTED` | `DISCONNECTED;<spillernavn>` | En klient faldt af. Laegges i koen af `Networking` og bliver aldrig sendt ud. Motoren laver den om til beskeden `PlayerLeft`. |

## Tal i beskeder

Kommatal skrives altid med **punktum**, aldrig komma - ellers ville en dansk og en engelsk
maskine laese `1,5` forskelligt. Det klarer `StateWriter` og `StateReader` selv.

## Vil du lave dine egne beskeder?

Det maa du gerne. Skriv dem ind i tabellerne herover, saa alle ved hvad de betyder, og brug:

```csharp
// afsender
context.Networking.SendMessageToServer("HOP", spillerNavn);
context.Networking.BroadcastMessageToClients("EKSPLOSION", x.ToString(), y.ToString());

// modtager
foreach (var besked in context.Networking.ConsumeAll("HOP"))
    Console.WriteLine($"{besked.Sender} hoppede");
```

Men proev foerst uden. Det meste kan klares ved bare at give din komponent et
`NetworkKind` og lade motoren om resten.
