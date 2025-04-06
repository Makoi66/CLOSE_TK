#version 460 core
out vec4 FragColor;

in vec3 TexCoords;

uniform samplerCube skybox;
uniform float brightnessFactor;

const float minSkyBrightness = 0.15;

void main()
{
    vec4 texColor = texture(skybox, TexCoords);
    float actualBrightness = mix(minSkyBrightness, 1.0, brightnessFactor);
    FragColor = vec4(texColor.rgb * actualBrightness, texColor.a);
}