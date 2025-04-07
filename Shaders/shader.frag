#version 460 core
out vec4 FragColor;

in vec2 TexCoord;
in vec3 FragPos;
in vec3 Normal;
in vec4 FragPosLightSpace;

uniform sampler2D texture0;
uniform sampler2D shadowMap;

uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientColor;
uniform vec3 viewPos;

float CalculateShadow(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    projCoords = projCoords * 0.5 + 0.5;
    float currentDepth = projCoords.z;

    if(currentDepth > 1.0)
        return 1.0;

    float bias = max(0.0015 * (1.0 - dot(normal, lightDir)), 0.00015);
    float shadow = 0.0;
    int pcfSamples = 4;
    float texelSize = 1.0 / textureSize(shadowMap, 0).x;

    for(int x = -pcfSamples/2; x < pcfSamples/2; ++x)
    {
        for(int y = -pcfSamples/2; y < pcfSamples/2; ++y)
        {
            float closestDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * texelSize).r;
            shadow += currentDepth - bias > closestDepth ? 0.0 : 1.0;
        }
    }
    shadow /= (pcfSamples * pcfSamples);

    return shadow;
}

void main()
{
    vec4 textureColor = texture(texture0, TexCoord);
    vec3 norm = normalize(Normal);

    float shadow = CalculateShadow(FragPosLightSpace, norm, lightDir);

    vec3 ambient = ambientColor * textureColor.rgb;

    float diffFactor = max(dot(norm, -lightDir), 0.0);
    vec3 diffuse = lightColor * diffFactor * shadow * textureColor.rgb;

    vec3 result = ambient + diffuse;
    FragColor = vec4(result, textureColor.a);
}