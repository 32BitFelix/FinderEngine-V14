using System.Runtime.InteropServices;
using Core.LECS;


namespace UserCore;


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