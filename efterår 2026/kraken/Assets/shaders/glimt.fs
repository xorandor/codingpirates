#version 330

// Kraken: lys + et glimt der loeber hen over tingen. Det er lys.fs med ti linjer ekstra
// nederst. Bruges saadan fra Render():
//
//   var shader = Assets.Shader("shaders/lys.vs", "shaders/glimt.fs");
//   Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "time"), (float)Raylib.GetTime(), ShaderUniformDataType.Float);
//   Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "center"), Position, ShaderUniformDataType.Vec3);
//   Raylib.SetShaderValue(shader, Raylib.GetShaderLocation(shader, "size"), new Vector2(18, 110), ShaderUniformDataType.Vec2);
//   Draw.Shaded(shader, () => Draw.Cube(Position, ...));

in vec3 fragPosition;
in vec2 fragTexCoord;
in vec4 fragColor;
in vec3 fragNormal;

uniform sampler2D texture0;
uniform vec4 colDiffuse;

#define MAX_LIGHTS 4

struct Light {
    int enabled;
    int type;
    vec3 position;
    vec3 target;
    vec4 color;
};

uniform Light lights[MAX_LIGHTS];
uniform vec4 ambient;
uniform vec3 viewPos;
uniform float shininess;

// Glimtets egne:
uniform float time;      // sekunder, fx GetTime()
uniform vec3 center;     // tingens midte i verden
uniform vec2 size;       // tingens bredde og hoejde
uniform float period;    // sekunder mellem to glimt (0 = brug 2.2)

out vec4 finalColor;

void main()
{
    vec4 texel = texture(texture0, fragTexCoord);
    vec4 base = texel * colDiffuse * fragColor;

    vec3 normal = normalize(fragNormal);
    vec3 toCamera = normalize(viewPos - fragPosition);

    vec3 lit = vec3(0.0);
    vec3 glint = vec3(0.0);

    for (int i = 0; i < MAX_LIGHTS; i++)
    {
        if (lights[i].enabled != 1) continue;

        vec3 toLight = (lights[i].type == 0)
            ? normalize(lights[i].position - lights[i].target)
            : normalize(lights[i].position - fragPosition);

        float facing = max(dot(normal, toLight), 0.0);
        lit += lights[i].color.rgb * facing;

        if (facing > 0.0 && shininess > 0.0)
        {
            float spec = pow(max(dot(toCamera, reflect(-toLight, normal)), 0.0), shininess);
            glint += lights[i].color.rgb * spec * 0.5;
        }
    }

    vec3 color = base.rgb * (ambient.rgb + lit) + glint;

    // Glimtet: et skraat baand der loeber fra bund til top og saa venter lidt.
    float p = period > 0.0 ? period : 2.2;
    float sweep = fract(time / p) * 2.6 - 1.3;                                  // -1.3 .. 1.3
    float along = (fragPosition.y - center.y) / (size.y * 0.5)
                + (fragPosition.x - center.x) / (size.x * 0.5) * 0.25;         // skraat
    float glow = smoothstep(0.16, 0.0, abs(along - sweep));
    color += vec3(glow * 0.85);

    finalColor = vec4(color, base.a);
}
