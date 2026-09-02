# Kraken

## Who is at the keyboard — ask before editing anything

Two kinds of people work in this repo, with different write permissions for Claude:

- **Underviser** (teacher): Claude may edit everything, including the source-controlled parts
  (`Engine/`, `Components/`, `GameTemplates/`, `Kraken.csproj`, the docs, `CLAUDE.md` itself).
- **Pirat** (a student, 10-15 years old): Claude may **only** edit `MyGames/`, the root
  `program.cs`, and `Assets/mine/` — the gitignored, personal part of the repo. Never touch
  `Engine/`, `Components/`, `GameTemplates/`, `Kraken.csproj`, `.gitignore` or the tracked docs
  for a pirat, not even on request. If a pirat's wish genuinely requires an engine change, do not
  make it — explain (in Danish, kindly) that this needs an underviser, and help them build the
  best possible version inside `MyGames/`/`program.cs` instead.

**Before the first file edit in a session, know the role.** Check memory first — if the user's
role is already stored there, use it without asking. Otherwise ask the user directly: "Er du
underviser eller pirat?" — and store the answer in memory (if memory is available on the machine;
otherwise just ask again next session). A pirat also gets answers, explanations and code written
*for* them like anyone else — the restriction is only about which files Claude edits.

## Project overview

Kraken is the game engine used to teach a group of 10-15 year olds to code, one session a week
across a season. It is the successor to `forår 2026/network-game`, rebuilt around 3D rendering,
object-initializer configuration, an engine-level interaction model, and server-authoritative
networking.

The engine exists to be *taught*, not to be complete. Every design decision is judged by whether a
12-year-old can read it, copy it, and change it without breaking anyone else's work.

## Tech stack

- C# on .NET 10 (`net10.0`). Requires the .NET 10 SDK/runtime — a machine with only .NET 9 can neither
  build nor run this. Retargeted from `net9.0` on 2026-08-29 when the second work machine turned out
  to have SDK 10.0.400 only.
- Raylib-cs 7.0.2 for everything graphical and audio. `AllowUnsafeBlocks` is on because raylib
  exposes a model's materials as a raw pointer (`model.Materials[0]`); the `unsafe` blocks live in
  `Engine/` only.
- .NET BCL for networking (raw TCP, line-based text protocol).

## Coordinate system — read this first

Kraken always renders through a `Camera3D`. By default it is **orthographic**, looking straight into
the xy-plane, so a 2D game just works while still getting meshes, textures and depth.

- **x** goes right.
- **y** goes **UP**. This is the opposite of raylib's 2D screen coordinates and the opposite of
  `forår 2026`. Pil op = +y.
- **z** is the layer. Higher z is closer to the camera and draws on top; the render pass sorts by z
  ascending so alpha-blended sprites composite correctly.
- `(0, 0, 0)` is the centre of the screen, not the top-left corner.
- World units default to 1 unit = 1 pixel (`Camera.Height` defaults to the window height).

`GameCamera.Tilt` / `Turn` / `Perspective` exist so a 2D scene can be shown as the 3D scene it
actually is. F3 toggles a debug view that does exactly that.

- `Perspective = true` keeps the framing: `Distance` defaults to 0 = automatic, which places the
  camera so z = 0 is exactly as large as in orthographic (`Height / (2·tan(fov/2))`, 869 units at
  720/45°). Set `Distance` yourself only to change that on purpose.
- raylib's default far clip plane is 1000 units. With the camera 869 units out that clips anything
  deeper than z ≈ −130, so `Run()` raises it to 20000 (`Rlgl.SetClipPlanes`).
- F3 in perspective swings the camera physically and pushes the field edges out of frame. It is a
  debug view; live with it.

## Rendering and light

Everything is **unlit until a `Light` exists** — raylib's default shader is flat, so a sphere is a
disc and rotation is invisible. `Engine/Lighting.cs` + `Assets/shaders/lys.{vs,fs}` add up to four
lights, ambient and a specular glint. Opt-in by design: a game without `Light` renders exactly as
before. Things to know before touching it:

- raylib has **two draw paths**. Batch primitives (`DrawCubeV`, lines, billboards) go through
  `BeginShaderMode`; models (`DrawModel`) use the shader in their material and ignore it. So
  `Lighting.Begin/End` wrap the 3D pass, and every `Draw.Model`/`Draw.Ball` calls `Prepare(ref
  model)` to swap the material shader in (or back out when lights are removed).
