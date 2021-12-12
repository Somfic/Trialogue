#version 450

layout(location = 0) in vec4 fsin_Color;
layout(location = 1) in vec3 fsin_Normal;
layout(location = 2) in vec3 fsin_Position;
layout(location = 3) in vec3 fsin_CameraPosition;

layout(location = 0) out vec4 fsout_Color;

void main()
{
    vec3 lightPos = vec3(100, 0, -100);
    vec3 lightColor = vec3(1, 1, 1);
    vec3 objectColor = vec3(1, 1, 1);

    float specularStrength = 0.5;

    // ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * lightColor;

    // diffuse 
    vec3 norm = normalize(fsin_Normal);
    vec3 lightDir = normalize(lightPos - fsin_Position);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    // specular
    vec3 viewDir = normalize(fsin_CameraPosition - fsin_Position);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 256);
    vec3 specular = specularStrength * spec * lightColor;

    vec3 result = (ambient + diffuse + specular) * objectColor;
    fsout_Color = vec4(result, 1.0);
}