#version 450

layout(location = 0) in vec3 vtin_position;
layout(location = 1) in vec3 vtin_normal;
layout(location = 2) in vec2 vtin_texture;

layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out vec3 fsin_worldPos;
layout(location = 2) out vec3 fsin_normal;

layout(set = 0, binding = 0) uniform ProjectionBuffer{ mat4 projection;};
layout(set = 0, binding = 1) uniform ViewBuffer { mat4 view; };
layout(set = 1, binding = 0) uniform ModelBuffer { mat4 model; };

void main()
{
    fsin_texCoords = vtin_texture;
    fsin_worldPos = vec3(model * vec4(vtin_position, 1.0));
    fsin_normal = mat3(model) * vtin_normal;   

    gl_Position =  projection * view * vec4(fsin_worldPos, 1.0);
}