- `DrawModel` writes `matModel`/`matNormal` and leaves them; batch geometry is already in world
  space and needs identity. `AfterModel()` resets them — forget it and the next cube is lit as if
  it sat where the last model was.
- `Draw.Ball` is a cached unit-sphere **model** (`Assets.UnitSphere`), not `DrawSphere`, so it has
  normals and shades correctly. Do not change it back.
- Sprites, lines and `Draw.Circle` are deliberately unlit (`Lighting.Unlit`). Billboards have no
  useful normals.
- `Draw.Shaded(shader, draw)` runs a custom shader and first copies the light uniforms onto it
  (`Lighting.Apply`) if it uses the same names as `lys.fs`. `glimt.fs` is the template to copy.
  With no lights `Apply` sets ambient to white so the custom shader looks unlit, not black.
- `Assets.Checkered` / `Assets.Ball` generate a texture / textured sphere from code under a name,
  so a rolling ball needs no files. `Draw.Model` has axis+angle and `Quaternion` overloads; the
  quaternion one sets `model.Transform`.

`GameEngine.DeltaTime` is clamped to `MaxDeltaTime` (0.1 s) and `context.DeltaTime` reads that,
not `GetFrameTime()`. Frame one includes window creation and connection (measured 0.2–1.4 s);
without the clamp anything moving jumps that far.

## Sound

`Assets.Tone` / `Assets.Noise` synthesize 16-bit mono PCM from code and cache it under a name, so
sounds need no files — same pattern as `Assets.Checkered`. A missing sound file becomes silence
plus a console line, never a crash (same philosophy as the magenta texture). `Assets.Play(name,
volume, pitch)` sets volume/pitch on the shared `Sound`, so they stick until the next Play sets
them — the 1-arg overload resets to 1/1.

- The `SoundEffects` component is the only thing that plays sounds by itself, and it listens only
  to the event bus (`Collected`, `Damaged`, `Healed`, `Died`, `GameOver`, `GameWon`,
  `PlayerJoined`). Built-in sound names start with `*`; a property set to `""` mutes that event.
  Custom components that publish those events get sound for free.
- **Network gap:** events fire where the logic runs, so in a networked game only the server hears
  sounds. Client-side sound needs events replicated or local triggers; not built.
- Raylib-cs 7.0.2 names raylib's `Wave.frameCount` **`SampleCount`** — the compile error is
  unhelpful if you guess `FrameCount`. Sound frame counts read back at the device rate (48 kHz),
  not the 44.1 kHz the samples were made at.

## Project structure

The project must stay simple enough that a student can copy a single file into their own checkout
without pulling from git. Five concepts:

- **`Engine/`** — the motor. Everything else may reference it; it references nothing else.
- **`Components/`** — shared components, one class per file. **A component must never reference
  another component's concrete type.** Interact through the interaction model below instead.
  Only the composition may reference this folder. **Only generic scaffolding belongs here**
  (screens, text, light, sound — the catalog is deliberately small: `StaticText`, `StartScreen`,
  `Light`, `SoundEffects`) — a complete game never lives here; complete games are game templates,
  and game-specific components live inside their template folder. (The MOENTJAGT-era reference
  specimens — `Player`, `Coin`, power-ups, HUD counters etc. — were deleted with MOENTJAGT on
  2026-09-01: unused code is deleted, and the templates are the reference now.)
- **`GameTemplates/`** — whole games as template folders, tracked in git. Copied out by students,
  never edited in place. See the Game templates section below.
- **`MyGames/`** — a student's private components and copied-in game templates, namespace `Mine`.
  Gitignored wholesale. Never commit anything from here, and never assume another machine has
  these files. (Renamed from `MyComponents/` on 2026-08-31 — the rename must carry the
  `.gitignore` entry, the tracked README/template inside it, and every mention in `Readme.md`.)
- **`program.cs`** — the composition. Gitignored. The only place allowed to reference everything.
  There is no root `program.cs.template` any more (it was the MOENTJAGT game, removed 2026-09-01):
  the committed starting points are the templates' own `program.cs` files under `GameTemplates/`.

