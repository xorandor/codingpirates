#version 330

// Kraken: lys. For hvert pixel: hvor meget peger fladen mod lyset?
//   - ambient   er grundlyset, som rammer alt lige meget (saa skyggesiden ikke er kulsort)
//   - diffuse   er "peger mod lyset": normalen prikket med retningen til lyset
//   - specular  er det lille blanke glimt, hvor lyset spejler sig ret ind i kameraet
//
// Op til 4 lyskilder. type 0 = retningslys (som solen), type 1 = punktlys (som en paere).

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

    finalColor = vec4(base.rgb * (ambient.rgb + lit) + glint, base.a);
}
