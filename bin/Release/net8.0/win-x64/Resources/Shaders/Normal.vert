#version 330 core

#extension GL_EXT_nonuniform_qualifier : enable


// The matrix that specifies
// the camera's position
uniform mat4 view;

// The matrix that specifies
// the camera's projection
uniform mat4 projection;


// The total amount of sprites
uniform int spriteAmount;


// The texture buffer to store
// bindless texture references
uniform usamplerBuffer bindlessTexBuffer;

// The texture buffer to store
// colour modifiers
uniform samplerBuffer colourModBuffer;

// The texture buffer to store
// model matrices
uniform samplerBuffer modelMatBuffer;


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
// colour modifier to the
// fragment shader
flat out vec4 colour;

// Relays the current
// UV coordinate to the
// fragment shader (with interpolation)
out vec2 texCoord;

// Relays the alpha tolerance
// to the fragments shader
out float tolerance;


// Starting point of the
// vertex shader
void main()
{
    // Save the texture coordinate
    texCoord = constCoord[gl_VertexID];


    // Evaluate the instance ID
    // of the current instance
    int instanceID;

    {
        int first = gl_InstanceID % spriteAmount;

        int second = gl_InstanceID - (first * 2) - 1;

        instanceID = gl_InstanceID >= spriteAmount ? second : first;
    }


    // Set the tolerance to
    // render the incoming fragments with
    tolerance = gl_InstanceID < spriteAmount ? 1.0f : 0.0f;


    // Sample the model matrix
    {    
        mat4 model;


        int index = instanceID * 4;

        model[0] = texelFetch(modelMatBuffer, index);

        index++;

        model[1] = texelFetch(modelMatBuffer, index);

        index++;

        model[2] = texelFetch(modelMatBuffer, index);

        index++;

        model[3] = texelFetch(modelMatBuffer, index);


        // Set the position of the
        // vertex to the screen
        gl_Position = vec4(constPos[gl_VertexID], 1.0) * transpose(model) * view * projection;
    }


    // Sample the colour modifier
    colour = texelFetch(colourModBuffer, instanceID);


    // Sample the bindless texture
    bindlessTex = texelFetch(bindlessTexBuffer, instanceID).xy;
}