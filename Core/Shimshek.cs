
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.Algorithms;
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
        // Define the program for the
        // sprite renderer

        SpriteProgram.Create("./Resources/Shaders/Normal.vert", "./Resources/Shaders/Sprite.frag");

        SpriteProgram.Use();

        // Set the units of the uniform samplers

        SpriteProgram.SetUniformInt("bindlessTexBuffer", 0);

        SpriteProgram.SetUniformInt("colourModBuffer", 1);

        SpriteProgram.SetUniformInt("modelMatBuffer", 2);


        // Create the texture buffer, that'll
        // hold the bindless texture references

        bindlessTexRefs.Create(SizedInternalFormat.Rg32ui, null, 0);


        // Create the texture buffer, that'll
        // hold the color modifiers

        colorMods.Create(SizedInternalFormat.Rgba8, null, 0);


        // Create the texture buffer, that'll
        // hold the model matrices

        modelMatrices.Create(SizedInternalFormat.Rgba32f, null, 0);


        // Generate the dummy VAO

        VAO = GL.GenVertexArray();


        // Create the element buffer

        byte[] indices =
        {
            0, 1, 2,
            0, 2, 3
        };

        EBO = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

        fixed(byte* ptr = indices)
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length, (nint)ptr, BufferUsageHint.DynamicDraw);

        
        // Create the array, that'll
        // hold the entity-points

        eP = CompactArray.Create<EntityPoint>(1);
    }


    public static int[] Init()
        => [Sprite.ComponentID, Transform.ComponentID];


    public static void PreRender(ArchetypeIterator* iter)    
    {
        
    }


    // The shader program to render the sprites with

    public static ProgramObj SpriteProgram;


    // The buffers to store the information
    // of the sprites

    private static TextureBufferObj bindlessTexRefs;

    private static TextureBufferObj colorMods;

    private static TextureBufferObj modelMatrices;


    // The persistently mapped buffer
    // references of the texture buffers

    private static long* bindlessTexPMB;

    private static int* colorModPMB;

    private static Matrix4* modelMatPMB;


    // The buffers containing the clues
    // for the draw call

    private static int EBO, VAO;


    // The list holding the currently
    // valid sprites

    private static EntityPoint* eP;

    private static int validSprites;


    public static void Render(ArchetypeIterator* iter)    
    {
        for(int i = 0; i < iter->Length(); i++)
        {
            // Skip to the next iteration,
            // if the current entity is invalid

            if(iter->GetEntityID(i) < 2)
                continue;


            // Increment the counter of valid
            // sprites and resize the entity-point
            // array, if it doesn't have space
            // to fit the new sprite

            validSprites++;
            
            if(CompactArray.Length(eP) <= validSprites)
            {
                // Resize the array that holds
                // the entity IDs of the valid sprites

                fixed(EntityPoint** ptr = &eP)
                    CompactArray.Resize(ptr, CompactArray.Length(eP) * 2);


                // Recreate the texture buffer, that
                // stores the bindless textures

                GL.DeleteBuffer(bindlessTexRefs.BO);

                bindlessTexRefs.BO = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.TextureBuffer, bindlessTexRefs.BO);

                GL.BufferStorage(BufferTarget.TextureBuffer, CompactArray.Length(eP) * sizeof(long), 0, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.ClientStorageBit);

                GL.BindTexture(TextureTarget.TextureBuffer, bindlessTexRefs.TBO);

                GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.Rg32ui, bindlessTexRefs.BO);

                bindlessTexPMB = (long*)GL.MapBufferRange(BufferTarget.TextureBuffer, 0, CompactArray.Length(eP) * sizeof(long), MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit);


                // Recreate the texture buffer, that
                // stores the color modifiers

                GL.DeleteBuffer(colorMods.BO);

                colorMods.BO = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.TextureBuffer, colorMods.BO);

                GL.BufferStorage(BufferTarget.TextureBuffer, CompactArray.Length(eP) * sizeof(int), 0, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.ClientStorageBit);

                GL.BindTexture(TextureTarget.TextureBuffer, colorMods.TBO);

                GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.Rgba8, colorMods.BO);

                colorModPMB = (int*)GL.MapBufferRange(BufferTarget.TextureBuffer, 0, CompactArray.Length(eP) * sizeof(int), MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit);


                // Recreate the texture buffer, that
                // stores the model matrices

                GL.DeleteBuffer(modelMatrices.BO);

                modelMatrices.BO = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.TextureBuffer, modelMatrices.BO);

                GL.BufferStorage(BufferTarget.TextureBuffer, CompactArray.Length(eP) * sizeof(Matrix4), 0, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.ClientStorageBit);

                GL.BindTexture(TextureTarget.TextureBuffer, modelMatrices.TBO);

                GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.Rgba32f, modelMatrices.BO);

                modelMatPMB = (Matrix4*)GL.MapBufferRange(BufferTarget.TextureBuffer, 0, CompactArray.Length(eP) * sizeof(Matrix4), MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit);
            }


            // Save the entity ID of the new sprite

            eP[validSprites - 1].Entity = iter->GetEntityID(i);
        }


        // Add the sprite dispatch to the
        // camera system's dispatch list,
        // if this is the last iteration,
        // or if there even are sprites
        // to render

        if(!iter->IsLast || (validSprites == 0))
            return;


        CameraSystem.AddDispatch(&spriteDispatch);

        CameraSystem.AddPostDispatch(&spritePostDispatch);
    }


    private static void spriteDispatch(int cameraID)
    {
        // Get the translation of the current camera

        Transform* camTran = (Transform*)Finder.GetComponent(cameraID, Transform.ComponentID);

        Camera* cam = (Camera*)Finder.GetComponent(cameraID, Camera.ComponentID);

        Vector4 camTranslation = camTran->GetGlobalTranslation(cameraID);


        // Use the shader program

        SpriteProgram.Use();


        // Set the uniforms related to the camera

        SpriteProgram.SetUniformMatrix4("view", camTran->GetViewMatrix(cameraID));

        SpriteProgram.SetUniformMatrix4("projection", cam->GetProjection());


        // Set the uniform, that stores
        // the amount of sprites to render

        SpriteProgram.SetUniformInt("spriteAmount", validSprites - 1);


        // Assign the texture buffers
        // to their respective units

        bindlessTexRefs.Use(TextureUnit.Texture0);

        colorMods.Use(TextureUnit.Texture1);

        modelMatrices.Use(TextureUnit.Texture2);


        // Saturate the entity-point array with the most
        // necessary information, if the GPU isn't busy

        for(int i = validSprites - 1; i > -1; i--)
        {
            // Get the global translation
            // of the current sprite

            Transform* tran = (Transform*)Finder.GetComponent(eP[i].Entity, Transform.ComponentID);

            Vector4 spriteTranslation = tran->GetGlobalTranslation(eP[i].Entity);


            // Get the difference between the
            // sprite and the camera and save
            // the length to the entity-point

            Vector4 diff = spriteTranslation - camTranslation;

            eP[i].Point = diff.Length;
        }


        // Sort the sprites based on
        // their distance to the camera

        Sorting.RadixSort(eP, validSprites);


        // Saturate the texture buffers through the
        // persistently mapped buffer references

        uploadToBuffers();


        // Bind the EBO and VAO
        // and make a draw call

        GL.BindVertexArray(VAO);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

        GL.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedByte, 0, validSprites << 1 - 1);  
    }

        // Responsible for updating
        // the data of the texture buffers

        private static void uploadToBuffers()
        {
            /*// Evaluate the amount of currently
            // free threads and the amount of
            // elements each can process

            int threadCount = JobCenter.CountFreeThreads();

            int elementsPerThread = validSprites / threadCount;


            // Set the overload of the threads

            int* vals = stackalloc int[threadCount * 2];

            for(int i = 0; i < threadCount; i++)
            {
                vals[i * 2] = elementsPerThread * i; // Index

                vals[i * 2 + 1] = elementsPerThread; // Length
            }

            // Add the leftover elements to the last thread

            vals[threadCount * 2 - 1] += validSprites % threadCount;


            // Now start the jobs

            Job job = new();

            job.Method = (delegate*<nuint, void>)(delegate*<int*, void>)&uploadJob;

            for(int i = 0; i < threadCount; i++)
            {
                job.Overload = (nuint)(&vals[i * 2]);

                JobCenter.RunJob(job);
            }*/


            for(int i = 0; i < validSprites; i++)
            {
                Sprite* spr = (Sprite*)Finder.GetComponent(eP[i].Entity, Sprite.ComponentID);

                bindlessTexPMB[i] = spr->Texture.BTO;

                colorModPMB[i] = spr->RGBA;


                Transform* tran = (Transform*)Finder.GetComponent(eP[i].Entity, Transform.ComponentID);

                modelMatPMB[i] = tran->GetModelMatrix(eP[i].Entity);
            }
        }


        /*private static void uploadJob(int* vals)
        {
            //Console.WriteLine(vals[0] + " INDEX " + vals[1] + " LENGTH");


            for(int i = 0; i < vals[1]; i++)
            {
                int index = i + vals[0];


                Sprite* spr = (Sprite*)Finder.GetComponent(eP[index].Entity, Sprite.ComponentID);

                bindlessTexPMB[index] = spr->Texture.BTO;

                colorModPMB[index] = spr->RGBA;


                Transform* tran = (Transform*)Finder.GetComponent(eP[index].Entity, Transform.ComponentID);

                modelMatPMB[index] = tran->GetModelMatrix(eP[index].Entity);
            }
        }*/


    private static void spritePostDispatch(int cameraID)
    {
        // Clear the valid sprite counter

        validSprites ^= validSprites;
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

        postDispatches = CompactArray.Create<nuint>(0);

        resetFence();
    }


    // The initialiser of the system.
    // It shows the engine, what archetypes
    // the system processes

    public static int[] Init()
        => [Camera.ComponentID, Transform.ComponentID];


    // An opengl fence object,
    // for observing the state of the GPU

    private static nint gpuFence;

    public static bool IsGPUBusy()
    {
        // Get the snyc status of the fence

        GL.GetSync(gpuFence, SyncParameterName.SyncStatus, 1, out int len, out int val);

        
        // Compares the given value
        // with the numerical
        // equivalent of GL_UNSIGNALED

        return val == 0x9118;
    }


    // Reset the fence

    private static void resetFence()
    {
        nint old = gpuFence;

        gpuFence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);

        GL.DeleteSync(old);
    }


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


        if(!iter->IsLast)
            return;


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
        // Try to find a free index to
        // save the dispatch at

        for(int i = CompactArray.Length(postDispatches) - 1; i > -1; i--)
        {
            if(postDispatches[i] != 0)
                continue;

            postDispatches[i] = (nuint)dispatch;

            return;
        }

        // Allocate a new space to
        // save the dispatch at

        fixed(nuint** ptr = &postDispatches)
            CompactArray.Resize(ptr, CompactArray.Length(postDispatches) + 1);

        postDispatches[CompactArray.Length(postDispatches) - 1] = (nuint)dispatch;
    }


    public static void PostRender(ArchetypeIterator* iter)
    {
        // Iterate through each valid camera

        for(int i = 0; i < iter->Length(); i++)
        {

            // Iterate through each dispatch

            for(int d = CompactArray.Length(postDispatches) - 1; d > -1; d--)
            {
                if(postDispatches[d] == 0)
                    continue;

                ((delegate*<int, void>)postDispatches[d])(iter->GetEntityID(i));
            }
        }


        if(!iter->IsLast)
            return;


        // Clear the dispatches

        for(int d = CompactArray.Length(postDispatches) - 1; d > -1; d--)
        {
            if(postDispatches[d] == 0)
                continue;

            postDispatches[d] ^= postDispatches[d];
        }


        // Resets the gpu fence

        resetFence();
    }
}


