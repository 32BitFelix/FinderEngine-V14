using System.Runtime.InteropServices;
using Core.LECS;
using Core.MemoryManagement;
using FinderIntrinsics;
using IO;

public unsafe static class Program
{


    public static void Main(string[] args)
    {
        //WindowManager.Start(&Engine.Pulse, &Engine.End);


        long* val;

        ChunkArray.Create(&val, 10);


        for(int i = 0; i < ChunkArray.Length(val); i++)
        {
            Console.WriteLine("------ " + i);

            long* ptr = ChunkArray.ReadChunk(&val, i);

            for(int j = 0; j < ChunkArray.ChunkLength; j++)
            {
                Console.WriteLine(ptr[j]);
            }
        }


        ChunkArray.Resize(&val, 8);


        for(int i = 0; i < ChunkArray.Length(val); i++)
        {
            Console.WriteLine("------ " + i);

            long* ptr = ChunkArray.ReadChunk(&val, i);

            for(int j = 0; j < ChunkArray.ChunkLength; j++)
            {
                Console.WriteLine(ptr[j]);
            }
        }        


        ChunkArray.Resize(&val, 20);


        for(int i = 0; i < ChunkArray.Length(val); i++)
        {
            Console.WriteLine("------ " + i);

            long* ptr = ChunkArray.ReadChunk(&val, i);

            for(int j = 0; j < ChunkArray.ChunkLength; j++)
            {
                Console.WriteLine(ptr[j]);
            }
        }
    }
}