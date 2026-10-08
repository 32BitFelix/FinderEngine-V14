using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.LECSSimple;
using IO;
using OpenTK.Mathematics;

public unsafe static class Program
{
    public static void Main(string[] args)
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        WindowManager.Start(&Finder.Pulse, &Finder.End);
    }
}