// The sprite component is used to
// display an oriented image

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


    // Indicates how wide the
    // camera's view should be

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
    // with orthographic projection.
    // If the value is non zero, then
    // the camera will be treated as orthographic

    public float ProjectionSize;
}


public unsafe static class CameraExt
{
    public static Matrix4 GetProjection(this ref Camera cam)
    {
        Matrix4 result =
            cam.ProjectionSize != 0.0f ? Matrix4.CreateOrthographic(cam.ProjectionSize * WindowManager.AspectRatio, cam.ProjectionSize, cam.NearClip, cam.FarClip) :
                Matrix4.CreatePerspectiveFieldOfView(cam._fov, WindowManager.AspectRatio, cam.NearClip, cam.FarClip);

        return result;
    }

}


// A wrapper for OpenGL's
// texture buffers

public struct TextureBufferObj
{   
    // The reference to the
    // opengl texture buffer

    public int TBO;

    // The reference to the
    // buffer object containing
    // the actual data

    public int BO;
}

// Extension methods for
// texture buffer object

public unsafe static class TextureBufferObjExt
{
    // Hlper method for generating
    // the texture buffer

    public static void Create(this ref TextureBufferObj tbObj, SizedInternalFormat format, byte* data, int length)
    {
        // Generate the buffer, that'll
        // hold the data

        tbObj.BO = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.TextureBuffer, tbObj.BO);

    
        // Save the given data to the buffer

