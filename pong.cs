//requirements: dotnet console app. Add nuget package raylib-cs
using Raylib_cs;
using System.Numerics;

//settings
var ballRadius = 10;
var playerMoveSpeed = 10;
var ballSpeed = 7;
var playerSize = new Vector2(10, 100);

var twoPlayer = false;

//variables
var ballPosition = Vector2.Zero;
var ballDirection = Vector2.Zero;
var ballStatus = BallStatus.WithPlayer;

var playerPosition = Vector2.Zero;
var enemyPosition = Vector2.Zero;

var playerPoints = 0;
var enemyPoints = 0;

//init
Raylib.InitWindow(800, 480, "Pong");
Raylib.SetTargetFPS(60);

enemyPosition = new Vector2(Raylib.GetScreenWidth() - 5 - playerSize.X, 0);

//game loop
while (!Raylib.WindowShouldClose())
{
    //logic
    MovePlayer();
    MoveEnemy();

    if (ballPosition.X <= ballRadius)
    {
        enemyPoints++;
        ballStatus = BallStatus.WithPlayer;
    }

    if (ballPosition.X > Raylib.GetScreenWidth())
    {
        playerPoints++;
        ballStatus = BallStatus.WithEnemy;
    }

    switch (ballStatus)
    {
        case BallStatus.WithPlayer:
            ballPosition.X = playerPosition.X + playerSize.X + ballRadius;
            ballPosition.Y = playerPosition.Y + playerSize.Y / 2;
            break;
        case BallStatus.WithEnemy:
            ballPosition.X = enemyPosition.X - playerSize.X;
            ballPosition.Y = enemyPosition.Y + playerSize.Y / 2;
            break;
        case BallStatus.Free:
            ballPosition += ballDirection;
            break;
    }

    if (ballStatus == BallStatus.Free)
    {
        if (Raylib.CheckCollisionCircleRec(ballPosition, ballRadius, new Rectangle(playerPosition, playerSize)))
        {
            ballDirection.X *= -1;
            CalculateBallDirection(BallPlayerDirection.Enemy, playerPosition);
        }

        if (Raylib.CheckCollisionCircleRec(ballPosition, ballRadius, new Rectangle(enemyPosition, playerSize)))
        {
            ballDirection.X *= -1;
            CalculateBallDirection(BallPlayerDirection.Player, enemyPosition);
        }
    }

    if (ballPosition.Y >= Raylib.GetScreenHeight() - ballRadius || ballPosition.Y <= 0)
    {
        ballDirection.Y *= -1;
    }

    if (ballStatus != BallStatus.Free && Raylib.IsKeyDown(KeyboardKey.Space))
    {
        CalculateBallDirection(BallPlayerDirection.Enemy, ballStatus == BallStatus.WithEnemy ? enemyPosition : playerPosition);
        ballStatus = BallStatus.Free;
    }

    //render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.White);

    int lineHeight = 0;
    while (lineHeight < Raylib.GetScreenHeight())
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

    Raylib.DrawText(playerPoints.ToString(), Raylib.GetScreenWidth() / 2 - 30 - Raylib.MeasureText(playerPoints.ToString(), 30), 30, 30, Color.Black);
    Raylib.DrawText(enemyPoints.ToString(), Raylib.GetScreenWidth() / 2 + 30, 30, 30, Color.Black);

    Raylib.DrawFPS(0, 0);

    Raylib.EndDrawing();
}

void MovePlayer()
{
    if (Raylib.IsKeyDown(KeyboardKey.Down))
    {
        playerPosition.Y = Raymath.Clamp(playerPosition.Y + playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }

    if (Raylib.IsKeyDown(KeyboardKey.Up))
    {
        playerPosition.Y = Raymath.Clamp(playerPosition.Y - playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
    }
}

void MoveEnemy()
{
    if(twoPlayer)
    {
        if (Raylib.IsKeyDown(KeyboardKey.S))
        {
            enemyPosition.Y = Raymath.Clamp(enemyPosition.Y + playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
        }

        if (Raylib.IsKeyDown(KeyboardKey.W))
        {
            enemyPosition.Y = Raymath.Clamp(enemyPosition.Y - playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
        }
    }
    else
    {
        if (enemyPosition.Y < ballPosition.Y)
        {
            enemyPosition.Y = Raymath.Clamp(enemyPosition.Y + playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
        }

        if (enemyPosition.Y > ballPosition.Y)
        {
            enemyPosition.Y = Raymath.Clamp(enemyPosition.Y - playerMoveSpeed, 0, Raylib.GetScreenHeight() - playerSize.Y);
        }
    }
}

void CalculateBallDirection(BallPlayerDirection ballPlayerDirection, Vector2 playerPosition)
{
    var maxAngle = 65;
    float angle = 0;

    var playerVerticalCenter = playerPosition.Y + playerSize.Y / 2;

    var anglePercentage = Math.Abs((playerPosition.Y - ballPosition.Y) / (playerSize.Y / 2) + 1);

    if (ballPlayerDirection == BallPlayerDirection.Enemy)
    {
        if (ballPosition.Y < playerVerticalCenter)
        {
            angle = anglePercentage * maxAngle;
        }
        else
        {
            angle = 360 - anglePercentage * maxAngle;
        }
    }

    if (ballPlayerDirection == BallPlayerDirection.Player)
    {
        if (ballPosition.Y < playerVerticalCenter)
        {
            angle = 180 - anglePercentage * maxAngle;
        }
        else
        {
            angle = 180 + anglePercentage * maxAngle;
        }
    }

    var radians = (Math.PI / 180) * angle;

    ballDirection.X = (float)Math.Cos(radians) * ballSpeed;
    ballDirection.Y = (float)-Math.Sin(radians) * ballSpeed;
}

Raylib.CloseWindow();

enum BallStatus
{
    WithPlayer,
    WithEnemy,
    Free
}

enum BallPlayerDirection
{
    Player,
    Enemy
}
