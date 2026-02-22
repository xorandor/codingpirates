using Components;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

InitWindow(800, 450, "network game");
SetTargetFPS(60);

// Composition: add your components here
var introText = new StaticText("The network game", position: new Vector2(10, 10), color: Color.Red);
var floatingText = new FloatingText("Let's go!", angle: 45, speed: 150);
var movingBall = new ArrowKeyControlledBall(new Vector2(200, 200), 300, 20, Color.Black);

while (!WindowShouldClose())
{
    introText.Update();
    floatingText.Update();
    movingBall.Update();

    BeginDrawing();
    ClearBackground(Color.RayWhite);

    introText.Render();
    floatingText.Render();
    movingBall.Render();

    EndDrawing();
}

CloseWindow();
