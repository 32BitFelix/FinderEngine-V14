using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Core;
using Core.LECSSimple;
using Core.MemoryManagement;
using Core.Shimshek;
using IO;
using IO.Logging;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

public unsafe static class Program
{
    public static void Main(string[] args)
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        WindowManager.Start(&Finder.Pulse, &Finder.End);        
    }


    public static void ShowBits(byte b)
    {
        for(int i = 7; i > -1; i--)
        {
            Console.Write((b >> i) & 1);

            if(i % 4 == 0)
                Console.Write(" ");
        }

        Console.WriteLine();

    }
}