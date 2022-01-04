#version 330 core

out vec4 FragColor;

in vec2 TexCoord;

uniform sampler2D AlbedoTexture;

void main() {
    FragColor = texture(AlbedoTexture, TexCoord);
}