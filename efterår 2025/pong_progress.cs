
using Raylib_cs;
using System.Numerics;

Raylib.InitWindow(1200, 800, "Pong Game");
Raylib.SetTargetFPS(60);
var ballPosition = new Vector2(100, 100);
var ballDirection = new Vector2(1, 1);

var playerPosition = new Vector2(20, 200);
var playerSize = new Vector2(20, 125);
var playerSpeed = 8;

var enemyPosition = new Vector2(Raylib.GetScreenWidth() - 40, 200);
var enemyDirection = new Vector2(0, 1);

var defaultSpeed = 5f;
var speed = defaultSpeed;

var radius = 20;

var playerScore = 0;
var enemyScore = 0;

while (!Raylib.WindowShouldClose())
{
    MovePlayer();
    MoveEnemy();


    if ((ballPosition.Y + radius) >= Raylib.GetScreenHeight()
        || ballPosition.Y - radius <= 0)
    {
        ballDirection.Y *= -1;
    }

    if (ballPosition.X + radius >= Raylib.GetScreenWidth())
    {
        playerScore++;
        ballPosition = new Vector2(600, 100);
        ballDirection = new Vector2(-1, 1);
        speed = defaultSpeed;
    }

    if (ballPosition.X - radius <= 0)
    {
        enemyScore++;
        ballPosition = new Vector2(100, 100);
        ballDirection = new Vector2(1, 1);
        speed = defaultSpeed;
    }

    if (Raylib.CheckCollisionCircleRec(ballPosition, radius, new Rectangle(playerPosition, playerSize)))
    {
        CalcAngle(false, playerPosition); 
        ballPosition.X += speed;
    }

    if (Raylib.CheckCollisionCircleRec(ballPosition, radius, new Rectangle(enemyPosition, playerSize)))
    {
        CalcAngle(true, enemyPosition);
        ballPosition.X -= speed;
    }

    ballPosition += ballDirection * speed;

    Console.WriteLine(ballPosition);

    // render
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.White);

    Raylib.DrawFPS(0, 0);

    var lineHeight = 0;
    var lineSize = 20;

    while (lineHeight < Raylib.GetScreenHeight())
    {
        Raylib.DrawLineEx(
            new Vector2(Raylib.GetScreenWidth() / 2, lineHeight),
            new Vector2(Raylib.GetScreenWidth() / 2, lineHeight + lineSize), 3, Color.Black);


        lineHeight += lineSize + 7;
    }

    Raylib.DrawRectangleV(playerPosition, playerSize, Color.Pink);
    Raylib.DrawRectangleV(enemyPosition, playerSize, Color.Red);

    Raylib.DrawCircleV(ballPosition, radius, Color.Red);

    var playerScoreWidth = Raylib.MeasureText(playerScore.ToString(), 40);
    Raylib.DrawText(playerScore.ToString(), Raylib.GetScreenWidth() / 2 - 50 - playerScoreWidth, 20, 40, Color.Black);

    Raylib.DrawText(enemyScore.ToString(), Raylib.GetScreenWidth() / 2 + 50, 20, 40, Color.Black);

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
    enemyPosition.Y = Raymath.Clamp(
        enemyPosition.Y + playerSpeed * enemyDirection.Y,
        0, Raylib.GetScreenHeight() - playerSize.Y);

    if (enemyPosition.Y >= Raylib.GetScreenHeight() - playerSize.Y ||
        enemyPosition.Y <= 0)
    {
        enemyDirection.Y *= -1;
    }

    var distanceToBall = enemyPosition.X - ballPosition.X;

    if (distanceToBall < 50)
    {
        if (ballPosition.Y > enemyPosition.Y + playerSize.Y / 2)
        {
            enemyDirection.Y = 1;
        }

        if (ballPosition.Y < enemyPosition.Y + playerSize.Y / 2)
        {
            enemyDirection.Y = -1;
        }
    }
}

void CalcAngle(bool isEnemy, Vector2 pos)
{
   var centerY = pos.Y + playerSize.Y / 2;
   var deadZone = playerSize.Y * 0.1;
   var maxAngle = 65f;
   var outputAngle = 0f;

   var distToCenterP = Math.Clamp(
       Math.Abs((centerY - ballPosition.Y) / (playerSize.Y / 2)),
       0, 1);
   
   maxAngle *= distToCenterP;

   if (ballPosition.Y > centerY + deadZone)
   {
       outputAngle = -maxAngle;
   } 
   else if (ballPosition.Y < centerY - deadZone)
   {
       outputAngle = maxAngle;
   }

   if (isEnemy)
   {
       outputAngle = -180 - outputAngle;
   }
   
   var radians = (Math.PI / 180) * outputAngle;
   
   
   speed *= 1.15f;
   
   ballDirection.X = (float)Math.Cos(radians);
   ballDirection.Y = (float)-Math.Sin(radians); 
}