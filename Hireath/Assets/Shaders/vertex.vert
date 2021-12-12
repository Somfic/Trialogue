#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 Texture;

layout(location = 0) out vec4 fsin_Color;
layout(location = 1) out vec3 fsin_Normal;
layout(location = 2) out vec3 fsin_Position;
layout(location = 3) out vec3 fsin_CameraPosition;

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 0, binding = 2) uniform PositionBuffer
{
    vec3 CameraPosition;
};

layout(set = 1, binding = 0) uniform WorldBuffer
{
    mat4 World;
};

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    
    fsin_Normal = Normal;
    fsin_Position = Position;
    fsin_Color = vec4(CameraPosition, 1);
    fsin_CameraPosition = CameraPosition;
}