using OpenTK.Mathematics;
using Core.LECSSimple;
using Core;
using Core.Shimshek;
using IO.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using IO;
using System.Runtime.InteropServices;

namespace UserCore.Mother;




[Level(true)]
public unsafe static class Main
{

    public static int LevelID;


    public static int pCamera, Redhead, Hourglass;


    public static int* ents;


    public static void Start()
    {
        int* comps = stackalloc int[2];

        comps[0] = Transform.ComponentID;

        comps[1] = Sprite.ComponentID;


        fixed(int* ptr = &Redhead)
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(Redhead, comps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(Redhead, Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 45, 0);

            tran->Translation = (0, 0, 0, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(Redhead, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create("./Resources/Textures/MisterRedhead.png", false);
        }


        fixed(int* ptr = &Hourglass)
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(Hourglass, comps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(Hourglass, Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (2, 0, 0, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(Hourglass, Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            spr->Texture.Create("./Resources/Textures/Hourglass.png", false);
        }


        const int entCount = 10;

        ents = (int*)NativeMemory.Alloc(sizeof(int) * entCount);

        Finder.CreateEntities(ents, entCount);

        for(int i = 0; i < entCount; i++)
            Finder.AddComponents(ents[i], comps, 2);

        for(int i = 0; i < entCount; i++)
        {
            Transform* tran = (Transform*)Finder.GetComponent(ents[i], Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (i * 2, -2, 0, 0);


            Sprite* spr = (Sprite*)Finder.GetComponent(ents[i], Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            int color = Random.Shared.Next();

            spr->Texture.Create((byte*)&color, 1, 1, false);
        }                 



        int* camComps = stackalloc int[2];

        camComps[0] = Transform.ComponentID;

        camComps[1] = Camera.ComponentID;


        fixed(int* ptr = &pCamera)    
            Finder.CreateEntities(ptr, 1);

        Finder.AddComponents(pCamera, camComps, 2);

        {
            Transform* tran = (Transform*)Finder.GetComponent(pCamera, Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (0, 0, 10, 0);


            Camera* cam = (Camera*)Finder.GetComponent(pCamera, Camera.ComponentID);

            cam->FarClip = 100;

            cam->NearClip = 0.1f;

            cam->FieldOfView = 90;

            cam->IsOrthographic = true;

            cam->ProjectionSize = 20;
        }


        /*WindowManager.WindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;

        WindowManager.CursorState = CursorModeValue.CursorDisabled;*/
    }


    public static void Update()
    {  
        if(KBM.IsHeld((int)Keys.Escape))
            WindowManager.CloseWindow();


        Transform* camTran = (Transform*)Finder.GetComponent(pCamera, Transform.ComponentID);

        //camTran->Translation.X += Finder.DeltaTime * 5;


        bool up = KBM.IsHeld((int)Keys.W);

        bool down = KBM.IsHeld((int)Keys.S);

        bool left = KBM.IsHeld((int)Keys.A);

        bool right = KBM.IsHeld((int)Keys.D);


        const float speed = 5;


        float delta = speed * Finder.DeltaTime;


        camTran->Translation.Y += (*(byte*)&up & 1) * delta;

        camTran->Translation.Y -= (*(byte*)&down & 1) * delta;

        camTran->Translation.X -= (*(byte*)&left & 1) * delta;

        camTran->Translation.X += (*(byte*)&right & 1) * delta;
    }


    public static void NarrowPhase()
    {



    }


    public static void BroadPhase()
    {



    }


    public static void PreRender()
    {



    }


    public static void Render()
    {



    }


    public static void PostRender()
    {



    }
}


[System(false, false)]
public unsafe static class TransformProcessor
{
    public static int[] Init()
        => [Transform.ComponentID];

    public static void Update(ArchetypeIterator* iter)
    {



    }
}


[System(false, false)]
public unsafe static class A_Processor
{
    public static int[] Init()
        => [A.ComponentID];


    public static void Update(ArchetypeIterator* iter)
    {

        //Console.WriteLine("LESSER");

    }

} 



[System(false, true)]
public unsafe static class AB_Processor
{
    public static int[] Init()
        => [A.ComponentID, B.ComponentID];


    public static void Update(ArchetypeIterator* iter)
    {



    }


    public static void NarrowPhase(ArchetypeIterator* iter)
    {



    }


    public static void BroadPhase(ArchetypeIterator* iter)
    {



    }


    public static void PreRender(ArchetypeIterator* iter)
    {



    }


    public static void Render(ArchetypeIterator* iter)
    {



    }


    public static void PostRender(ArchetypeIterator* iter)
    {



    }
}


[Component]
public unsafe struct A
{
    public int i;

    public nuint ptr;


    public static int ComponentID;


    public static int counter = 0;


    public static void Init(int entityID, A* component)
    {
        counter += 100;

        component->i = counter;


        Console.WriteLine("A INIT " + (nuint)component);
    }


    public static void Fin(int entityID, A* component)
    {
        Console.WriteLine("A FIN " + (nuint)component + " " + entityID + " " + component->i);



    }
}


[Component]
public unsafe struct B
{
    public short S;

    public nuint ptr;


    public static int ComponentID;


    public static int counter = 0;


    public static void Init(int entityID, B* component)
    {
        counter += 20;

        component->S = (short)counter;

        Console.WriteLine("B INIT " + (nuint)component);


    }


    public static void Fin(int entityID, B* component)
    {
        Console.WriteLine("B FIN " + (nuint)component + " " + entityID + " " + component->S);



    }
}


[Component]
public unsafe struct C
{
    public byte b;

    public nuint ptr;


    public static int ComponentID;


    public static int counter = 0;


    public static void Init(int entityID, C* component)
    {
        counter += 1;

        component->b = (byte)counter;

        Console.WriteLine("C INIT " + (nuint)component);


    }


    public static void Fin(int entityID, C* component)
    {
        Console.WriteLine("C FIN " + (nuint)component + " " + entityID + " " + component->b);



    }
}