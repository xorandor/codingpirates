using Components;
using Raylib_cs;
using static Raylib_cs.Raylib;
using System.Numerics;

InitWindow(800, 450, "network game");
SetTargetFPS(60);

// Composition: add your components here
var introText = new StaticText("The network game", position: new Vector2(10, 10), color: Color.Red);
var floatingText = new FloatingText("Let's go!", angle: 45, speed: 150);

while (!WindowShouldClose())
{
    introText.Update();
    floatingText.Update();

    BeginDrawing();
    ClearBackground(Color.RayWhite);

    introText.Render();
    floatingText.Render();

    EndDrawing();
}

CloseWindow();
