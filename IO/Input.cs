
using System.Numerics;
using System.Runtime.InteropServices;
using Core.MemoryManagement;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace IO.Input;


// This class manages keyboard and mouse input

public unsafe static class KBM
{
    // Type initialiser

    static KBM()
    {
        // Allocate and sanitise the array

        inputs = CompactArray.Create<InputState>(349);

        for(int i = 0; i < 349; i++)
            ((short*)inputs)[i] ^= ((short*)inputs)[i];


        // Set the keyboard callback

        {
            delegate*<Window*, Keys, int, InputAction, KeyModifiers, void> kC = &keyCallback;

            GLFWCallbacks.KeyCallback callback = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.KeyCallback>((nint)kC);

            GLFW.SetKeyCallback(WindowManager.windowPtr, callback);
        }


        // Set the mousebutton callback

        {
            delegate*<Window*, MouseButton, InputAction, KeyModifiers, void> mbC = &mouseButtonCallback;

            GLFWCallbacks.MouseButtonCallback callback = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.MouseButtonCallback>((nint)mbC);

            GLFW.SetMouseButtonCallback(WindowManager.windowPtr, callback);
        }


        // Set the mouse wheel callback

        {
            delegate*<Window*, double, double, void> sC = &scrollCallback;

            GLFWCallbacks.ScrollCallback callback = Marshal.GetDelegateForFunctionPointer<GLFWCallbacks.ScrollCallback>((nint)sC);

            GLFW.SetScrollCallback(WindowManager.windowPtr, callback);
        }
    }


    private static void keyCallback(Window* window, Keys key, int scanCode, InputAction action, KeyModifiers mod)
    {
        // See, if the given action
        // is about pressing or repeating

        bool positive = action != 0;

        // Held becomes true for any
        // positive input

        inputs[(int)key].Held = positive;

        // Pressed has to be made sure, that
        // the positive input isn't "repeat"

        inputs[(int)key].Pressed = positive & action != InputAction.Repeat;
    }


    private static void mouseButtonCallback(Window* window, MouseButton mouseButton, InputAction action, KeyModifiers mod)
    {
        // See, if the given action
        // is about pressing or repeating

        bool positive = action != 0;

        // Held becomes true for any
        // positive input

        inputs[(int)mouseButton].Held = positive;

        // Pressed has to be made sure, that
        // the positive input isn't "repeat"

        inputs[(int)mouseButton].Pressed = positive & action != InputAction.Repeat;
    }

    
    // An array to keep track
    // of keyboard and mouse buttons
    // (Compact array)

    private static InputState* inputs;


    private static double scrollXOffset, scrollYOffset;

    public static Vector2 ScrollVelocity;

    public static float ScrollSensitivity = 1.0f;


    // Callback for scroll events

    private static void scrollCallback(Window* window, double X, double Y)
    {
        ScrollVelocity.X = (float)(X - scrollXOffset) * ScrollSensitivity;

        ScrollVelocity.Y = (float)(Y - scrollYOffset) * ScrollSensitivity;


        scrollXOffset = X;

        scrollYOffset = Y;
    }


    // The cursor positions from the
    // last update

    private static double cursorXPos, cursorYPos;

    // The velocit of the cursor

    public static Vector2 CursorVelocity;

    // The sensitivity of the cursor

    public static float CursorSensitivity = 1.0f;


    public static bool IsPressed(int InputID)
        => inputs[InputID].Pressed;


    public static bool IsHeld(int InputID)
        => inputs[InputID].Held;


    public static void Update()
    {
        // Cursor velocity handling

        {
            GLFW.GetCursorPos(WindowManager.windowPtr, out double X, out double Y);

            CursorVelocity.X = (float)(X - cursorXPos) * CursorSensitivity;

            CursorVelocity.Y = (float)(Y - cursorYPos) * CursorSensitivity;

            cursorXPos = X;

            cursorYPos = Y;


            // Keep the cursor within the bounds of the screen

            if(GLFW.GetInputMode(WindowManager.windowPtr, CursorStateAttribute.Cursor) == CursorModeValue.CursorDisabled)
            {
                GLFW.GetFramebufferSize(WindowManager.windowPtr, out int width, out int height);

                GLFW.SetCursorPos(WindowManager.windowPtr, double.Clamp(X, 0, width), double.Clamp(Y, 0, height));
            }
        }


        // Scroll velocity handling

        fixed(Vector2* ptr = &ScrollVelocity)
            *(long*)ptr ^= *(long*)ptr;


        // Button handling

        for(int i = 0; i < 349; i++)
            *(byte*)&inputs[i].Pressed ^= *(byte*)&inputs[i].Pressed;
    }
}   


// 

public struct InputState
{
    // Indicates, that the
    // button was pressed
    // in this very frame

    public bool Pressed;

    // Indicates, if the
    // button is being held
    // for longer than a frame

    public bool Held;
}