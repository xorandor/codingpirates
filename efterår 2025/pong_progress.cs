using Raylib_cs;
using System.Numerics;

Raylib.InitWindow(800, 600, "Pong Game");
Raylib.SetTargetFPS(60);

var position = new Vector2(100,100);
var direction = new Vector2(1, 1);

var playerPosition = new Vector2(20, 200);
var playerSize = new Vector2(20, 150);
var playerSpeed = 10;

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

    if(position.X + radius >= Raylib.GetScreenWidth())
    {
        direction.X *= -1;
    }

    if (position.X - radius <= 0)
    {
        Console.WriteLine("død");
    }

    MovePlayer();

    if (Raylib.CheckCollisionCircleRec(position, radius, new Rectangle(playerPosition, playerSize)))
    {
        direction.X *= -1;
    }
    
    Console.WriteLine(position);

    // render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.White);

    Raylib.DrawFPS(0, 0);

    Raylib.DrawRectangleV(playerPosition, playerSize, Color.Pink);

    Raylib.DrawCircleV(position, radius, Color.Red);

    Raylib.EndDrawing();
}

void MovePlayer()
{
    if (Raylib.IsKeyDown(KeyboardKey.S))
    {
        playerPosition.Y = Raymath.Clamp(playerPosition.Y + playerSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if (Raylib.IsKeyDown(KeyboardKey.W))
    {
        playerPosition.Y = Raymath.Clamp(playerPosition.Y - playerSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }
}
