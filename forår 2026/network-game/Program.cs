using Components;
using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

var mode = args.Contains("server") ? GameMode.Server : GameMode.Client;

InitWindow(800, 450, "network game");
SetTargetFPS(60);

var game = new GameEngine(Color.RayWhite, mode);

// Composition: add your components here
game.Add(new StaticText("The network game", position: new Vector2(10, 10), color: Color.Red));
game.Add(new FloatingText("Let's go!", angle: 45, speed: 150));
game.Add(new ArrowKeyControlledBall(new Vector2(200, 200), 300, 20, Color.Black));

game.Run();
