// -----------------------------------------------------------------------------
// BREAKOUT - sla klodserne itu med bolden foer du mister alle dine liv.
// Pil venstre/hoejre styrer battet. Nogle klodser taber en power-up naar de gaar
// i stykker - saml den op med battet.
//
// Kopier hele Breakout-mappen ind i MyGames/, og skriv saa een linje i din program.cs
// i roden:   BreakoutGame.Run();   Saa: dotnet run. Se README.md her i mappen.
// -----------------------------------------------------------------------------

using Components;
using Kraken;
using Raylib_cs;

namespace Mine;

public static class BreakoutGame
{
    public static void Run()
    {
        var game = new GameEngine
        {
            Title = "Breakout",
            Width = 1280,
            Height = 720,
            Background = Color.Black
        };

        // 1 world unit = 1 pixel. Midten er (0,0). x gaar fra -640 til 640, y fra -360 til 360.
        game.Camera.Height = 720;
        game.Camera.Perspective = true;   // ting laengere vaek bliver mindre - se stjernerne

        // Skaerm og tal.
        game.Add(new StartScreen { Title = "BREAKOUT", Subtitle = "Pil venstre/hoejre styrer. Tryk Enter for at starte" });
        game.Add(new PointTavle());
        game.Add(new Slutskaerm { StartLiv = 3 });
        game.Add(new SoundEffects());

        // Selve banen.
        game.Add(new Stjerner());
        game.Add(new Light { Position = new(0, 250, 300) });
        game.Lighting.Ambient = new Color(35, 35, 50, 255);   // moerkere grundlys = dybere skygger
        game.Add(new KameraRyk());

        game.Add(new KlodsBane());
        game.Add(new PowerUpSpawner());
        game.Add(new PowerUpEffekter());

        game.Add(new Bat { Position = new(0, -310, 0) });
        game.Add(new Bold());

        game.Run();
    }
}
