namespace Mine;

/// <summary>En linje paa highscore-listen.</summary>
public record Placering(string Navn, int Point);

/// <summary>
/// Laeser og gemmer highscore-listen i en lille tekstfil - een linje per placering, "navn;point".
/// Filen ligger i Assets/mine/, som aldrig kommer i git, saa listen er din egen.
/// </summary>
public static class Highscore
{
    public static string Fil { get; set; } = Path.Combine("Assets", "mine", "asteroids-highscore.txt");
    public static int Antal { get; set; } = 10;

    /// <summary>Listen, bedste foerst. Tom hvis filen ikke findes endnu.</summary>
    public static List<Placering> Laes()
    {
        try
        {
            if (!File.Exists(Fil)) return [];

            var liste = new List<Placering>();
            foreach (var linje in File.ReadAllLines(Fil))
            {
                int skille = linje.LastIndexOf(';');
                if (skille < 0) continue;
                if (!int.TryParse(linje[(skille + 1)..], out int point)) continue;
                liste.Add(new Placering(linje[..skille], point));
            }

            return liste.OrderByDescending(p => p.Point).Take(Antal).ToList();
        }
        catch
        {
            return [];   // en odelagt fil skal ikke vaelte spillet - saa starter listen bare forfra
        }
    }

    /// <summary>Det bedste resultat paa listen - 0 hvis listen er tom.</summary>
    public static int Bedste() => Laes().FirstOrDefault()?.Point ?? 0;

    /// <summary>Er dette resultat godt nok til at komme paa listen?</summary>
    public static bool KommerPaaListen(int point)
    {
        var liste = Laes();
        return liste.Count < Antal || point > liste[^1].Point;
    }

    /// <summary>Laegger et resultat paa listen og gemmer. Kun de Antal bedste bliver.</summary>
    public static void Gem(string navn, int point)
    {
        var liste = Laes();
        liste.Add(new Placering(navn, point));
        liste = liste.OrderByDescending(p => p.Point).Take(Antal).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(Fil)!);
        File.WriteAllLines(Fil, liste.Select(p => $"{p.Navn};{p.Point}"));
    }
}