The spring originals are **not** copied into this folder. If you need to see how something used to
work, read `forår 2026/network-game/` — it is still in the repo, one directory up. Do not modify
anything under `forår 2026/`; that season is finished and stays as it was.

## Game templates

`GameTemplates/<Name>/` holds a whole game as a folder the student copies out. The one rule,
phrased for a 12-year-old: **templates are copied out — never edited in place.** That keeps the
no-conflicts guarantee: students still never modify tracked files.

Each template folder contains:

- **`program.cs`** — the game's composition.
- **Component files**, one class per file, written in **`namespace Mine`** so they compile
  unchanged after being copied into `MyGames/`.
- **`README.md`** (Danish) — three lines on what the game is, the copy instructions, plus a
  handful of "proev at aendre..." ideas in increasing wildness. Students who stall steal an idea
  from it, and Claude reads it when helping in that folder.

Copy flow, done in **Windows Explorer (or the terminal, or by Claude) — never in Visual Studio's
Solution Explorer**: copy the whole template folder into `MyGames/` (giving e.g.
`MyGames/Pong/`), then copy its `program.cs` over the root `program.cs`. After that the whole
game is the student's own, gitignored, free to wreck. The copy itself is a fine first Claude
prompt of the evening ("kopier Pong-skabelonen ind som mit spil").

Why never Solution Explorer: a VS copy-paste of excluded files carries their item metadata along
by **writing `<None Include>`/`<Compile Remove>` entries for the copies into `Kraken.csproj`** —
the student's copy silently does not compile, and a tracked file has been modified, which is
exactly the conflict the whole design exists to prevent. Explorer/terminal copies are inert:
anything landing in `MyGames/` compiles via the default globs. To keep the hazard untriggerable,
`GameTemplates/` is **removed from the project entirely** (`Compile Remove` + `None Remove`), so
it does not appear in Solution Explorer at all — unless "Show All Files" is toggled on, which
shows everything on disk regardless. Students read a template in Explorer, or after copying it
out, where it sits in `MyGames/` as ordinary compiled files.

Build rules in `Kraken.csproj`:

- **`<Compile Remove="GameTemplates/**" />`** — the folders contain real `.cs` files including
  entry points, so without it the build breaks with duplicate `Main`s. Real `.cs` files (not
  `.template` renames) are deliberate: syntax highlighting for students, and no mass-renaming
  on copy.
- **`<Compile Remove="MyGames/**/program.cs" />`** — the whole-folder copy brings the template's
  `program.cs` along into `MyGames/<Name>/`, and without this rule that stray copy is a second
  entry point and the build breaks the moment a student follows the instructions. The root
  `program.cs` is the only composition that compiles.

Gitignore rule: the ignore pattern for the root composition must stay **root-anchored** —
`/program.cs`, not `program.cs`. An unanchored pattern matches at every level and silently keeps
each template's `program.cs` out of git; that exact mistake shipped Pong without its composition
once. `git status GameTemplates/` after adding a template is the check.

A template may be shipped **complete** (evening one: copy Pong, then mutate it with Claude) or
**with a deliberate hole** — the scaffolding components are provided, the game's core component
(the ball, the tower, the car) is missing, and building it is the evening's challenge. The
teacher's reference version of a missing component stays out of git.

Didactic intent, so future changes preserve it: `Components/` grows with generic scaffolding,
templates carry whole games, and the season ramps from "copy a complete template" via "template
with a hole" to "own game from scratch". Complete games must therefore never migrate into
`Components/`.

## Conventions

- **Configuration is object initializers, never constructor parameters.** Every component has an
  implicit parameterless constructor and public get/set properties with sensible defaults:
  `game.Add(new Markoer { Fart = 380, Farve = Color.SkyBlue })`. Do not add constructors to
  components. Anything computed from configured values goes in `OnAdded` — including
  `Collider ??= Collider.Circle(Radius)` and `Tags.Add(...)`, which is the house pattern.
- **Positions are `Vector3`.** Directions are unit vectors; speed and distance are separate floats.
- **`Render()` draws in the 3D world, `RenderUI()` draws in screen space.** Never call `DrawText`,
  `DrawRectangle` or any other 2D raylib function from `Render()`, and never call `DrawSphere`,
  `DrawCube` or `DrawModel` from `RenderUI()`. Use `Draw.ToScreen` / `Draw.TextAbove` to label a
  world object from `RenderUI()`. Neither takes a `GameContext` — cache it in `OnAdded` if needed.
