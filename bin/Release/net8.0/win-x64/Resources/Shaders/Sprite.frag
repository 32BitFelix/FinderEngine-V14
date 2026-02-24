#version 330 core

#extension GL_EXT_nonuniform_qualifier : enable

// The final result of the
// current fragment
out vec4 FragColor;


// The reference of the texture
flat in uvec2 bindlessTex;

// The color modifier of
// the texture
flat in vec4 color;

// The UV coordinate to
// get the color info from
in vec2 texCoord;


// Starting point of the
// fragment shader
void main()
{
    vec4 texColor = texture(sampler2D(bindlessTex), texCoord) * color;

    if(texColor.a == 0)
        discard;

    FragColor = texColor;
}