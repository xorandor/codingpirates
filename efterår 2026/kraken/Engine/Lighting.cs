using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Kraken;

/// <summary>
/// En lyskilde. Laeg den i context.Lighting.Lights - eller brug komponenten Light,
/// som goer det for dig og kan flytte sig.
/// </summary>
public class LightSource
{
    public Vector3 Position { get; set; } = new(0, 0, 500);

    /// <summary>Kun for retningslys: det punkt lyset peger mod.</summary>
    public Vector3 Target { get; set; }

    public Color Color { get; set; } = Color.White;

    /// <summary>1 = fuld styrke. Over 1 blaeser farverne ud, under 1 daemper.</summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>True = som solen: samme retning overalt. False = som en paere: fra et punkt.</summary>
    public bool Directional { get; set; }
}

/// <summary>
/// Lyset i verden. Uden lyskilder tegnes alt som foer - fladt, uden skyggesider.
/// Saa snart der er mindst een LightSource, tegnes kugler, terninger og modeller med lys.
/// Sprites og linjer roeres ikke - de er flade billeder og skal ikke have skyggesider.
///
///   game.Add(new Light { Position = new(-300, 300, 600) });
///   game.Lighting.Ambient = new Color(60, 60, 80, 255);   // grundlys, saa skyggen ikke er sort
/// </summary>
public class Lighting
{
    private const int MaxLights = 4;

    private Shader _shader;
    private bool _loaded;
    private Shader _defaultShader;

    private int _locAmbient, _locViewPos, _locShininess, _locMatModel, _locMatNormal;
    private readonly int[] _locEnabled = new int[MaxLights];
    private readonly int[] _locType = new int[MaxLights];
    private readonly int[] _locPosition = new int[MaxLights];
    private readonly int[] _locTarget = new int[MaxLights];
    private readonly int[] _locColor = new int[MaxLights];

    /// <summary>Lyskilderne. Kun de foerste 4 bruges.</summary>
    public List<LightSource> Lights { get; } = [];

    /// <summary>Grundlyset, der rammer alt lige meget. Moerkere = dybere skygger.</summary>
    public Color Ambient { get; set; } = new(70, 70, 70, 255);

    /// <summary>Hvor skarpt det blanke glimt er. 0 slaar det fra. 16 er blodt, 128 er som poleret metal.</summary>
    public float Shininess { get; set; } = 24f;

    /// <summary>True naar der er mindst een lyskilde - og lyset dermed er slaaet til.</summary>
    public bool Active => Lights.Count > 0;

    // ---------------------------------------------------------------- Motoren

    /// <summary>Kaldes af motoren lige efter BeginMode3D. Saetter uniforms og slaar shaderen til.</summary>
    internal void Begin(Camera3D camera)
    {
        _viewPos = camera.Position;
        if (!Active) return;
        EnsureLoaded();

        SetShaderValue(_shader, _locViewPos, camera.Position, ShaderUniformDataType.Vec3);
        SetShaderValue(_shader, _locAmbient, ToVec4(Ambient, 1f), ShaderUniformDataType.Vec4);
        SetShaderValue(_shader, _locShininess, Shininess, ShaderUniformDataType.Float);

        for (int i = 0; i < MaxLights; i++)
        {
            bool on = i < Lights.Count;
            SetShaderValue(_shader, _locEnabled[i], on ? 1 : 0, ShaderUniformDataType.Int);
            if (!on) continue;

            var light = Lights[i];
            SetShaderValue(_shader, _locType[i], light.Directional ? 0 : 1, ShaderUniformDataType.Int);
            SetShaderValue(_shader, _locPosition[i], light.Position, ShaderUniformDataType.Vec3);
            SetShaderValue(_shader, _locTarget[i], light.Target, ShaderUniformDataType.Vec3);
            SetShaderValue(_shader, _locColor[i], ToVec4(light.Color, light.Intensity), ShaderUniformDataType.Vec4);
        }

        ResetModelMatrix();
        BeginShaderMode(_shader);
    }

    /// <summary>Kaldes af motoren lige foer EndMode3D.</summary>
    internal void End()
    {
        if (Active) EndShaderMode();
    }

    /// <summary>
    /// Goer en model klar til at blive tegnet: med lys-shaderen naar lyset er til, ellers
    /// med standard-shaderen. Skal kaldes foer hver DrawModel, fordi modeller ikke gaar
    /// gennem BeginShaderMode - de har deres egen shader i materialet.
    /// </summary>
    internal unsafe void Prepare(ref Model model)
    {
        if (_defaultShader.Id == 0) _defaultShader = model.Materials[0].Shader;

        if (Active)
        {
            EnsureLoaded();
            model.Materials[0].Shader = _shader;
        }
        else
        {
            model.Materials[0].Shader = _defaultShader;
        }
    }

