using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Core;
using Core.Algorithms;
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
        /*try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

            WindowManager.Start(&Finder.Pulse, &Finder.End);   
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);

            Console.WriteLine(e.StackTrace);
        }*/


        Console.WriteLine(JobCenter.FreeThreadCount());


        const int count = 1000000;

        int* values = CompactArray.Create<int>(count);

        for(int i = 0; i < count; i++)
        {
            bool nextBool = Random.Shared.Next() < (int.MaxValue / 2);

            values[i] = Random.Shared.Next() | (*(byte*)&nextBool << 31);
        }


        //for(int i = 0; i < count; i++)  
        //    Console.WriteLine(values[i]);

        Console.WriteLine("-----");

        Stopwatch watch = Stopwatch.StartNew();

        Sorting.RadixSortST(values);

        Console.WriteLine(watch.ElapsedMilliseconds);

        //for(int i = 0; i < count; i++)  
        //    Console.WriteLine(values[i]);
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