- **Purely visual animation belongs in `Render()`, driven by `GetTime()`, not in `Update()`.**
  Writing an animation into `Position` fights anything else that moves the component (the coin
  magnet), and `Update` does not run for server-controlled components on a client.
- **Assets load through `Assets`, never `LoadTexture` directly.** A missing file becomes a magenta
  checkerboard instead of a crash. Assets may only be loaded once the window is open, i.e. from
  `OnAdded` or later, never from a field initializer.
- **Comments and student-facing text are in Danish**, without Danish letters in identifiers (write
  `oe`, `ae`, `aa`). Engine XML doc comments are Danish too — they are what students read in
  IntelliSense.
- `GameMode.Server` is also single player. A game must be playable with no clients connected.

## Interaction model

Components never name each other's types. Five mechanisms, in the order to reach for them:

1. **Collision** — `Collider = Collider.Circle(r)` / `Collider.Box(w, h)`, engine does the
   broadphase and calls `OnCollision(other, context)`. Computed in the xy-plane; z is ignored.
2. **Tags** — `Tags.Add("bold")`, `other.HasTag("bold")`, `context.FindByTag`. The way to ask
   *what* you collided with without naming a type. (The capability interfaces `ICollectable`/
   `IDamageable`/`IHarmful`/`IPushable` were deleted by the user on 2026-09-01 — after the
   MOENTJAGT cleanup nothing consumed them, and they "smelled of one specific game, not general
   engine features". Do not reintroduce them without asking.)
3. **Typed event bus** — `context.On<T>(handler)` (auto-unsubscribes when the component is removed)
   and `context.Publish(...)`. Engine events: `GameStarted`, `GameOver`, `GameWon`, `Collected`,
   `Damaged`, `Healed`, `Died`, `ScoreChanged`, `PlayerJoined`, `PlayerLeft`.
4. **Timers** — `context.After(s, action)`, `context.Every(s, action)`. Owned by the component that
   created them: cancelled on removal, and paused while a blocking component is up unless the owner
   sets `RunsWhileBlocked`.
5. **Shared state** — `context.State.Add("score", 10)` / `Number` / `Flag` / `Text`. A HUD is only
   a display of a state key. Numbers, flags and text replicate to clients; `Set<T>` does not.

No transform hierarchy, deliberately.

## Blocking

`IsBlocking` pauses `Update` and collision for everything else — **not** rendering, so the world
freezes behind a menu instead of vanishing. `RunsWhileBlocked` opts a component (and its timers)
back in.

## Networking

Server-authoritative. Clients send `INPUT` only; the server runs all logic and broadcasts generic
entity snapshots at `NetworkRate` (20 Hz).

- A component opts in with `NetworkKind`. `Position` replicates automatically; extra fields go
  through `WriteState`/`ReadState` in matching order.
- **A client does not need the server's component code.** It looks `kind` up in `game.NetworkKinds`
  and falls back to `UnknownEntity` (grey box with the kind name) when it does not know one.
- On a client, `game.Add` silently drops components that have a `NetworkKind` — the server owns
  them. Server-controlled components have `IsRemote` true: their `Update` and `OnCollision` never
  run locally.
- A client must not `Remove` a server-controlled component; the server will keep sending state for
  an id the client no longer has, and it never comes back.
- Purely decorative networked-looking things (confetti, particles) should return `null` from
  `NetworkKind` so each machine makes its own.

`Network protocol.md` defines the message contracts and must be updated whenever a message is added
or changed. Protocol rules: UTF-8, one message per line terminated by `\n`, `;` separator, ALL CAPS
command first, receivers ignore unknown commands without crashing. Floats always use `.` via
`CultureInfo.InvariantCulture`.

### Network mode and discovery (2026-08-31)

Networking is **opt-in per game**: `Network = true` on the engine. Without it the engine never
touches the network — no listener, no discovery, no corner status, no player box. With it, startup
shows **one start screen**: the list of discovered games with "Start dit eget spil" as the top row
(there is no separate host/join choice screen — user decision 2026-09-01). Picking the top row
makes you host, and an interactive host is then asked for the game's name, an optional password,
and **their own player name** (presets via `Password`/`PlayerName` skip the matching prompt).
`Mode` is a *nullable pre-choice* for tests only; the resolved role lives in the internal
`ActiveMode`, and the client-side "drop components with a NetworkKind" check moved from `Add` to
`FlushPendingChanges` because the role is unknown at composition time.

- **No IP is ever shown or typed.** Clients find games via UDP discovery (port 12346,
  `DISCOVER` broadcast+loopback → unicast `GAME;titel;vaert;tcpPort;antal;laas`), pick from a
  list, and the sender address of the reply is used to connect. `Engine/Discovery.cs` holds both
  the responder and the client collector. The TCP listener binds `IPAddress.Any` — it used to bind
  the LAN address only, which made loopback clients impossible.
- **Password handshake lives in `Networking`**, not game logic: `JOINED;<navn>;<kode>` is answered
  with `WELCOME` (always sent, so the client's 3 s wait is deterministic) or `DENIED;<grund>` +
  close; a denied client is never registered and triggers no events.
- `INPUT` is **7 characters** now — Enter travels as the seventh button (`Input.Enter`).
- The roster replicates through shared state under the reserved key `*spillere` (set in
  `ServerTick`, only on change); the P-toggled player box reads it on clients and
  `PlayerRoster()` on the server. **The roster includes the host** (host's `PlayerName` first,
  then `ConnectedPlayers`) — `GameContext.MyName` is therefore set on hosts too, and
  Hoppebolde's `MarkoerPerSpiller` names the host marker from it.
- **Discovery dedup:** the same server answers both the broadcast and the loopback copy of
  `DISCOVER`, so `DiscoveryClient` keys found games by `vaert:tcpPort`, not sender address —
  and keeps the loopback address when it has one (always reachable on the same machine).
- **Client movement is snapshot-interpolated** (`Bane` in `Engine/Replication.cs`): the client
  renders `2/Rate` (~0.1 s) in the past and lerps between the two buffered snapshots that
  bracket that moment; never extrapolates (missing packets = stand still, no overshoot on
  bounces). A short queue (10 snapshots) is required — with only two stored points the render
  time falls before both and everything clamps back to 20 Hz stepping (measured: 40/119 frames
  moved; with the queue 119/119). Both TCP ends also set `NoDelay` — Nagle otherwise bunches
  the small 20 Hz STATE packets into visible stutter. Rejected alternatives: client-side
  prediction + reconciliation (far too complex for the audience), extrapolation/dead reckoning
  (overshoots on every bounce), higher send rate (more bandwidth, still steps).
- Test bypasses (never student-facing): `dotnet run -- klient <ip> <navn> [kode]` skips every
  screen; `dotnet run -- vaert [kode]` starts hosting without prompts. The discovery *screen*
  itself needs a human hand (synthetic keys never reach raylib); the protocol under it is testable
  with a UDP/TCP probe — see the Hoppebolde case in Status.

## Development setup

`dotnet run` from this folder. Test bypasses for network games (skip every screen):
`dotnet run -- klient <ip> <navn> [kode]` and `dotnet run -- vaert [kode]`. F3 toggles the 3D
debug view; P toggles the player box.

**On a fresh checkout there is no `program.cs`** — it is gitignored on purpose. Copy
a template's `program.cs` (e.g. `GameTemplates/Hoppebolde/program.cs`) to the root as `program.cs`
before the first `dotnet run`, or the build fails with no entry point. Same for `MyGames/` (only
its README and template are tracked) and `Assets/mine/`.

**Verifying visually on this machine:** GDI screen capture (`CopyFromScreen`) returns a blank white
rectangle for the raylib window — it cannot read the accelerated surface, and this is true for raw
raylib too, not just Kraken. Killing the app also leaves a ghost white window behind that is easy to
mistake for a live one. Synthetic keystrokes (`SendKeys`, `AppActivate`) do not reach the window
either, so a script cannot press a key.

Kraken has **no screenshot feature**, and does not need one. Nothing in the engine has to change to
take a picture — put a throwaway component in `program.cs` (gitignored) and capture from `Update`:

```csharp
public override void Update(GameContext context)   // ikke Render/RenderUI
{
    var img = Raylib.LoadImageFromScreen();
    Raylib.ExportImage(img, "test.png");
    Raylib.UnloadImage(img);
}
```

- **It must be `Update`.** `Update` runs before `RenderFrame()`, so the buffer still holds the
  previous frame, complete: HUD, FPS counter, IP, credits, blocking overlays and all. Capturing from
  `Render`/`RenderUI` instead lands mid-render — a component's `RenderUI` runs before the other
  components' and before `RenderEngineUI()`, so the picture comes out with the world but no HUD.
  Verified both ways on 2026-08-29.
- Wait a beat before the first shot (`context.After(1.5f, ...)`); frame zero has nothing to read.
  Give the component `RunsWhileBlocked => true` or it will not tick behind a start screen.
- Use `ExportImage`, not `TakeScreenshot`: `TakeScreenshot` calls `GetFileName` internally, throws
  any directory away and always writes next to the exe.
- Reading the framebuffer costs that frame — the FPS counter dips in the picture. Harmless.
- Delete the component and the pngs afterwards.

raylib's **own F12 binding is compiled into `EndDrawing`** and cannot be turned off, so a real F12
press drops a `screenshotNNN.png` next to the exe, with a counter that restarts every run and
overwrites. `.gitignore` covers it.

## Status (2026-08-30)

The engine is **feature-complete for the start of the season** and builds clean. Everything on the
original plan is done, and a second game (Pong) has been built on it to find what was missing:

1. ~~Copy from `forår 2026`, rename, target a current .NET~~
2. ~~3D loop, Render/RenderUI split, orthographic camera, asset cache~~
3. ~~Object-initializer convention, all 18 spring components ported~~
4. ~~Collision, capability interfaces, tags, events, timers, shared state~~
5. ~~Server-authoritative network replication~~
6. ~~`Readme.md` as the full component catalogue~~

### What has actually been run and checked

- **Verified again on a second machine (2026-08-29, Intel Arc 140V, SDK 10.0.400):** fresh checkout,
  `program.cs` copied from the template, builds 0/0 and renders the full template game — coins,
  player, cannon and bullet, both power-ups, HUD (score, coins left, high score, lives, IP, 60 FPS)
  and the credits line. Captured from a throwaway component in `program.cs`, with no engine change
  and no keypress.
- **F3 confirmed by hand (2026-08-29):** it tilts the world to a raised three-quarter view. Kraken has
  no F12 binding of its own; raylib's is what fires, and the stray png is gitignored.
- Builds with 0 errors and 0 warnings.
- Single player renders correctly (coins, players, cannon, bullets, buffs, HUD, credits).
- Server + client on one machine: client mirrors the server exactly, 32 components on both sides.
- **The "client does not need the server's code" claim is verified** — a client that deliberately
  did not register `"moent"` drew grey `UnknownEntity` boxes labelled `moent` and kept playing,
  while everything it did know rendered normally.
- Collision → `ICollectable` → score, `IHarmful` → damage, `Healed` → lives, and `GAMESTATE`
  replication (client showed the server's score) all confirmed by instrumented test runs.

### The Hoppebolde case (2026-08-31)

`GameTemplates/Hoppebolde/` is the second template and the network test game: every player steers
a marker (arrows), Enter fires a bouncing ball, the server owns the balls, all clients see the
same ones. Built to exercise the discovery/password/Network-mode work. Verified on one machine:

- Server + two bypass clients (Anna, Bo): each injected an Enter press by sending the raw
  `INPUT;0000001` line (keyboard cannot be scripted); the server spawned exactly one ball per
  client. At a server-broadcast trace signal all three processes reported **identical ball sets**
  (same radii, positions within 4 px — one 20 Hz snapshot) and the replicated roster `Bo, Anna`.
  A client screenshot shows both balls, three named markers and the P box.
- UDP probe: `DISCOVER` → `GAME;HOPPEBOLDE;<maskine>;12345;0;1` with a password set.
- TCP probe: wrong code → `DENIED;forkert kode`; right code → `WELCOME`.
- End to end with password: a client passing the right code joined and synced; one with a wrong
  code never entered the game and never appeared in the roster.
- Found and fixed on the way: the TCP listener bound the LAN address only (loopback clients could
  not connect), and the roster had to be change-detected or it broadcast 20×/s.
- Not scriptable, press by hand once: the start screen (game list + "Start dit eget spil" on top,
  arrow keys + Enter) and the host name/kode/player-name prompts. The protocol under them is what
  the probes covered.

**Review round (2026-09-01)** — five user findings, all fixed and re-verified the same way:
the separate role screen is gone (merged into the start screen, top row hosts); the host is asked
for a player name and appears first in the roster (`spillere='Hans, Anna, Bo'` measured);
discovery no longer lists the same server twice (keyed by `vaert:tcpPort`, sniff probe returned
exactly 1 row with the loopback address); and client stutter is fixed with snapshot interpolation
+ TCP `NoDelay` — measured by counting frames where a ball moved on a client: 40/119 before
(≈20 Hz stepping), 119/119 after. See "Network mode and discovery" for the design notes.

### MOENTJAGT removed (2026-09-01)

User decision: MOENTJAGT (`program.cs.template`) is deleted, along with the components only it
used and the ones nothing used — 15 files. `Components/` now holds exactly `StaticText`,
`StartScreen`, `Light`, `SoundEffects`. Readme (Kom i gang, catalog, spring-conversion table),
protocol examples, engine XML-doc examples, `.gitignore`'s `highscore.txt` line and both
templates' comments were swept for references. The capability interfaces were deleted by the
user right after (no shipped consumer, and too game-specific — see Interaction model); the
engine events stay, with `SoundEffects` as their consumer.

### The Pong case (2026-08-29/31)

A one-player Pong, built to find engine gaps. It ships as the first game template:
**`GameTemplates/Pong/`**, complete including the ball, so it works out of the box after the copy
flow (whole folder → `MyGames/Pong/`, its `program.cs` copied over the root one — verified
2026-08-31 with the rename and the `MyGames/**/program.cs` exclusion in place: the stray copy
compiles as nothing, the ten components compile from `MyGames/Pong/`, and the game renders).
All components are `namespace Mine`, one class per file. `Beskeder.cs` holds the
contract between the ball and the rest (`IHarRetning` + the `BatRamt`/`Maal`/`BoldenServes`
records — pure event bus, so a student can delete `Bold.cs` and write their own; the README's
wildest idea says exactly that). Engine gaps fixed along the way:

- `DeltaTime` clamp (first frame spike moved things a second's worth).
- `Collider.Width/Height` — the ball needed the bat's height for the bounce angle and could not ask.
- `Score` with empty `Label` rendered `: 0`.
- `Camera.Distance` automatic, far clip raised — perspective was unusable without both.
- Light, `Draw.Ball` as a real sphere, `Draw.Model` rotation overloads, `Assets.Checkered/Ball`,
  `Draw.Circle`, `Draw.Shaded`, `Light` component.
- Sound: `Assets.Tone/Noise` (synthesized, no files), `Play(name, volume, pitch)`, missing sound
  is silence instead of a crash, and the `SoundEffects` component (event-driven). Pong plays serve,
  wall, goal and a bat hit whose pitch rises with ball speed. Verified programmatically
  (`IsSoundPlaying`, sample counts, missing-file path); listen once to judge the sound design.

Everything above was verified with screenshots (rolling checkered sphere, shaded bats and ball,
star parallax, camera shake, trail, hit flash + ring, shader glint) and MOENTJAGT re-checked with
a `Light` added: coins, player and cannon shade correctly, HUD untouched. The keyboard path
(`context.Input`) is the one thing a script cannot exercise; play it once.

Measured: the O(n²) collision pass holds 60 FPS up to ~2000 colliders on the Arc 140V and breaks
at 3000 (112 ms/frame). The template game has ~30. Not a problem; noted so nobody guesses.

### Known gaps

- The O(n²) collision pass is fine at the scale we play at, but has no spatial partitioning.
- A client that calls `Remove` on a server-controlled component loses it permanently; nothing
  guards against it yet.
- `Lighting.Apply` looks uniform locations up by name on every call. Fine for a handful of shaded
  draws per frame; cache per shader id if someone shades hundreds of things.
- Hoppebolde has no cap on balls; enough Enter-mashing eventually reaches the collider count
  where frames get slow (~2000 measured).

### Ideas not built, roughly by value

A tilemap / level component; sprite-sheet animation; sounds heard on clients in networked games
(events only fire on the server); client-side prediction so remote players stop lagging on a slow
link; a spatial grid for collision.
