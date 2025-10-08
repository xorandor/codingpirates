
using Raylib_cs;
using System.Numerics;

Raylib.InitWindow(800, 600, "Pong Game");
Raylib.SetTargetFPS(60);

var position = new Vector2(100, 100);
var direction = new Vector2(1, 1);

var playerPosition = new Vector2(20, 200);
var playerSize = new Vector2(20, 150);
var playerSpeed = 10;

var enemyPosition = new Vector2(Raylib.GetScreenWidth() - 40, 200);

var speed = 5;

var radius = 20;

while (!Raylib.WindowShouldClose())
{
    MovePlayer();
    MoveEnemy();

    position += direction * speed;

    if ((position.Y + radius) >= Raylib.GetScreenHeight()
        || position.Y - radius <= 0)
    {
        direction.Y *= -1;
    }

    if (position.X + radius >= Raylib.GetScreenWidth())
    {
        direction.X *= -1;
    }

    if (position.X - radius <= 0)
    {
        Console.WriteLine("død");
    }


    if (Raylib.CheckCollisionCircleRec(position, radius, new Rectangle(playerPosition, playerSize)))
    {
        direction.X *= -1;
    }

    if (Raylib.CheckCollisionCircleRec(position, radius, new Rectangle(enemyPosition, playerSize)))
    {
        direction.X *= -1;
    }

    Console.WriteLine(position);

    // render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.White);

    Raylib.DrawFPS(0, 0);

    var lineHeight = 0;
    var lineSize = 20;

    while(lineHeight < Raylib.GetScreenHeight())
    {
        Raylib.DrawLineEx(
            new Vector2(Raylib.GetScreenWidth() / 2, lineHeight), 
            new Vector2(Raylib.GetScreenWidth() / 2, lineHeight + lineSize), 3, Color.Black);


        lineHeight += lineSize + 7;
    }


    Raylib.DrawRectangleV(playerPosition, playerSize, Color.Pink);
    Raylib.DrawRectangleV(enemyPosition, playerSize, Color.Red);

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

void MoveEnemy()
{
    if (position.Y > enemyPosition.Y + playerSize.Y / 2)
    {
        enemyPosition.Y = Raymath.Clamp(enemyPosition.Y + playerSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if (position.Y < enemyPosition.Y + playerSize.Y / 2)
    {
        enemyPosition.Y = Raymath.Clamp(enemyPosition.Y - playerSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }
}
