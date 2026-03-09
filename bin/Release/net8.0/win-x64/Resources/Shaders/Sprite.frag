#version 330 core

#extension GL_ARB_bindless_texture : enable
#extension GL_EXT_nonuniform_qualifier : enable

// The final result of the
// current fragment
out vec4 FragColour;


// The reference of the texture
flat in uvec2 bindlessTex;

// The colour modifier of
// the texture
flat in vec4 colour;

// The UV coordinate to
// get the color info from
in vec2 texCoord;

in float tolerance;


// Starting point of the
// fragment shader
void main()
{
    vec4 texColour = texture(sampler2D(bindlessTex), texCoord) * colour;

    //if(texColour.a == 0)
    //    discard;

    //if(texColour.a < tolerance)
    //    discard;

    FragColour = texColour;

    //FragColour = vec4(1.0f, 1.0f, 1.0f, 1.0f);

    //FragColour = colour;
}