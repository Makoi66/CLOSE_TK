#version 460 core
out vec4 FragColor;

in vec2 TexCoord;
in vec3 FragPos;
in vec3 Normal;

uniform sampler2D texture0;

uniform vec3 lightDir;
uniform vec3 lightColor;
uniform vec3 ambientColor;
uniform vec3 viewPos;

void main()
{
    vec4 textureColor = texture(texture0, TexCoord);

    vec3 ambient = ambientColor * textureColor.rgb;

    vec3 norm = normalize(Normal);
    float diffFactor = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = lightColor * diffFactor * textureColor.rgb;

    vec3 result = ambient + diffuse;

    FragColor = vec4(result, textureColor.a);
}