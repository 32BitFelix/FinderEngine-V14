using Core.LECS;


namespace UserCore;


[Level(true)]
public static class MotherLevel
{
    public static int LevelID;


    public static void Start()
    {
        Console.WriteLine("START");
    }


    static int cnt = 0;

    public static void Update()
    {
        Console.WriteLine("UPD " + cnt);

        cnt++;

        if(cnt == 100)
        {
            Engine.EndLevel(typeof(MotherLevel));

            Engine.StartLevel(typeof(Secondary));
        }

    }


    public static void End()
    {
        Console.WriteLine("END");

        cnt = 0;
    }
}


[Level(false)]
public static class Secondary
{
    public static int LevelID;

    public static void Start()
    {
        Console.WriteLine("STARTSEC");
    }


    static int cnt;

    public static void Update()
    {
        Console.WriteLine("UPDSEC " + cnt);

        cnt++;

        if(cnt == 100)
        {
            Engine.EndLevel(typeof(Secondary));

            Engine.StartLevel(typeof(MotherLevel));
        }
    }


    public static void End()
    {
        Console.WriteLine("ENDSEC");

        cnt = 0;
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


    public static void Update(int* offsets, int offsetAmount)
    {

    }
}