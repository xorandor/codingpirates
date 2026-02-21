using Raylib_cs;
using static Raylib_cs.Raylib;

InitWindow(800, 450, "network game");

SetTargetFPS(60);

while(!WindowShouldClose())
{
    BeginDrawing();

    ClearBackground(Color.RayWhite);

    DrawText("network game", 200, 200, 20, Color.Black);

    EndDrawing();
}

CloseWindow();