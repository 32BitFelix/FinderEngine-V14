
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.LECSSimple;
using Core.MemoryManagement;
using IO;
using IO.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;


namespace Core.Shimshek;


[System(false, false)]
public static unsafe class SpriteRenderSystem
{
    static SpriteRenderSystem()
    {
        // Define the element buffer object

        {
            byte[] indices =
            {
                0, 1, 2,
                0, 2, 3
            };


            EBO = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            fixed(byte* ptr = indices)
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length, (nint)ptr, BufferUsageHint.DynamicDraw);
        }


        // Define the vertex buffer object

        VBO = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

        GL.BufferData(BufferTarget.ArrayBuffer, blockSize, 0, BufferUsageHint.DynamicDraw);


        // Define the vertex array object

        VAO = GL.GenVertexArray();

        GL.BindVertexArray(VAO);


            // Define the pointer to the
            // bindless texture data

            GL.VertexAttribIPointer(0, 2, VertexAttribIntegerType.Int, blockSize, 0);

            GL.VertexAttribDivisor(0, 1);

            GL.EnableVertexAttribArray(0);


            // Define the pointer to the
            // color modifier

            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, blockSize, bindlessTexSize);

            GL.VertexAttribDivisor(1, 1);

            GL.EnableVertexAttribArray(1);


            // Define the pointer to the
            // model matrix

            for(byte i = 0; i < 4; i++)
            {
                GL.VertexAttribPointer(2 + i, 4, VertexAttribPointerType.Float, false, blockSize, bindlessTexSize + colorModSize + sizeof(float) * 4 * i);

                GL.VertexAttribDivisor(2 + i, 1);

                GL.EnableVertexAttribArray(2 + i);
            }


        // Define the program for the
        // sprite renderer

        SpriteProgram.Create("./Resources/Shaders/Normal.vert", "./Resources/Shaders/Sprite.frag");
    }


    public static int[] Init()
        => [Sprite.ComponentID, Transform.ComponentID];


    public static void PreRender(ArchetypeIterator* iter)    
    {
        
    }


    const int bindlessTexSize = sizeof(long);

    const int colorModSize = sizeof(byte) * 4;

    const int modelMatSize = sizeof(float) * 16;


    const int blockSize = bindlessTexSize + colorModSize + modelMatSize; 


    public static int VBO;

    private static int VBOLength;

    private static int spriteAmount;

    public static int VAO;

    public static int EBO;


    public static ProgramObj SpriteProgram;


    public static void Render(ArchetypeIterator* iter)    
    {
        for(int i = 0; i < iter->Length(); i++)
        {
            if(iter->GetEntityID(i) < 2)
                continue;


            Sprite* curSprite = (Sprite*)iter->GetComponent(i, Sprite.ComponentID);

            Transform* curTran = (Transform*)iter->GetComponent(i, Transform.ComponentID);

            Matrix4 modelMat = curTran->GetModelMatrix(iter->GetEntityID(i)); 


            // Resize the vertex buffer,
            // if there is some more space needed

            if(VBOLength <= spriteAmount)
            {
                VBOLength = spriteAmount + 1;

                GL.BufferData(BufferTarget.ArrayBuffer, blockSize * VBOLength, 0, BufferUsageHint.DynamicDraw);
            }


            {
                void* ptr = (void*)GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);


                long* bindless = (long*)((nint)ptr + spriteAmount * blockSize);

                *bindless = curSprite->Texture.BTO;


                int* RGBA = (int*)((nint)ptr + spriteAmount * blockSize + bindlessTexSize);

                *RGBA = curSprite->RGBA;


                Matrix4* mat = (Matrix4*)((nint)ptr + spriteAmount * blockSize + bindlessTexSize + colorModSize); 

                *mat = modelMat;


                GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            }


            spriteAmount++;
        }


        if(!iter->IsLast)
            return;

        CameraSystem.AddDispatch(&spriteDispatch);
    }


    private static void spriteDispatch(int cameraID)
    {
        Transform* camTran = (Transform*)Finder.GetComponent(cameraID, Transform.ComponentID);

        Camera* cam = (Camera*)Finder.GetComponent(cameraID, Camera.ComponentID);


        SpriteProgram.SetUniformMatrix4("view", camTran->GetViewMatrix(cameraID));

        SpriteProgram.SetUniformMatrix4("projection", cam->GetProjection());


        GL.BindVertexArray(VAO);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

        SpriteProgram.Use();

        GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedByte, 0, spriteAmount);

        spriteAmount ^= spriteAmount;
    }
}


// The system handles all jobs
// related to rendering stuff
// directly to the viewport

[System(false, false)]
public static unsafe class CameraSystem
{
    static CameraSystem()
    {
        dispatches = CompactArray.Create<nuint>(0);


    }


