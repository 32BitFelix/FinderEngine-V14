
using Core.LECS;
using IO;

public unsafe static class Program
{


    public static void Main(string[] args)
    {
        WindowManager.Start(&Engine.Pulse, &Engine.End);
    }

}