        GL.BufferData(BufferTarget.TextureBuffer, length, (nint)data, BufferUsageHint.DynamicDraw);


        // Generate the texture, that'll
        // hold the reference to the buffer

        tbObj.TBO = GL.GenTexture();        

        GL.BindTexture(TextureTarget.TextureBuffer, tbObj.TBO);

        GL.TexBuffer(TextureBufferTarget.TextureBuffer, format, tbObj.BO);
    }


    // Helper method for deleting the
    // texture buffer

    public static void Delete(this TextureBufferObj tbObj)
    {
        GL.DeleteBuffer(tbObj.BO);

        GL.DeleteTexture(tbObj.TBO);
    }


    // Helper method for binding
    // the texture buffer to the
    // given texture unit

    public static void Use(this TextureBufferObj tbObj, TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);

        GL.BindTexture(TextureTarget.TextureBuffer, tbObj.TBO);
    }
}


// A wrapper for OpenGL's texture.
// Includes both the texture buffer
// reference along with the bindless
// texture reference

public struct TextureObj
{
    // Reference to the
    // texture data

    public int TO;


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
        // Create the texture
        // and bind to it

        tObj.TO = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, tObj.TO);


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

        tObj.BTO = GL.Arb.GetTextureHandle(tObj.TO);

        GL.Arb.MakeTextureHandleResident(tObj.BTO);
    }


    public static void Create(this ref TextureObj tObj, string path, bool isGreyScale)
    {
        // Create the texture buffer
        // and bind to it

        tObj.TO = GL.GenTexture();

        GL.BindTexture(TextureTarget.Texture2D, tObj.TO);


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

        tObj.BTO = GL.Arb.GetTextureHandle(tObj.TO);

        GL.Arb.MakeTextureHandleResident(tObj.BTO);
    }


    // Deletes the given texture object

    public static void Delete(this ref TextureObj tObj)
    {
        // Delete the bindless texture reference

        GL.Arb.MakeTextureHandleNonResident(tObj.BTO);


        // Delete the texture buffer

        GL.DeleteTexture(tObj.TO);
    }


    // Binds the given
    // texture object to the
    // specified texture unit

    public static void Use(this ref TextureObj tObj, TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);

        GL.BindTexture(TextureTarget.Texture2D, tObj.TO);
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