    /// <summary>
    /// Kaldes efter hver DrawModel. raylib saetter matModel/matNormal til modellens matrix
    /// og lader dem staa - men terninger og andre batch-figurer tegnes i world-koordinater
    /// og skal have identitets-matricen. Ellers bliver de lyst forkert efter en model.
    /// </summary>
    internal void AfterModel()
    {
        if (Active) ResetModelMatrix();
    }

    /// <summary>Tegn noget uden lys, selv om lyset er slaaet til - sprites, linjer, tekst i verden.</summary>
    internal void Unlit(Action draw) => With(null, draw);

    /// <summary>
    /// Tegn noget med en bestemt shader (eller ingen), og stil bagefter tilbage til det lyset
    /// ellers ville have brugt. Det er den Draw.Shaded og Draw.Sprite bygger paa.
    /// </summary>
    internal void With(Shader? shader, Action draw)
    {
        if (shader is { } own) BeginShaderMode(own);
        else if (Active) EndShaderMode();

        draw();

        if (Active) BeginShaderMode(_shader);
        else if (shader != null) EndShaderMode();
    }

    /// <summary>
    /// Saetter lysets uniforms paa en anden shader, der bruger de samme navne som lys.fs
    /// (lights[], ambient, viewPos, shininess). Saa kan din egen shader faa lys med gratis.
    /// Er lyset slaaet fra, saettes ambient til hvidt, saa din shader tegner som uden lys.
    /// </summary>
    public void Apply(Shader shader)
    {
        SetShaderValue(shader, GetShaderLocation(shader, "viewPos"), _viewPos, ShaderUniformDataType.Vec3);
        SetShaderValue(shader, GetShaderLocation(shader, "shininess"), Shininess, ShaderUniformDataType.Float);
        SetShaderValue(shader, GetShaderLocation(shader, "ambient"),
            Active ? ToVec4(Ambient, 1f) : new Vector4(1f, 1f, 1f, 1f), ShaderUniformDataType.Vec4);

        for (int i = 0; i < MaxLights; i++)
        {
            bool on = Active && i < Lights.Count;
            SetShaderValue(shader, GetShaderLocation(shader, $"lights[{i}].enabled"), on ? 1 : 0, ShaderUniformDataType.Int);
            if (!on) continue;

            var light = Lights[i];
            SetShaderValue(shader, GetShaderLocation(shader, $"lights[{i}].type"), light.Directional ? 0 : 1, ShaderUniformDataType.Int);
            SetShaderValue(shader, GetShaderLocation(shader, $"lights[{i}].position"), light.Position, ShaderUniformDataType.Vec3);
            SetShaderValue(shader, GetShaderLocation(shader, $"lights[{i}].target"), light.Target, ShaderUniformDataType.Vec3);
            SetShaderValue(shader, GetShaderLocation(shader, $"lights[{i}].color"), ToVec4(light.Color, light.Intensity), ShaderUniformDataType.Vec4);
        }

        // Terninger og andre batch-figurer ligger i world-koordinater - se AfterModel.
        SetShaderValueMatrix(shader, GetShaderLocation(shader, "matModel"), Matrix4x4.Identity);
        SetShaderValueMatrix(shader, GetShaderLocation(shader, "matNormal"), Matrix4x4.Identity);
    }

    private Vector3 _viewPos;

    private void ResetModelMatrix()
    {
        SetShaderValueMatrix(_shader, _locMatModel, Matrix4x4.Identity);
        SetShaderValueMatrix(_shader, _locMatNormal, Matrix4x4.Identity);
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;

        _shader = Assets.Shader("shaders/lys.vs", "shaders/lys.fs");
        _locAmbient = GetShaderLocation(_shader, "ambient");
        _locViewPos = GetShaderLocation(_shader, "viewPos");
        _locShininess = GetShaderLocation(_shader, "shininess");
        _locMatModel = GetShaderLocation(_shader, "matModel");
        _locMatNormal = GetShaderLocation(_shader, "matNormal");

        for (int i = 0; i < MaxLights; i++)
        {
            _locEnabled[i] = GetShaderLocation(_shader, $"lights[{i}].enabled");
            _locType[i] = GetShaderLocation(_shader, $"lights[{i}].type");
            _locPosition[i] = GetShaderLocation(_shader, $"lights[{i}].position");
            _locTarget[i] = GetShaderLocation(_shader, $"lights[{i}].target");
            _locColor[i] = GetShaderLocation(_shader, $"lights[{i}].color");
        }

        _loaded = true;
    }

    private static Vector4 ToVec4(Color color, float scale)
        => new(color.R / 255f * scale, color.G / 255f * scale, color.B / 255f * scale, 1f);
}