    // The initialiser of the system.
    // It shows the engine, what archetypes
    // the system processes

    public static int[] Init()
        => [Camera.ComponentID, Transform.ComponentID];


    // The list of dispatches
    // to process in the current iteration
    // (Compact array)

    private static nuint* dispatches;

    public static void AddDispatch(delegate*<int, void> dispatch)
    {
        // Try to find a free index to
        // save the dispatch at

        for(int i = CompactArray.Length(dispatches) - 1; i > -1; i--)
        {
            if(dispatches[i] != 0)
                continue;

            dispatches[i] = (nuint)dispatch;

            return;
        }

        // Allocate a new space to
        // save the dispatch at

        fixed(nuint** ptr = &dispatches)
            CompactArray.Resize(ptr, CompactArray.Length(dispatches) + 1);

        dispatches[CompactArray.Length(dispatches) - 1] = (nuint)dispatch;
    }


    public static void Render(ArchetypeIterator* iter)
    {
        // Iterate through each valid camera

        for(int i = 0; i < iter->Length(); i++)
        {

            // Iterate through each dispatch

            for(int d = CompactArray.Length(dispatches) - 1; d > -1; d--)
            {
                if(dispatches[d] == 0)
                    continue;

                ((delegate*<int, void>)dispatches[d])(iter->GetEntityID(i));
            }
        }


        // Clear the dispatches

        for(int d = CompactArray.Length(dispatches) - 1; d > -1; d--)
        {
            if(dispatches[d] == 0)
                continue;

            dispatches[d] ^= dispatches[d];
        }
    }


    // The list of post dispatches
    // to process in the current iteration

    private static nuint* postDispatches;

    public static void AddPostDispatch(delegate*<int, void> dispatch)
    {



    }


    public static void PostRender(ArchetypeIterator* iter)
    {



    }
}


[Component]
public unsafe struct Sprite
{
    public static int ComponentID;

    public TextureObj Texture;

    public int RGBA;
}


// The camera component is used
// on objects, that the player
// is supposed to see from

[Component]
public unsafe struct Camera
{
    public static int ComponentID;


    // Indicates how narrow the
    // camera should be

    public float FieldOfView
    {
        get => _fov * 180 / MathHelper.Pi;

        set
        {
            float angle = MathHelper.Clamp(value, 1, 120);

            _fov = angle * (MathHelper.Pi / 180);
        }
    }

    public float _fov;


    // The nearest and farthest
    // objects can be to the camera

    public float NearClip, FarClip;


    // The size of the camera's view

    public float ProjectionSize;


    // Indicates, if the camera's
    // projection is orthographic

    public bool IsOrthographic;
}


public unsafe static class CameraExt
{
    public static Matrix4 GetProjection(this ref Camera cam)
    {
        Matrix4 result =
            cam.IsOrthographic ? Matrix4.CreateOrthographic(cam.ProjectionSize * WindowManager.AspectRatio, cam.ProjectionSize, cam.NearClip, cam.FarClip) :
                Matrix4.CreatePerspectiveFieldOfView(cam._fov, WindowManager.AspectRatio, cam.NearClip, cam.FarClip);

        return result;
    }

}


// A wrapper for OpenGL's texture.
// Includes both the texture buffer
// reference along with the bindless
// texture reference

public struct TextureObj
{
    // Reference to the buffer
    // holding the texture's information

    public int TBO;


    // The bindless texture reference
    // to the texture's buffer

    public long BTO;
}

// Extension method for
// texture objects

public unsafe static class TextureObjExt
{
    // Creates a texture object from the
    // given data

    public static void Create(this ref TextureObj tObj, byte* Data, int Width, int Height, bool isGreyScale)
    {
        // Create the texture buffer
        // and bind to it

        tObj.TBO = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, tObj.TBO);


        // Upload the texture data

        GL.TexImage2D(TextureTarget.Texture2D, 0, isGreyScale ? PixelInternalFormat.R8 : PixelInternalFormat.Rgba,
            Width, Height, 0, isGreyScale ? PixelFormat.Red : PixelFormat.Rgba, PixelType.UnsignedByte, (nint)Data);


        // Set up the texture parameters

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


        // Generate the bindless reference
        // to the texture and make it resident

        tObj.BTO = GL.Arb.GetTextureHandle(tObj.TBO);

