# Kraken

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
- Raylib-cs 7.0.2 for everything graphical and audio.
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

## Project structure

The project must stay simple enough that a student can copy a single file into their own checkout
without pulling from git. Four concepts:

- **`Engine/`** — the motor. Everything else may reference it; it references nothing else.
- **`Components/`** — shared components, one class per file. **A component must never reference
  another component's concrete type.** Interact through the interaction model below instead.
  Only the composition may reference this folder.
- **`MyComponents/`** — a student's private components, namespace `Mine`. Gitignored wholesale.
  Never commit anything from here, and never assume another machine has these files.
- **`program.cs`** — the composition. Gitignored. The only place allowed to reference everything.
  `program.cs.template` is the committed starting point; keep the two in sync.

The spring originals are **not** copied into this folder. If you need to see how something used to
work, read `forår 2026/network-game/` — it is still in the repo, one directory up. Do not modify
anything under `forår 2026/`; that season is finished and stays as it was.

## Conventions

- **Configuration is object initializers, never constructor parameters.** Every component has an
  implicit parameterless constructor and public get/set properties with sensible defaults:
  `game.Add(new Player { Speed = 300, Color = Color.SkyBlue })`. Do not add constructors to
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
2. **Capability interfaces** — `ICollectable`, `IDamageable`, `IHarmful`, `IPushable`, plus `Tags`
   and `context.FindByTag`. This is what lets a private component in `Mine` interact with a shared
   one. `Player` only ever asks "can this be picked up / does this hurt", never "is this a Coin".
3. **Typed event bus** — `context.On<T>(handler)` (auto-unsubscribes when the component is removed)
   and `context.Publish(...)`. Engine events: `GameStarted`, `GameOver`, `GameWon`, `Collected`,
   `Damaged`, `Healed`, `Died`, `ScoreChanged`, `PlayerJoined`, `PlayerLeft`.
4. **Timers** — `context.After(s, action)`, `context.Every(s, action)`. Owned by the component that
   created them: cancelled on removal, and paused while a blocking component is up unless the owner
   sets `RunsWhileBlocked`.
5. **Shared state** — `context.State.Add("score", 10)` / `Number` / `Flag` / `Text`. `Score` is only
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
- Purely decorative networked-looking things should return `null` from `NetworkKind` so each machine
  makes its own (see `FallingCoin`).

`Network protocol.md` defines the message contracts and must be updated whenever a message is added
or changed. Protocol rules: UTF-8, one message per line terminated by `\n`, `;` separator, ALL CAPS
command first, receivers ignore unknown commands without crashing. Floats always use `.` via
`CultureInfo.InvariantCulture`.

## Development setup

`dotnet run` from this folder. `dotnet run -- klient <ip> <navn>` starts in client mode and skips the
prompts. F3 toggles the 3D debug view.

**On a fresh checkout there is no `program.cs`** — it is gitignored on purpose. Copy
`program.cs.template` to `program.cs` before the first `dotnet run`, or the build fails with no
entry point. Same for `MyComponents/` (only its README and template are tracked) and `Assets/mine/`.

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

## Status (2026-08-29)

The engine is **feature-complete for the start of the season** and builds clean. Everything on the
original plan is done:

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

### Known gaps

- The O(n²) collision pass is fine at the scale we play at, but has no spatial partitioning.
- A client that calls `Remove` on a server-controlled component loses it permanently; nothing
  guards against it yet.

### Ideas not built, roughly by value

Sound effects on the standard components; a tilemap / level component; sprite-sheet animation;
client-side prediction so remote players stop lagging on a slow link; a spatial grid for collision.
