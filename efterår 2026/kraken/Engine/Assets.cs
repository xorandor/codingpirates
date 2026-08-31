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
    private static readonly Dictionary<string, Shader> Shaders = new();
    private static Texture2D? _missing;

    /// <summary>Navnet den indbyggede enhedskugle (radius 1) ligger under i cachen.</summary>
    internal const string UnitSphereName = "*kugle";

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
        {
            // Samme filosofi som billeder: en manglende fil maa ikke crashe. Den bliver stilhed.
            Console.WriteLine($"Kraken kunne ikke finde lyden '{name}' - der spilles stilhed i stedet. " +
                              $"Laeg filen i {Root}/ eller {Root}/mine/, eller lav lyden med Assets.Tone.");
            return FromSamples(name, new short[1]);
        }

        var sound = LoadSound(path);
        Sounds[name] = sound;
        return sound;
    }

    /// <summary>Spiller en lyd med det samme.</summary>
    public static void Play(string name) => Play(name, 1f);

    /// <summary>Spiller en lyd med selvvalgt styrke og tonehoejde. pitch 2 = en oktav op, 0.5 = en ned.</summary>
    public static void Play(string name, float volume, float pitch = 1f)
    {
        var sound = Sound(name);
        SetSoundVolume(sound, volume);
        SetSoundPitch(sound, pitch);
        PlaySound(sound);
    }

    /// <summary>
    /// En lyd lavet af koden - ingen fil noedvendig. En tone der glider fra en frekvens til en
    /// anden og doer ud. Gemmes under navnet og spilles som enhver anden lyd:
    ///   Assets.Tone("plink", 880, 1567, 0.09f);
    ///   Assets.Play("plink");
    /// Firkantboelgen lyder som en gammel spillemaskine. firkant: false giver en bloedere tone.
    /// </summary>
    public static Sound Tone(string name, float fraHz, float tilHz, float sekunder, bool firkant = true)
    {
        if (Sounds.TryGetValue(name, out var cached))
            return cached;

        int antal = Math.Max(1, (int)(sekunder * SampleRate));
        var samples = new short[antal];
        double fase = 0;

        for (int i = 0; i < antal; i++)
        {
            float andel = (float)i / antal;

            // Frekvensen glider, saa fasen skal vokse med den aktuelle frekvens - ellers hakker det.
            double hz = fraHz + (tilHz - fraHz) * andel;
            fase += 2 * Math.PI * hz / SampleRate;

            double boelge = Math.Sin(fase);
            if (firkant) boelge = Math.Sign(boelge) * 0.6;

            double styrke = (1 - andel) * (1 - andel);   // doer ud
            samples[i] = (short)(boelge * styrke * 0.5 * short.MaxValue);
        }

        return FromSamples(name, samples);
    }

    /// <summary>Et sus af stoej der doer ud - god til eksplosioner og maal. Assets.Noise("bang", 0.3f);</summary>
    public static Sound Noise(string name, float sekunder)
    {
        if (Sounds.TryGetValue(name, out var cached))
            return cached;

        int antal = Math.Max(1, (int)(sekunder * SampleRate));
        var samples = new short[antal];
        var tilfaeldig = new Random(7);   // fast froe, saa lyden er ens hver gang

        for (int i = 0; i < antal; i++)
        {
            float andel = (float)i / antal;
            double styrke = (1 - andel) * (1 - andel);
            samples[i] = (short)((tilfaeldig.NextDouble() * 2 - 1) * styrke * 0.4 * short.MaxValue);
        }

        return FromSamples(name, samples);
    }

    private const int SampleRate = 44100;

    private static unsafe Sound FromSamples(string name, short[] samples)
    {
        fixed (short* data = samples)
        {
            var wave = new Wave
            {
                SampleCount = (uint)samples.Length,
                SampleRate = SampleRate,
                SampleSize = 16,
                Channels = 1,
                Data = data
            };

            // LoadSoundFromWave kopierer samples ind i lydsystemet, saa arrayet maa gerne forsvinde.
            var sound = LoadSoundFromWave(wave);
            Sounds[name] = sound;
            return sound;
        }
    }

    /// <summary>En shader, loadet fra to filer i Assets-mappen. Bruges af motorens lys.</summary>
    public static Shader Shader(string vertexName, string fragmentName)
    {
        string key = vertexName + "|" + fragmentName;
        if (Shaders.TryGetValue(key, out var cached))
            return cached;

        string? vs = Resolve(vertexName);
        string? fs = Resolve(fragmentName);
        if (vs == null || fs == null)
            throw new FileNotFoundException($"Kraken kunne ikke finde shaderen '{vertexName}' / '{fragmentName}' i {Root}/.");

        var shader = LoadShader(vs, fs);
        Shaders[key] = shader;
        return shader;
    }

    /// <summary>En kugle med radius 1 og rigtige normaler. Det er den Draw.Ball tegner.</summary>
    internal static Model UnitSphere()
    {
        if (Models.TryGetValue(UnitSphereName, out var cached))
            return cached;

        var model = LoadModelFromMesh(GenMeshSphere(1f, 20, 20));
        Models[UnitSphereName] = model;
        return model;
    }

    // ------------------------------------------------------- Lavet af koden

    /// <summary>
    /// Et ternet billede lavet af koden - ingen fil noedvendig. Gemmes under navnet,
    /// saa du bagefter kan bruge det som enhver anden tekstur:
    ///   Assets.Checkered("skak", Color.White, Color.Black);
    ///   Draw.Sprite("skak", Position, 40);
    /// </summary>
    public static Texture2D Checkered(string name, Color a, Color b, int cells = 8)
    {
        if (Textures.TryGetValue(name, out var cached))
            return cached;

        const int size = 256;
        var image = GenImageChecked(size, size, size / cells, size / cells, a, b);
        var texture = LoadTextureFromImage(image);
        UnloadImage(image);
        Textures[name] = texture;
        return texture;
    }

    /// <summary>
    /// En kugle med et billede paa, lavet af koden. Radius 1 - skru paa scale i Draw.Model.
    ///   Assets.Ball("bold", "skak");
    ///   Draw.Model("bold", Position, Vector3.UnitY, vinkel, 20);
    /// </summary>
    public static Model Ball(string name, string textureName)
    {
        if (Models.TryGetValue(name, out var cached))
            return cached;

        var model = LoadModelFromMesh(GenMeshSphere(1f, 24, 24));
        unsafe { SetMaterialTexture(ref model.Materials[0], MaterialMapIndex.Albedo, Texture(textureName)); }
        Models[name] = model;
        return model;
    }

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
        foreach (var shader in Shaders.Values) UnloadShader(shader);
        if (_missing != null) UnloadTexture(_missing.Value);

        Textures.Clear();
        Models.Clear();
        Sounds.Clear();
        Shaders.Clear();
        _missing = null;
    }
}
