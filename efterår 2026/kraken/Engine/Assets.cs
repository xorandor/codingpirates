using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Kraken;

/// <summary>
/// Loader billeder, 3D-modeller og lyde fra Assets-mappen, og husker dem saa de kun
/// loades een gang. Kald f.eks. Assets.Texture("coin.png").
///
/// Filer kan ligge to steder:
///   Assets/          - delte filer, som ligger i git
///   Assets/mine/     - dine egne filer, som ALDRIG kommer i git
/// Assets/mine/ vinder hvis den samme fil findes begge steder.
///
/// Findes filen slet ikke, faar du et lyseroedt/sort ternet placeholder-billede
/// i stedet for et crash. Saa er det nemt at se hvad der mangler.
/// </summary>
public static class Assets
{
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly Dictionary<string, Model> Models = new();
    private static readonly Dictionary<string, Sound> Sounds = new();
    private static Texture2D? _missing;

    /// <summary>Mappen der ledes i. Behoever normalt ikke aendres.</summary>
    public static string Root { get; set; } = "Assets";

    public static Texture2D Texture(string name)
    {
        if (Textures.TryGetValue(name, out var cached))
            return cached;

        string? path = Resolve(name);
        var texture = path != null ? LoadTexture(path) : MissingTexture();
        Textures[name] = texture;
        return texture;
    }

    public static Model Model(string name)
    {
        if (Models.TryGetValue(name, out var cached))
            return cached;

        string? path = Resolve(name);
        if (path == null)
            throw new FileNotFoundException($"Kraken kunne ikke finde 3D-modellen '{name}'. Laeg den i {Root}/ eller {Root}/mine/.");

        var model = LoadModel(path);
        Models[name] = model;
        return model;
    }

    public static Sound Sound(string name)
    {
        if (Sounds.TryGetValue(name, out var cached))
            return cached;

        string? path = Resolve(name);
        if (path == null)
            throw new FileNotFoundException($"Kraken kunne ikke finde lyden '{name}'. Laeg den i {Root}/ eller {Root}/mine/.");

        var sound = LoadSound(path);
        Sounds[name] = sound;
        return sound;
    }

    /// <summary>Spiller en lyd med det samme.</summary>
    public static void Play(string name) => PlaySound(Sound(name));

    private static string? Resolve(string name)
    {
        string mine = Path.Combine(Root, "mine", name);
        if (File.Exists(mine)) return mine;

        string shared = Path.Combine(Root, name);
        if (File.Exists(shared)) return shared;

        return null;
    }

    private static Texture2D MissingTexture()
    {
        if (_missing != null) return _missing.Value;

        var image = GenImageChecked(64, 64, 8, 8, Color.Magenta, Color.Black);
        var texture = LoadTextureFromImage(image);
        UnloadImage(image);
        _missing = texture;
        return texture;
    }

    internal static void UnloadAll()
    {
        // Placeholder-billedet kan ligge i cachen under flere navne, men maa kun
        // frigives een gang - ellers frigiver vi den samme tekstur to gange.
        foreach (var texture in Textures.Values)
            if (_missing == null || texture.Id != _missing.Value.Id) UnloadTexture(texture);

        foreach (var model in Models.Values) UnloadModel(model);
        foreach (var sound in Sounds.Values) UnloadSound(sound);
        if (_missing != null) UnloadTexture(_missing.Value);

        Textures.Clear();
        Models.Clear();
        Sounds.Clear();
        _missing = null;
    }
}
