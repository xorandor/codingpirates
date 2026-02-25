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

game.Run();