        GL.Arb.MakeTextureHandleResident(tObj.BTO);
    }


    public static void Create(this ref TextureObj tObj, string path, bool isGreyScale)
    {
        // Create the texture buffer
        // and bind to it

        tObj.TBO = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, tObj.TBO);


        // Load the image

        StbImage.stbi_set_flip_vertically_on_load(1);

        ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);


        // Upload the texture data

        GL.TexImage2D(TextureTarget.Texture2D, 0, isGreyScale ? PixelInternalFormat.R8 : PixelInternalFormat.Rgba,
            image.Width, image.Height, 0, isGreyScale ? PixelFormat.Red : PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);


        // Set up the texture parameters

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


        // Generate the bindless reference
        // to the texture and make it resident

        tObj.BTO = GL.Arb.GetTextureHandle(tObj.TBO);

        GL.Arb.MakeTextureHandleResident(tObj.BTO);
    }


    // Deletes the given texture object

    public static void Delete(this ref TextureObj tObj)
    {
        // Delete the bindless texture reference

        GL.Arb.MakeTextureHandleNonResident(tObj.BTO);


        // Delete the texture buffer

        GL.DeleteTexture(tObj.TBO);
    }


    // Binds the given
    // texture object to the
    // specified texture unit

    public static void Use(this ref TextureObj tObj, TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);

        GL.BindTexture(TextureTarget.Texture2D, tObj.TBO);
    }
}


// A wrapper for OpenGL's program.
// Simplifies their use with a focus
// on performance

public struct ProgramObj
{
    // Represents the reference to the
    // program stored in the gpu

    public int Handle;
}

// Extension methods for the
// program object

public unsafe static class ProgramObjExt
{
    // Creates a new program object from the
    // given paths to the shader codes

    public static void Create(this ref ProgramObj pObj, string vertexShaderPath, string fragmentShaderPath)
    {
        // Create the vertex shader and load
        // it's source code

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);

        {
            string source = File.ReadAllText(vertexShaderPath);

            GL.ShaderSource(vertexShader, source);
        }

        // Compile the vertex shader.
        // Throw an error, if it doesn't
        // work out

        GL.CompileShader(vertexShader);

        {
            int compileStatus = 0;

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, &compileStatus);

            if(compileStatus == 0)
                Logger.LogError(GL.GetShaderInfoLog(vertexShader));
        }


        // Create the fragment shader and load
        // it's source code

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);

        {
            string source = File.ReadAllText(fragmentShaderPath);

            GL.ShaderSource(fragmentShader, source);
        }

        // Compile the fragment shader.
        // Throw an error, if it doesn't
        // work out

        GL.CompileShader(fragmentShader);

        {
            int compileStatus = 0;

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, &compileStatus);

            if(compileStatus == 0)
                Logger.LogError(GL.GetShaderInfoLog(fragmentShader));
        }


        // Create the program and attach
        // the given shaders to it.
        // Throw an error, if it doesn't
        // work out

        pObj.Handle = GL.CreateProgram();

        GL.AttachShader(pObj.Handle, vertexShader);

        GL.AttachShader(pObj.Handle, fragmentShader);

        GL.LinkProgram(pObj.Handle);

        int linkStatus = 0;

        GL.GetProgram(pObj.Handle, GetProgramParameterName.LinkStatus, &linkStatus);

        if(linkStatus == 0)
            Logger.LogError(GL.GetProgramInfoLog(pObj.Handle));


        // Do some final cleanup

        GL.DetachShader(pObj.Handle, vertexShader);

        GL.DetachShader(pObj.Handle, fragmentShader);

        GL.DeleteShader(vertexShader);

        GL.DeleteShader(fragmentShader);
    }


    // Deletes the given program.
    // Throws an error, if the
    // deleting failed

    public static void Delete(this ProgramObj pObj)
    {
        GL.DeleteProgram(pObj.Handle);

        int deleteStatus = 0;

        GL.GetProgram(pObj.Handle, GetProgramParameterName.DeleteStatus, &deleteStatus);

        if(deleteStatus == 0)
            Logger.LogError(GL.GetProgramInfoLog(pObj.Handle));
    }


    // Binds the given program

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Use(this ProgramObj pObj)
        => GL.UseProgram(pObj.Handle);


    // Sets the uniform matrix with the given name
    // in the given program

    public static void SetUniformMatrix4(this ProgramObj pObj, string uniformName, Matrix4 value)
    {
        int location = GL.GetUniformLocation(pObj.Handle, uniformName);

        GL.UniformMatrix4(location, true, ref value);
    }


    // Sets the uniform vector with the given name
    // in the given program

    public static void SetUniformVector4(this ProgramObj pObj, string uniformName, Vector4 value)
    {
        int location = GL.GetUniformLocation(pObj.Handle, uniformName);

        GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }


    // Sets the uniform integer with the given name
    // in the given program

    public static void SetUniformInt(this ProgramObj pObj, string uniformName, int value)
    {
        int location = GL.GetUniformLocation(pObj.Handle, uniformName);

        GL.Uniform1(location, value);
    }
}
