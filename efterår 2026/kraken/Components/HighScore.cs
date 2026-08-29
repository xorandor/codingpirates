using System.Globalization;
using System.Numerics;
using Kraken;
using Raylib_cs;

namespace Components;

/// <summary>
/// Husker det hojeste pointtal - ogsaa efter at spillet er lukket ned.
///
///   game.Add(new HighScore());
///   game.Add(new HighScore { ScreenPosition = new(20, 120), FileName = "banerekord.txt" });
///
/// Rekorden ligger i en helt almindelig tekstfil ved siden af spillet, saa du kan
/// aabne den og kigge - og slette den hvis du vil starte forfra.
/// </summary>
public class HighScore : Component
{
    /// <summary>Hvilket tal i context.State der holdes oeje med.</summary>
    public string ScoreKey { get; set; } = "score";

    public string Label { get; set; } = "Rekord";
    public Vector2 ScreenPosition { get; set; } = new(20, 120);
    public int FontSize { get; set; } = 24;
    public Color Color { get; set; } = Color.DarkGray;

    /// <summary>Filen rekorden gemmes i. Saet til null for kun at huske den mens spillet koerer.</summary>
    public string? FileName { get; set; } = "highscore.txt";

    public int Points { get; private set; }

    public override bool Persistent => true;

    private bool _unsaved;

    public override void OnAdded(GameContext context)
    {
        Points = Load();

        // Vi skriver hoejst filen een gang i sekundet. Ellers ville et regnvejr af monter
        // give hundredvis af skrivninger paa ingen tid.
        context.Every(1f, SaveIfChanged);
    }

    public override void Update(GameContext context)
    {
        int current = context.State.Number(ScoreKey);
        if (current <= Points) return;

        Points = current;
        _unsaved = true;
    }

    public override void OnRemoved(GameContext context) => SaveIfChanged();

    /// <summary>Nulstiller rekorden - ogsaa i filen.</summary>
    public void Reset()
    {
        Points = 0;
        _unsaved = true;
        SaveIfChanged();
    }

    private void SaveIfChanged()
    {
        if (!_unsaved) return;

        _unsaved = false;
        Save();
    }

    public override void RenderUI()
        => Draw.Text($"{Label}: {Points}", ScreenPosition, FontSize, Color);

    private int Load()
    {
        if (FileName == null) return 0;

        try
        {
            return File.Exists(FileName)
                && int.TryParse(File.ReadAllText(FileName).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int saved)
                ? saved
                : 0;
        }
        catch
        {
            return 0; // filen er laast eller uleaselig - saa starter vi bare paa nul
        }
    }

    private void Save()
    {
        if (FileName == null) return;

        try
        {
            File.WriteAllText(FileName, Points.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // kunne ikke skrive - rekorden lever videre i denne omgang, men gemmes ikke
        }
    }
}
