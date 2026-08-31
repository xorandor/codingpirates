// -----------------------------------------------------------------------------
// PONG - det klassiske. Du styrer venstre bat med pil op/ned, computeren har hoejre.
// Foerste til 11 vinder.
//
// Kopier denne fil op i roden som din program.cs, og de andre .cs-filer herfra
// ind i MyComponents/. Saa: dotnet run. Se README.md her i mappen.
// -----------------------------------------------------------------------------

using Components;
using Kraken;
using Mine;
using Raylib_cs;
using System.Numerics;

var game = new GameEngine
{
    Title = "Pong",
    Width = 1280,
    Height = 720,
    Background = Color.Black,
    Mode = GameMode.Server
};

// 1 world unit = 1 pixel. Midten er (0,0). x gaar fra -640 til 640, y fra -360 til 360.
game.Camera.Height = 720;
game.Camera.Perspective = true;   // ting laengere vaek bliver mindre - se stjernerne

// Skaerm og tal.
game.Add(new StartScreen { Title = "PONG", Subtitle = "Pil op/ned styrer. Tryk Enter for at starte" });
game.Add(new PointTavle());
game.Add(new Slutskaerm { PointForAtVinde = 11 });
game.Add(new SoundEffects());

// Selve banen.
game.Add(new Stjerner());
game.Add(new Light { Position = new(-350, 300, 650) });
game.Add(new KameraRyk());
game.Add(new Halespor { Tag = "bold" });
game.Add(new MidterLinje());
game.Add(new Bat { Position = new(-600, 0, 0), Styring = Styring.Tastatur, Fart = 520 });
game.Add(new Bat { Position = new(600, 0, 0), Styring = Styring.Computer, Fart = 420 });
game.Add(new Bold());

game.Run();
