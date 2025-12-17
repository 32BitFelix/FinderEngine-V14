using System.Runtime.InteropServices;
using Core.LECS;


namespace UserCore;


[Level(true)]
public unsafe static class MainLevel
{
    public static int LevelID;


    public static void Start()
    {
        Console.WriteLine("Starting");


        int* ents = stackalloc int[64];

        Engine.CreateEntities(ents, 64);


        for(int i = 0; i < 64; i++)
        {
            Console.WriteLine(ents[i]);
        }


        Engine.BindChildren(ents[0], &ents[10], 5);


        int[] children = Engine.ShowChildren(ents[0]);

        for(int i = 0; i < children.Length; i++)
        {
            Console.WriteLine(children[i] + " CHILD");
        }


        Engine.UnbindChildren(ents[0], &ents[10], 3);


        children = Engine.ShowChildren(ents[0]);

        for(int i = 0; i < children.Length; i++)
        {
            Console.WriteLine(children[i] + " CHILD");
        }


        Console.WriteLine(Engine.ShowParent(ents[14]) + " PARENT");


        Engine.DeleteEntities(ents, 64);
    }


    public static void Update()
    {

        //Console.WriteLine("Updating");

    }


    public static void End()
    {

        Console.WriteLine("Ending");

    }
}


[System]
public unsafe static class TestSystem
{
    public static int[] Init()
        => [JoeComp.ComponentID, MikeComp.ComponentID];    


    public static void Update(void* chunk)
    {
        


    }
}


[Component]
public unsafe struct JoeComp
{

    public static void Init(int eID, int chunkIndex, void* chunk)
    {

    }


    public static void Fin(int eID, int chunkIndex, void* chunk)
    {

    }


    public static int ComponentID;



    public int third;


    public ushort second;


    public byte first;
}

[Component]
public unsafe struct MikeComp
{

    public static int ComponentID;

}


/*[Level(true)]
public unsafe static class MotherLevel
{
    public static int LevelID;


    public static void Start()
    {
        Console.WriteLine("HELLO");

        int first = Engine.CreateEntity();

        Console.WriteLine("FIRST");

        int second = Engine.CreateEntity();

        Console.WriteLine("SECOND");

        int third = Engine.CreateEntity();

        Console.WriteLine(first + " " + second + " " + third);


        Engine.DeleteEntity(third);


        Engine.BindChild(first, second);

        int* list = Engine.ShowChildren(first);

        Console.WriteLine(list[0]);

        for(int i = 0; i < list[0]; i++)
            Console.WriteLine(i + ": " + list[i + 1]);

        NativeMemory.Free(list);


        Engine.UnbinChild(first, second);

        list = Engine.ShowChildren(first);

        Console.WriteLine(list[0]);

        for(int i = 0; i < list[0]; i++)
            Console.WriteLine(i + ": " + list[i + 1]);

        NativeMemory.Free(list);
    }


    static int cnt = 0;

    public static void Update()
    {


    }


    public static void End()
    {


    }
}


[Component]
public struct Velocity
{
    public static int ComponentID;

    public static void Init(int eID)
    {
        Console.WriteLine("HELLO!");

    }
}



[System(SystemState.None)]
public unsafe static class Accelerator
{
    public static OffsetInfo Init()
    {


        return new([Velocity.ComponentID]);
    }


    public static void Update(int* offsets)
    {

    }
}*/