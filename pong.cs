//requirements: dotnet console app. Add nuget package raylib-cs
using Raylib_cs;
using System.Numerics;

//variable
var ballPosition = Vector2.Zero;
var ballDirection = new Vector2(5, 5);
var ballRadius = 10;

var playerPosition = new Vector2(5, 0);
var enemyPosition = Vector2.Zero;

var playerMoveSpeed = 10;
var playerSize = new Vector2(10, 100);

var playerPoints = 0;
var enemyPoints = 0;

//init
Raylib.InitWindow(800, 480, "Pong");
Raylib.SetTargetFPS(60);

enemyPosition = new Vector2(Raylib.GetScreenWidth() - 5 - playerSize.X, 0);
ResetGame();

//game loop
while(!Raylib.WindowShouldClose())
{
    //logic
    if(Raylib.IsKeyDown(KeyboardKey.Down))
    {
        playerPosition.Y = Raymath.Clamp(playerPosition.Y + playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if (Raylib.IsKeyDown(KeyboardKey.Up))
    {
        playerPosition.Y = Raymath.Clamp(playerPosition.Y - playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if(enemyPosition.Y < ballPosition.Y)
    {
        enemyPosition.Y = Raymath.Clamp(enemyPosition.Y + playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if (enemyPosition.Y > ballPosition.Y)
    {
        enemyPosition.Y = Raymath.Clamp(enemyPosition.Y - playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if (Raylib.CheckCollisionCircleRec(ballPosition, ballRadius, new Rectangle(playerPosition, playerSize)))
    {
        ballDirection.X *= -1;
        //calculate new angle for direction vector
    }

    if (Raylib.CheckCollisionCircleRec(ballPosition, ballRadius, new Rectangle(enemyPosition, playerSize)))
    {
        ballDirection.X *= -1;
        //calculate new angle for direction vector
    }

    if (ballPosition.X <= ballRadius)
    {
        enemyPoints++;
        ResetGame();
    }

    if(ballPosition.X > Raylib.GetScreenWidth())
    {
        playerPoints++;
        ResetGame();
    }

    if (ballPosition.Y >= Raylib.GetScreenHeight() - ballRadius || ballPosition.Y <= 0)
    {
        ballDirection.Y *= -1;
    }

    ballPosition += ballDirection;

    //render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.White);

    int lineHeight = 0;
    while(lineHeight < Raylib.GetScreenHeight())
    {
        var lineLength = 10;
        var lineSpace = 5;
        
        var startPosition = new Vector2(Raylib.GetScreenWidth() / 2, lineHeight);
        var endPosition = startPosition + new Vector2(0, lineLength);

        Raylib.DrawLineEx(startPosition, endPosition, 3, Color.Black);

        lineHeight += lineLength + lineSpace;
    }

    Raylib.DrawRectangleV(playerPosition, playerSize, Color.Blue);
    Raylib.DrawRectangleV(enemyPosition, playerSize, Color.Blue);
    
    Raylib.DrawCircleV(ballPosition, ballRadius, Color.Red);

    Raylib.DrawText(playerPoints.ToString(), Raylib.GetScreenWidth()/2 - 30 - Raylib.MeasureText(playerPoints.ToString(), 30), 30, 30, Color.Black);
    Raylib.DrawText(enemyPoints.ToString(), Raylib.GetScreenWidth() / 2 + 30, 30, 30, Color.Black);

    Raylib.DrawFPS(0,0);

    Raylib.EndDrawing();
}

void ResetGame()
{
    ballPosition = new Vector2(50, 400);
    //stick ball to player, await player shoots
}

Raylib.CloseWindow();
