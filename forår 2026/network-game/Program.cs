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
game.Add(new ArrowKeyControlledBall(new Vector2(200, 200), 300, 20, Color.Red, 1));
game.Add(new ArrowKeyControlledBall(new Vector2(230, 200), 300, 20, Color.Yellow, 2));
game.Add(new ArrowKeyControlledBall(new Vector2(260, 200), 300, 20, Color.Blue, 3));
game.Add(new ArrowKeyControlledBall(new Vector2(290, 200), 300, 20, Color.Green, 4));
game.Add(new ArrowKeyControlledBall(new Vector2(310, 200), 300, 20, Color.Magenta, 5));
game.Add(new ArrowKeyControlledBall(new Vector2(340, 200), 300, 20, Color.DarkPurple, 6));
game.Add(new ArrowKeyControlledBall(new Vector2(370, 200), 300, 20, Color.Gold, 7));
game.Add(new ArrowKeyControlledBall(new Vector2(400, 200), 300, 20, Color.Lime, 8));
game.Add(new ArrowKeyControlledBall(new Vector2(430, 200), 300, 20, Color.Maroon, 9));
game.Add(new ArrowKeyControlledBall(new Vector2(460, 200), 300, 20, Color.Violet, 10));

game.Run();
