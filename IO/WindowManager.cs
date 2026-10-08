
using System.Diagnostics;
using System.Runtime.InteropServices;
using IO.Input;
using IO.Logging;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Monitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;


namespace IO;


// The window manager manages
// the shown window, initialises
// the graphics and audio library
// and kickstarts the updates of
// whatever given engine

public unsafe static class WindowManager
{
    // Type initialiser
#pragma warning disable CS8618
    static WindowManager()
#pragma warning restore
    {
        _initGL();

        _initAL();
    }

        // Initialises the graphics library

        private static void _initGL()
        {
            // Initialise the
            // glfw library

            if(!GLFW.Init())
            {
                // Failed to initialise glfw

                return;
            }


            // Allocate the array
            // to hold the title
            // of the window

            byte* title =
                (byte*)NativeMemory.Alloc(sizeof(byte) * 7);

            title[0] = (byte)'F';

            title[1] = (byte)'I';

            title[2] = (byte)'N';

            title[3] = (byte)'D';

            title[4] = (byte)'E';

            title[5] = (byte)'R';

            title[6] = (byte)'\0';


            // Set the major version

            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 3);

            // Set the minor version

            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);

            // Hinting that the client's api is OpenGL

            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);

            // Hinting that the GL to use is the core version

            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

            // Hinting that the window starts focused

            GLFW.WindowHint(WindowHintBool.Focused, true);


            // Create a window

            windowPtr = GLFW.CreateWindowRaw(800, 600, title, null, null);


            // Check if the making of the window failed

            if(windowPtr == (Window*)null)
            {
                // Window creation failed!

                GLFW.Terminate();

                return;
            }


            // Make window's context current

            GLFW.MakeContextCurrent(windowPtr);

            // Get the reference of the
            // computer's main monitor        

            monitorPtr = GLFW.GetPrimaryMonitor();

            // Load a glfw bindings context

            glfwContext = new GLFWBindingsContext();

            // Load opengl bindings

            GL.LoadBindings(glfwContext);


            // Set the default aspect ratio

            AspectRatio = 4f / 3;


            // Set the clear color of
            // the backbuffer

            GL.ClearColor(0.5f, 0.5f, 1, 1);   


            GLFW.SwapInterval(0);


            // Set a callback for resizing
            // the window

            GLFW.SetFramebufferSizeCallback(windowPtr, Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.FramebufferSizeCallback>((nint)(delegate*<Window*, int, int, void>)&_resize));


            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            // Define the attachments of
            // the framebuffer


            // Retrieve the window's
            // framebuffer's size

            /*int xDim = 0;

            int yDim = 0;

            GLFW.GetFramebufferSizeRaw(windowPtr, &xDim, &yDim);


            // Generate the colorbuffer

            fixed(int* ptr = &ColorBuffer)
                GL.GenTextures(1, ptr);

            GL.BindTexture(TextureTarget.Texture2D, ColorBuffer);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, xDim, yDim, 0, PixelFormat.Rgb, PixelType.UnsignedByte, 0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


            // Generate the depth and stencil buffer

            fixed(int* ptr = &DepthStencilBuffer)
                GL.GenTextures(1, ptr);

            GL.BindTexture(TextureTarget.Texture2D, DepthStencilBuffer);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, xDim, yDim, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, 0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);


            // Generate the framebuffer

            fixed(int* ptr = &MainBuffer)
                GL.GenFramebuffers(1, ptr);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainBuffer);

            // Bind the color buffer

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffer, 0);
            
            // Bind the depth and stencil buffer

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, TextureTarget.Texture2D, DepthStencilBuffer, 0);


            // Bind back to the normal frame buffer

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);*/
        }


        // initialises the audio library

        private static void _initAL()
        {
            // The array of
            // possible devices
            // to output from

            string[] deviceNames = ["OpenAL Soft", "Generic Software", "Generic Hardware"];
            
            // Iterate through each device

            for(int j = 0; j < deviceNames.Length; j++)
            {
                // Try to open the
                // current device

                alDevice = ALC.OpenDevice(deviceNames[j]);

                // If connecting was
                // successful, prematurely
                // end the loop

                if(alDevice != ALDevice.Null)
                    break;
            }


            // Allocate an int array
            // that'll hold attributes
            // for the new context 

            int* newAttribs = stackalloc int[3];

            // Set the ID of the attribute,
            // so that the context knows
            // what should be set.
            // In this case it's the amount
            // of mono sources

            newAttribs[0] = 4112;

            // Set the value of the
            // attribute

            newAttribs[1] = int.MaxValue;

            // Add a null terminator,
            // to tell the context that
            // this is everything of the
            // new context

            newAttribs[2] = 0;


            // Create the new context

            alContext = ALC.CreateContext(alDevice, newAttribs);

            // Make the new context current,
            // but if that fails, prematurely
            // end the method

            if(!ALC.MakeContextCurrent(alContext))
                return;


            // Set the Position of the
            // listener to zero

            AL.Listener(ALListener3f.Position, 0f, 0f, 0f);

            // Set the velocity of the
            // listener to zero

            AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);

            // Create the orientation of
            // the listener

            float[] orientation = [0, 0, 1, 0, 1, 0];

            // Set the orientation of
            // the listener

            AL.Listener(ALListenerfv.Orientation, orientation);

            // Set the distance model
            // of the listener

            AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);
        }


    // The OpenGL and GLFW context of the window

    private static GLFWBindingsContext glfwContext;

    // The target framebuffer to render to.
    // It might have a different resolution
    // compared to the window's resolution

    public static readonly int MainBuffer;

        // The colorbuffer of the framebuffer

        public static readonly int ColorBuffer;

        // The depth- and stencilbuffer of
        // the framebuffer

        public static readonly int DepthStencilBuffer;


    // The desired aspect
    // ratio of the framebuffer

    public static float AspectRatio;


    // OpenAL context of the window

    private static ALContext alContext;

    // The device to play the sounds from

    private static ALDevice alDevice;



    // The pointer reference to the window

    public static Window* windowPtr;

    // The pointer reference to the monitor

    private static Monitor* monitorPtr;


    // Kickstarts a loop, that repeats,
    // as long as the window is up and
    // calls the given update method, too   

    public static void Start(delegate*<float, void> update, delegate*<void> end)
    {
        // Repeat this loop as long
        // as the window is open or
        // there is no error to speak of

        while(!GLFW.WindowShouldClose(windowPtr) && !Logger.HasError)
        {
            // Check for some
            // window specific
            // events

            GLFW.PollEvents();


            // Bind to the buffer to render to

            //GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);


            // Clear the backbuffer

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            // Call the update
            // of the hooked engine
            // and share the deltatime

            //update((float)GLFW.GetTime());

            float delta = (float)GLFW.GetTime();

            GLFW.SetTime(0);

            update(delta);


            KBM.Update();


            // Reset the timer

            //GLFW.SetTime(0);


            // Copy the buffer to render to
            // to the window's framebuffer


            /*int xDim = 0;

            int yDim = 0;

            GLFW.GetFramebufferSizeRaw(windowPtr, &xDim, &yDim);


            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, MainBuffer);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);


            GL.BlitFramebuffer(xDim, yDim, 0, 0, xDim, yDim, 0, 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);*/

            //Console.WriteLine(GL.GetError());


            // Swap the buffers

            GLFW.SwapBuffers(windowPtr);
        }
        

        // Get the window out of fullscreen,
        // before hiding it

        if(previousWindowState == WindowState.Fullscreen)
        {
            GLFW.GetWindowSize(windowPtr, out int width, out int height);

            GLFW.SetWindowMonitor(windowPtr, null, 0, 0, width, height, 0);
        }


        // Hides the window, so that it
        // doesn't bother the user anymore

        GLFW.HideWindow(windowPtr);


        // Call the end of the hooked
        // engine, if it is defined

        if(end != null)
            end();


        GLFW.DestroyWindow(windowPtr);

        GLFW.Terminate();
    }   
    

    // The method to handle all
    // resize events of the window

    private static void _resize(Window* ptr, int width, int height)
    {   
        // Set the viewport

        GL.Viewport(0, 0, width, height);


        AspectRatio = (float)width / height;


        // Set the colorbuffer size

        /*GL.BindTexture(TextureTarget.Texture2D, ColorBuffer);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, 0);


        // Set the depth and stencil buffer size

        GL.BindTexture(TextureTarget.Texture2D, DepthStencilBuffer);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Depth24Stencil8, width, height, 0, PixelFormat.DepthStencil, PixelType.UnsignedInt248, 0);*/
    }                             


    public static CursorModeValue CursorState
    {
        get => GLFW.GetInputMode(windowPtr, CursorStateAttribute.Cursor);

        set => GLFW.SetInputMode(windowPtr, CursorStateAttribute.Cursor, value);
    }


    private static WindowState previousWindowState;

    public static WindowState WindowState
    {
        get => previousWindowState;

        set
        {
            if(previousWindowState == WindowState.Fullscreen)
            {
                GLFW.GetWindowSize(windowPtr, out int width, out int height);

                GLFW.SetWindowMonitor(windowPtr, null, 0, 0, width, height, 0);
            }

            previousWindowState = value;

            switch(value)
            {
                case WindowState.Normal:
                    GLFW.RestoreWindow(windowPtr);
                return;

                case WindowState.Minimized:
                    GLFW.IconifyWindow(windowPtr);
                return;

                case WindowState.Maximized:
                    GLFW.MaximizeWindow(windowPtr);
                return;

                case WindowState.Fullscreen:
                    VideoMode* nMode = GLFW.GetVideoMode(monitorPtr);
                    GLFW.SetWindowMonitor(windowPtr, monitorPtr, 0, 0, nMode->Width, nMode->Height, nMode->RefreshRate);
                return;
            }
        }
    }


    public static void CloseWindow()
        => GLFW.SetWindowShouldClose(windowPtr, true);
}