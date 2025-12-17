using System.Numerics;
using System.Runtime.InteropServices;
using Core.LECS;
using Core.MemoryManagement;
using FinderIntrinsics;
using IO;

public unsafe static class Program
{
    public static void Main(string[] args)
    {
        WindowManager.Start(&Engine.Pulse, &Engine.End);
    }
}