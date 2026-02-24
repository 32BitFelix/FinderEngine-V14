using System.Runtime.InteropServices;

namespace FinderIntrinsics;


// A wrapper of a self made
// SIMD library for floating points
// and floating point types 
public unsafe static class FloatFast
{
    //  Type initialiser
    static FloatFast()
    {

        // Construct the string that'll hold
        // the path of the library fitting
        // best to the currently running
        // architecture and platform 

        string Path = "./Wrappers/FloatFast/";


        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Path = string.Concat(Path, "FF_Win");
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Path = string.Concat(Path, "FF_Lin");
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Path = string.Concat(Path, "FF_OSX");


        switch(RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
                Path = string.Concat(Path, "32");
            break;

            case Architecture.X64:
                Path = string.Concat(Path, "64");
            break;

            case Architecture.Arm:
                Path = string.Concat(Path, "arm32");
            break;

            case Architecture.Arm64:
                Path = string.Concat(Path, "arm64");
            break;
        }


        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Path = string.Concat(Path, ".dll");
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Path = string.Concat(Path, ".so");
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Path = string.Concat(Path, ".dylib");


        // Load the library

        nint libPtr = NativeLibrary.Load(Path);


        // Initialise the libraray

        NativeLibrary.GetExport(libPtr, "Initialise");

        delegate* unmanaged[Cdecl]<byte, void> function = (delegate* unmanaged[Cdecl]<byte, void>)NativeLibrary.GetExport(libPtr, "Initialise");

        function(2 | 4); // Currently blocking AVX and AVX512 support, because thy're not fully implemented


        // Save the length of a register
        // in floats

        OpCount = (byte*)NativeLibrary.GetExport(libPtr, "OP_AMOUNT");


        // Get the reference to the
        // list of operations

        nint* ops = (nint*)NativeLibrary.GetExport(libPtr, "OPS");


        // Finally, save the
        // references to the
        // operations

        Add = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[0];

        Sub = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[1];

        Mul = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[2];

        Div = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[3];

        Rem = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[4];


        Rcp =  (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[5];


        Sqrt = (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[6];

        RcpSqrt = (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[7];


        Max = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[8];

        Min = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[9];


        Equals = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[10];

        GreaterEquals = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[11];

        Greater = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[12];

        LessEquals = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[13];

        Less = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[14];


        NotEquals = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[15];

        NotGreaterEquals = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[16];
    
        NotGreater = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[17];
    
        NotLessEquals = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[18];

        NotLess = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[19];


        NotNAN = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[20];

        IsNAN = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[21];


        Mat4Add = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[19];

        Mat4Sub = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[20];

        Mat4Mul = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[21];

        Mat4Transpose = (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[22];

        Mat4Inv = (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[23];


        Vec4Transform = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[24];


        Vec4Add = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[25];

        Vec4Sub = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[26];

        Vec4Mul = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[27];

        Vec4Div = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[28];

        Vec4Rem = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[29];


        Vec4Len = (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[30];

        Vec4Dist = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[31];

        Vec4Dot = (delegate* unmanaged[Cdecl]<float*, float*, float*, void>)ops[32];

        Vec4Lerp = (delegate* unmanaged[Cdecl]<float*, float*, float*, float*, void>)ops[33];

        Vec4Norm = (delegate* unmanaged[Cdecl]<float*, float*, void>)ops[34];
    }


    // The amount of floating points
    // that can be processed at once
    public readonly static byte* OpCount;


    // References to basic
    // arithmetoc operations
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Add, Sub, Mul, Div, Rem;


    // Reference to the
    // reciprocal operation
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, void> Rcp;


    // References to the
    // squareroot operations
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, void> Sqrt, RcpSqrt;


    // References to the
    // Maximum and minimum
    // operations
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Max, Min;


    // References to the
    // comparison operations
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Equals, GreaterEquals, Greater, LessEquals, Less;


    // References to the negated
    // comparison operations
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> NotEquals, NotGreaterEquals, NotGreater, NotLessEquals, NotLess;


    // References to the
    // order oprations
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> NotNAN, IsNAN;


    // References to the
    // standard operations
    // of a matrix that take
    // one matrix
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, void> Mat4Transpose, Mat4Inv;


    // References to the
    // standard operations
    // of a matrix that take
    // two matrices
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Mat4Add, Mat4Sub, Mat4Mul;


    // Reference to the
    // Vector4 tranform
    // function.
    // vec, mat, dst
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Vec4Transform;


    // Reference to the
    // basic arithmetic
    // vector operstions
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Vec4Add, Vec4Sub, Vec4Mul, Vec4Div, Vec4Rem;


    // References to the
    // vector specific operations,
    // that take one vector
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, void> Vec4Len, Vec4Norm;


    // Reference to the
    // vector specific operations,
    // that take two vectors
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, void> Vec4Dist, Vec4Dot;
    
    
    // Reference to the
    // vector4 lerp
    // function
    // a, b, t, dst
    public static readonly delegate* unmanaged[Cdecl]<float*, float*, float*, float*, void> Vec4Lerp;


    // Copies the given amount of
    // bytes from the given source
    // to the given destination
    public static void Copy(nuint from, nuint to, int copySize)
    {
        // Long copy

        {
            // Initialise the counter
            // for this loop

            int i = copySize / sizeof(ulong);

            // Decrease the amount to
            // copy, to what's left
            // after the long copy

            copySize %= sizeof(ulong);


            // Loop repeats until
            // there isn't something
            // to copy with long

            while(i > 0)
            {
                // Decrement the counter

                i--;


                // Copy 64 bits

                ((ulong*)to)[i] = ((ulong*)from)[i];
            }
        }


        // Int copy

        {
            // Initialise the counter
            // for this loop

            int i = copySize / sizeof(uint);

            // Decrease the amount to
            // copy, to what's left
            // after the int copy

            copySize %= sizeof(uint);


            // Loop repeats until
            // there isn't something
            // to copy with int

            while(i > 0)
            {
                // Decrement the counter

                i--;


                // Copy 64 bits

                ((uint*)to)[i] = ((uint*)from)[i];
            }
        }


        // Short copy

        {
            // Initialise the counter
            // for this loop

            int i = copySize / sizeof(ushort);

            // Decrease the amount to
            // copy, to what's left
            // after the short copy

            copySize %= sizeof(ushort);


            // Loop repeats until
            // there isn't something
            // to copy with short

            while(i > 0)
            {
                // Decrement the counter

                i--;


                // Copy 64 bits

                ((ushort*)to)[i] = ((ushort*)from)[i];
            }
        }


        // Byte copy

        {
            // Initialise the counter
            // for this loop

            int i = copySize / sizeof(byte);


            // Loop repeats until
            // there isn't something
            // to copy with long

            while(i > 0)
            {
                // Decrement the counter

                i--;


                // Copy 64 bits

                ((byte*)to)[i] = ((byte*)from)[i];
            }
        }
    }
} 