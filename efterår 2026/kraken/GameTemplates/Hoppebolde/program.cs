// -----------------------------------------------------------------------------
// HOPPEBOLDE - et lille netvaerksspil. Alle spillere styrer hver deres markoer med
// pilene, og Enter skyder en hoppebold ud som hopper rundt paa banen. Det er
// serveren der ejer boldene - alle ser praecis de samme.
//
// Kor spillet:       dotnet run   (vaelg et spil i listen, eller "Start dit eget spil" oeverst)
// Spillerliste:      P viser og skjuler den
// -----------------------------------------------------------------------------

using Components;
using Kraken;
using Mine;
using Raylib_cs;
using System.Numerics;

var game = new GameEngine
{
    Title = "HOPPEBOLDE",
    Width = 1280,
    Height = 720,
    Background = new Color(16, 18, 30, 255),
    Network = true,

    // Linjerne herunder er KUN til test - de springer alle skaermene over:
    //   dotnet run -- vaert [kode]                 (start direkte som vaert)
    //   dotnet run -- klient 127.0.0.1 Anna [kode] (forbind direkte, uden soegning)
    Mode = args.Contains("klient") ? GameMode.Client : args.Contains("vaert") ? GameMode.Server : null,
    ServerIp = args.Contains("klient") && args.Length > 1 ? args[1] : "",
    PlayerName = args.Length > 2 ? args[2] : "",
    Password = args.Contains("klient") ? (args.Length > 3 ? args[3] : "") : (args.Contains("vaert") && args.Length > 1 ? args[1] : "")
};

game.Camera.Height = 720;

// Klientens opslagsbog: hvad skal der tegnes naar serveren sender de her slags?
game.NetworkKinds.Register("markoer", () => new Markoer());
game.NetworkKinds.Register("hoppebold", () => new HoppeBold());

game.Add(new Light());
game.Add(new SoundEffects());
game.Add(new StaticText
{
    Text = "Pile: styr din markoer   Enter: ny hoppebold   P: spillere",
    ScreenPosition = new(20, 20),
    FontSize = 20,
    Color = Color.LightGray
});

// En markoer til hver spiller - serveren laver dem, klienterne faar dem tilsendt.
game.Add(new MarkoerPerSpiller());

game.Run();
