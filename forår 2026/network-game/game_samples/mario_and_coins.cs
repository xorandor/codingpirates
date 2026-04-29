using Components;
using Engine;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

var mode = args.Contains("server") ? GameMode.Server : GameMode.Client;

InitWindow(1000, 800, "network game");
SetTargetFPS(60);

var random = new Random();

var game = new GameEngine(Color.RayWhite, mode);

game.Add(new CircleShooter(
    new Vector2(200, 600),
    radius: 30,
    stickLength: 40,
    Color.Red,
    autoShootIntervalMs: 2000,
    maxBounces: 5));

var score = new Score(new Vector2(10, 30));
game.Add(score);

var coinCounter = game.Add(new CoinCounter(new Vector2(10, 70)));

var player = new Player(new Vector2(400, 300), speed: 300f, radius: 30f, Color.Red);

player.OnGameStarted += (se, ea) =>
{
    foreach (var coin in game.Components.OfType<Coin>().ToList())
    {
        game.Remove(coin);
    }

    foreach (var circleshooter in game.Components.OfType<CircleShooter>().ToList())
    {
        game.Remove(circleshooter);
    }

    for (int i = 0; i < 3; i++)
    {
        game.Add(new Coin(new Vector2(
            random.Next(0, GetScreenWidth()),
            random.Next(0, GetScreenHeight())
        ), innerColor: Color.Red, outerColor: Color.Orange));
    }
};

player.OnCoinCollected += (se, ea) =>
{
    score.Points++;
    Console.WriteLine(game.Components.OfType<Coin>().Count());
    if (coinCounter.Count == 0)
    {
        for (int i = 0; i < 3; i++)
        {
            game.Add(new Coin(new Vector2(
                random.Next(0, GetScreenWidth()),
                random.Next(0, GetScreenHeight())
            ), innerColor: Color.Red, outerColor: Color.Orange));
        }
    }

    game.Add(new CircleShooter(new Vector2(100, 100), maxBounces: 3, autoShootIntervalMs: 1000));
};

game.Add(player);

game.Run();