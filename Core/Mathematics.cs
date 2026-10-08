
namespace Core.Mathematics;


// A fixed point representation
// of decimal numbers based on the
// DAFH convention
// TODO: benchmark with 64 bit implementation

public struct Fix
{
    // A constant representing the unit
    // value of the fixed point representation

    public const int Unit = 1000;

    // Find a way to utilise the integer inverse of the unit
    // public const int InverseUnit = ;


    // The value of the type

    public int Val;


    // Addition

    public static Fix operator +(Fix a, Fix b)
        => new Fix{Val = a.Val + b.Val};

    // Subtraction

    public static Fix operator -(Fix a, Fix b)
        => new Fix{Val = a.Val - b.Val};

    // Negation

    public static Fix operator -(Fix a)
        => new Fix{Val = -a.Val};

    // Multiplication

    public static Fix operator *(Fix a, Fix b)
        => new Fix{Val = (int)(Math.BigMul(a.Val, b.Val) / Unit)};    

    // Division

    public static Fix operator /(Fix a, Fix b)
        => new Fix{Val = (int)(Math.BigMul(a.Val, Unit) / b.Val)};

    // Remainder

    public static Fix operator %(Fix a, Fix b)
        => new Fix{Val = a.Val % b.Val};

    // Increment

    public static Fix operator ++(Fix a)
        => new Fix{Val = a.Val + Unit};

    // Decrement

    public static Fix operator --(Fix a)
        => new Fix{Val = a.Val - Unit};


    // AND

    public static Fix operator &(Fix a, Fix b)
        => new Fix{Val = a.Val & b.Val};

    // OR

    public static Fix operator |(Fix a, Fix b)
        => new Fix{Val = a.Val | b.Val};

    // XOR

    public static Fix operator ^(Fix a, Fix b)
        => new Fix{Val = a.Val ^ b.Val};

    // Left shift

    public static Fix operator <<(Fix a, int b)
        => new Fix{Val = a.Val << b};

    // Right shift

    public static Fix operator >>(Fix a, int b)
        => new Fix{Val = a.Val >> b};


    // Converts the fixed point representation
    // into IEE 754 floating point notation

    public static implicit operator float(Fix a)
        => a.Val * ((float)1 / Unit); // Inverse calculated at runtime


    // Converts an IEE 754 float to the DAFH 

    public static implicit operator Fix(float a)
        => new Fix{Val = (int)(a * Unit)};
}


// SIMD extensions to the
// DAFH fixed point notation

public static class FixVECTOR
{
    public static void Test()
    {



    }


}