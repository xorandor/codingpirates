// -----------------------------------------------------------------------------
// ASTEROIDS - skyd asteroiderne i stykker og overlev saa laenge du kan.
// Pil venstre/hoejre drejer skibet, pil op giver gas, mellemrum skyder. Flyver du ud
// over kanten, kommer du ind paa den anden side. Nogle asteroider taber et ekstra liv
// eller en skud-opgradering - saml dem op med skibet. Det bliver svaerere for hvert
// halve minut. Naar du doer, skriver du dit navn, og kommer paa highscore-listen.
//
// Kopier hele Asteroids-mappen ind i MyGames/, og skriv saa een linje i din program.cs
// i roden:   AsteroidsGame.Run();   Saa: dotnet run. Se README.md her i mappen.
// -----------------------------------------------------------------------------

using Components;
using Kraken;
using Raylib_cs;

namespace Mine;

public static class AsteroidsGame
{
    public static void Run()
    {
        var game = new GameEngine
        {
            Title = "Asteroids",
            Width = 1280,
            Height = 720,
            Background = Color.Black
        };

        // 1 world unit = 1 pixel. Midten er (0,0). x gaar fra -640 til 640, y fra -360 til 360.
        game.Camera.Height = 720;
        game.Camera.Perspective = true;   // ting laengere vaek bliver mindre - se stjernerne

        // Tal og skaerme. Pointtaelleren foerst, saa skaermene laegger sig oven paa den.
        game.Add(new Pointtaeller());
        game.Add(new Spilstyring { StartLiv = 3, SekunderPrNiveau = 30f });
        game.Add(new Velkomstskaerm());
        game.Add(new NavneSkaerm());
        game.Add(new SoundEffects());

        // Rummet.
        game.Add(new Stjernehimmel());
        game.Add(new Light { Position = new(-300, 300, 400) });
        game.Lighting.Ambient = new Color(35, 35, 50, 255);   // moerkere grundlys = dybere skygger
        game.Add(new Rystelse());

        // Selve spillet.
        game.Add(new AsteroideSpawner());
        game.Add(new OpgraderingSpawner());
        game.Add(new OpgraderingsEffekter());
        game.Add(new Skib());

        game.Run();
    }
}
