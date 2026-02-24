#version 330 core

#extension GL_EXT_nonuniform_qualifier : enable

// The reference to the
// sprite's texture
layout (location = 0) in uvec2 aBindlessTex;

// The color modifiers for
// the sprite's texture
layout (location = 1) in vec4 aColor;

// The matrix that specifies
// teh sprites position in
// world space
layout (location = 2) in mat4 model;


// The matrix that specifies
// the camera's position
uniform mat4 view;

// The matrix that specifies
// the camera's projection
uniform mat4 projection;


// The vertex positions for each vertex
// of the sprite
const vec3 constPos[4] = vec3[](vec3(-1.0f, -1.0f, 0.0f),
                        vec3(1.0f, -1.0f, 0.0f),
                        vec3(1.0f, 1.0f, 0.0f),
                        vec3(-1.0f, 1.0f, 0.0f));

// The UV coordinates for the
// sprite's texture
const vec2 constCoord[4] = vec2[](vec2(0.0f, 0.0f),
                        vec2(1.0f, 0.0f),
                        vec2(1.0f, 1.0f),
                        vec2(0.0f, 1.0f));


// Relays the texture
// reference to the
// fragment shader
flat out uvec2 bindlessTex;

// Relays the texture's
// color modifier to the
// fragment shader
flat out vec4 color;

// Relays the current
// UV coordinate to the
// fragment shader
out vec2 texCoord;


// Starting point of the
// vertex shader
void main()
{
    // Set the position of the
    // vertex to the screen

    gl_Position = vec4(constPos[gl_VertexID], 1.0) * transpose(model) * view * projection;


    // Set the values, that
    // will be relayed to the
    // fragment shader

    texCoord = constCoord[gl_VertexID];

    color = aColor;

    bindlessTex = aBindlessTex;
}