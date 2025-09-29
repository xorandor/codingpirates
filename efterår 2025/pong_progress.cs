using Raylib_cs;
using System.Numerics;

Raylib.InitWindow(800, 600, "Pong Game");
Raylib.SetTargetFPS(60);

var position = new Vector2(100,100);
var direction = new Vector2(1, 1);
var speed = 3;

var radius = 50;

while (!Raylib.WindowShouldClose())
{
    //update state
    position += direction * speed;

    if((position.Y + radius) >= Raylib.GetScreenHeight()
        || position.Y - radius <= 0)
    {
        direction.Y *= -1;
    }

    if(position.X + radius >= Raylib.GetScreenWidth()
        || position.X - radius <= 0)
    {
        direction.X *= -1;
    }

    Console.WriteLine(position);

    // render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.White);

    Raylib.DrawFPS(0, 0);

    var playerPosition = new Vector2(20, 200);
    var playerSize = new Vector2(20, 150);

    Raylib.DrawRectangleV(playerPosition, playerSize, Color.Pink);

    Raylib.DrawCircleV(position, radius, Color.Red);

    Raylib.EndDrawing();
}
