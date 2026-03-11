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
