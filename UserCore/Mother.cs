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


        int* entComps = stackalloc int[3];

        entComps[0] = Sprite.ComponentID;

        entComps[1] = Transform.ComponentID;

        entComps[2] = Rotator.ComponentID;

        const int entCount = 1000;

        ents = (int*)NativeMemory.Alloc(sizeof(int) * entCount * 3);

        Finder.CreateEntities(ents, entCount * 3);

        for(int i = 0; i < entCount * 3; i++)
            Finder.AddComponents(ents[i], entComps, 3);


        // Make the opaque boxes

        for(int i = 0; i < entCount; i++)
        {
            Transform* tran = (Transform*)Finder.GetComponent(ents[i], Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (i % (int)MathF.Sqrt(entCount) * 2, i % entCount / (int)MathF.Sqrt(entCount) * 2, 0, 0);            


            Sprite* spr = (Sprite*)Finder.GetComponent(ents[i], Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            uint color = uint.MaxValue;

            spr->Texture.Create((byte*)&color, 1, 1, false);   
        }


        // Make the slightly transparent boxes

        for(int i = entCount; i < entCount * 2; i++)
        {
            Transform* tran = (Transform*)Finder.GetComponent(ents[i], Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (i % (int)MathF.Sqrt(entCount) * 2, i % entCount / (int)MathF.Sqrt(entCount) * 2, 1, 0);            


            Sprite* spr = (Sprite*)Finder.GetComponent(ents[i], Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (255 << 24);

            int color = (255 << 8) | (150 << 24);

            spr->Texture.Create((byte*)&color, 1, 1, false);   
        }


        // Make the very transparent boxes

        for(int i = entCount * 2; i < entCount * 3; i++)
        {
            Transform* tran = (Transform*)Finder.GetComponent(ents[i], Transform.ComponentID);

            tran->Scale = (1, 1, 1, 1);

            tran->Rotation = (0, 0, 0, 0);

            tran->Translation = (i % (int)MathF.Sqrt(entCount) * 2, i % entCount / (int)MathF.Sqrt(entCount) * 2, 2, 0);            


            Sprite* spr = (Sprite*)Finder.GetComponent(ents[i], Sprite.ComponentID);

            spr->RGBA = 255 | (255 << 8) | (255 << 16) | (100 << 24);

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

            cam->ProjectionSize = 0.0f;
        }


        WindowManager.WindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;

        WindowManager.CursorState = CursorModeValue.CursorDisabled;
    }


    public static void Update()
    {  
        if(KBM.IsHeld((int)Keys.Escape))
            WindowManager.CloseWindow();


        Transform* camTran = (Transform*)Finder.GetComponent(pCamera, Transform.ComponentID);


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


        camTran->Rotation.Y += KBM.CursorVelocity.X;

        camTran->Rotation.X += KBM.CursorVelocity.Y;


        camTran->Rotation.X = Math.Clamp(camTran->Rotation.X, -90, 90);
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


[Component]
public struct Rotator
{
    public static int ComponentID;
}


[System(false, false)]
public unsafe static class RotatorSystem
{
    public static int[] Init()
        => [Transform.ComponentID, Rotator.ComponentID];


    public static void Update(ArchetypeIterator* iter)
    {
        float delta = Finder.DeltaTime * 20;

        for(int i = 0; i < iter->Length(); i++)
        {
            if(iter->GetEntityID(i) < 2)
                continue;


            Transform* tran = (Transform*)iter->GetComponent(i, Transform.ComponentID);

            tran->Rotation.Z += delta;
        }


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