
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.MemoryManagement;
using IO.Logging;
using UserCore;

namespace Core.LECS_OLD;


// NOTE: I've been noticing, that
// the way memory is handled in the
// engine resembles a lot to the way
// how GPU compute libraries handle
// memory. I wouldn't be opposed to
// native compute shader support

// The engine that handles
// the LECS system. 

public unsafe static partial class Engine
{
    // type initializer

    static Engine()
    {
        // Get all types of
        // the application

        Type[] types = Assembly.GetExecutingAssembly().GetTypes();   


        // An array to hold all components

        Type[] fComponents = [];


        // An array to hold all systems

        Type[] fSystems = [];


        // An array to hold all levels

        Type[] fLevels = [];


        // Iterate through each type

        foreach(Type type in types)
        {
            // If the type is a component,
            // add it to the found component collection

            if(type.GetCustomAttribute<ComponentAttribute>() != null)
            {
                // Skip this type, if it
                // isn't a value type

                if(!type.IsValueType)
                    continue;


                Array.Resize(ref fComponents, fComponents.Length + 1);

                fComponents[^1] = type;

                continue;
            }


            // If the type is a component,
            // add it to the found system collection

            if(type.GetCustomAttribute<SystemAttribute>() != null)
            {
                // Skip this type, if it
                // isn't a class

                if(!type.IsClass)
                    continue;


                Array.Resize(ref fSystems, fSystems.Length + 1);

                fSystems[^1] = type;

                continue;
            }


            // If the type is a component,
            // add it to the found level collection

            if(type.GetCustomAttribute<LevelAttribute>() != null)
            {
                // Skip this type, if it
                // isn't a class

                if(!type.IsClass)
                    continue;


                Array.Resize(ref fLevels, fLevels.Length + 1);

                fLevels[^1] = type;

                continue;
            }
        }


        _compAss(fComponents);


        _systAss(fSystems);


        _lvlAss(fLevels);
    }


        // TODO: Multithread the processing
        // of the list of picked types


        // This method will assign the given
        // component types a bit index 

        private static void _compAss(Type[] types)
        {

            // Allocate memory for the
            // array to hold all the component types 

            components = CompactArray.Create<Component>(types.Length);

            // Keeps track of all components, that
            // have been deemed valid. The components
            // array might get resized, depending
            // on if the valid components are less
            // than the givn amount

            int tracker = 0;


            // Iterate through each
            // component type

            foreach(Type type in types)
            {
                // Get the necessary info
                // of the component and
                // increment the tracker


                Component nCom = new Component()
                {
                    // Get the index of the
                    // bit representing the
                    // component

                    BitIndex = tracker,

                    // Get the size in bytes
                    // of the component

                    Size = Marshal.SizeOf(type)
                };


                // Check for the component's
                // component ID field and
                // set it's value

                bool hasIDField = false;

                foreach(FieldInfo f in type.GetFields())
                {
                    if(!f.IsStatic)
                        continue;

                    if(f.FieldType != typeof(int))
                        continue;

                    if(!string.Equals(f.Name.ToLower(), "componentid"))
                        continue;

                    hasIDField = !hasIDField;

                    f.SetValue(null, tracker);

                    break;
                }

                // Blare an error, if there
                // is no component ID field

                if(!hasIDField)
                {


                    return;
                }


                // Iterate through each method
                // of the component

                foreach (MethodInfo m in type.GetMethods())
                {
                    // If the method isn't
                    // static, skip to the next iteration

                    if(!m.IsStatic)
                        continue;


                    // Check the name of the
                    // current method

                    switch(m.Name.ToLower())
                    {
                        // Component initialiser

                        case "init":

                            nCom.Initialiser = (delegate*<int, void>)m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Component finaliser

                        case "fin":

                            nCom.Finaliser = (delegate*<int, void>)m.MethodHandle.GetFunctionPointer();

                        continue;
                    }
                }


                // Add the component to
                // the collection

                components[tracker++] = nCom;
            }
        }


        // This method will initialise the
        // systems and save some information of them

        private static void _systAss(Type[] types)
        {
            // Allocate memory for the array
            // to hold all systems

            systems = CompactArray.Create<System>(types.Length);


            // Iterate through each found system

            foreach(Type type in types)
            {

                // Create a new instance
                // of a system

                System nSys = new()
                {
                    OnUpdate = 0,

                    OnBroadPhase = 0,

                    OnNarrowPhase = 0,

                    OnPreRender = 0,

                    OnRender = 0,

                    OnPostRender = 0,
                };

                // Iterate through each method
                // of the system

                foreach(MethodInfo m in type.GetMethods())
                {
                    // Skip to the next iteration,
                    // if the method isn't static

                    if(!m.IsStatic)
                        continue;

                    // Check the name of
                    // the current method

                    switch(m.Name.ToLower())
                    {
                        // System initialiser

                        case "init":

                            // Get the offset infos

                            nSys.OffsetInfo = ((delegate*<OffsetInfo>)m.MethodHandle.GetFunctionPointer())();

                            // Blare an error, if
                            // the method returned
                            // no offset info

                            if(CompactArray.Length(nSys.OffsetInfo.ComponentIDs) == 0)
                            {


                                return;
                            }

                        continue;

                        // System update

                        case "update":

                            nSys.OnUpdate = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // System fixed broad phase

                        case "broadPhase":

                            nSys.OnBroadPhase = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // System fixed narrow phase

                        case "narrowPhase":

                            nSys.OnNarrowPhase = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // System pre-render

                        case "prerender":

                            nSys.OnPreRender = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // System render

                        case "render":

                            nSys.OnRender = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // System post-render

                        case "postrender":

                            nSys.OnPostRender = m.MethodHandle.GetFunctionPointer();

                        continue;
                    }
                }

#pragma warning disable CS8600
#pragma warning disable CS8602

                // Get some states for
                // the system

                SystemAttribute sysAttr = type.GetCustomAttribute<SystemAttribute>();

                nSys.State = sysAttr.State;

#pragma warning restore CS8600
#pragma warning restore CS8602

            }
        }


        // This system will save all levels
        // and hold them ready for

        private static void _lvlAss(Type[] types)
        {
            // Allocate memory for the array
            // to hold all levels

            levels = CompactArray.Create<Level>(types.Length);


            // A counter for the
            // level's IDs

            int tracker = 0;


            // Iterate through each type
            // representing a level

            foreach(Type type in types)
            {
                
                // Create an instance of
                // the new level

                Level nLevel = new()
                {
                    LevelID = tracker,

                    TimeScale = 1,

                    State = 0,
                };

                // Create the array to hold the entities
                // of the new level

                ChunkArray.Create(&nLevel.Entities, 0);

                // Create the array to hold the archetypes
                // of the new level

                ChunkArray.Create(&nLevel.Archetypes, 1);


                // Create the default
                // archetype of the level

                {
                    // Get the pointer address of the
                    // first zero archetype

                    Archetype* zeroArchetype = ChunkArray.ReadChunk(&nLevel.Archetypes, 0);


                    // Initialise the systems list

                    zeroArchetype->Systems = CompactArray.Create<System>(0);


                    // Set the data

                    zeroArchetype->Data = (byte*)NativeMemory.Alloc(sizeof(long));

                    *(long*)zeroArchetype->Data ^= *(long*)zeroArchetype->Data;


                    // Set the mask

                    int maskSize = getMaskSize();

                    zeroArchetype->ComponentMask = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * maskSize));

                    for(int i = 0; i < maskSize; i++)
                        zeroArchetype->ComponentMask[i] ^= zeroArchetype->ComponentMask[i];
                }


                // Check, if the level is a starter

#pragma warning disable CS8600, CS8602

                LevelAttribute? lA = type.GetCustomAttribute<LevelAttribute>();

                if(lA.IsStarter)
                    nLevel.State = LevelState.ShouldStart;

#pragma warning restore


                bool hasLevelID = false;

                // Iterate through each field
                // of the current type

                foreach(FieldInfo field in type.GetFields())
                {
                    // Skip to the next
                    // iteration, if the
                    // current field isn't
                    // static

                    if(!field.IsStatic)
                        continue;

                    // Skip to the next iteration,
                    // if the current type isn't an int

                    if(field.FieldType != typeof(int))
                        continue;

                    // Skip to the next iteration, if
                    // the field's name is not levelid

                    if(!string.Equals(field.Name.ToLower(), "levelid"))
                        continue;


                    field.SetValue(null, tracker);

                    
                    hasLevelID = !hasLevelID;

                    break;
                }


                // Blare an error, if the
                // current type has no
                // levelID field

                if(!hasLevelID)
                {


                    return;
                }


                // Iterate through each
                // method of the level

                foreach(MethodInfo m in type.GetMethods())
                {

                    // Skip to the next
                    // iteration, if the
                    // current method isn't
                    // static

                    if(!m.IsStatic)
                        continue;


                    // Check the name of 
                    // the current method

                    switch(m.Name.ToLower())
                    {
                        // Start method

                        case "start":

                            nLevel.OnStart = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Update method

                        case "update":

                            nLevel.OnUpdate = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Broadphase method

                        case "broadphase":

                            nLevel.OnBroadPhase = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Narrowphase method

                        case "narrowphase":

                            nLevel.OnNarrowPhase = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Pre-Render method

                        case "prerender":

                            nLevel.OnPreRender = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Render method

                        case "render":

                            nLevel.OnRender = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // Post-Render method

                        case "postrender":

                            nLevel.OnPostRender = m.MethodHandle.GetFunctionPointer();

                        continue;

                        // end method

                        case "end":

                            nLevel.OnEnd = m.MethodHandle.GetFunctionPointer();

                        continue;
                    }
                }


                levels[tracker++] = nLevel;
            }
        }


    // The collection of levels
    // (Compact array)

    private static Level* levels;


    // The collection of systems
    // (Compact array)

    private static System* systems;


    // The collection of all component types
    // (Compact array)

    private static Component* components;


    // The delta time of the current level

    public static float DeltaTime {get; private set;}


    // The index of the currently run level

    private static int currentLevelIndex;


    // The point where the pulses
    // related to the 

    public static void Pulse(float dt)
    {
        stepUpdate(dt);

        stepFixedUpdate();

        stepRenderUpdate();
    }


    // Hidden method, that steps through
    // all update methods of systems and levels

    private static void stepUpdate(float dt)
    {
        // Step through each Level

        for(int i = CompactArray.Length(levels) - 1; i >= 0; i--)
        {
            // Check, if the scene should be initialised,
            // or if it is initialised.
            // If it isn't either, skip to the next iteration

            bool check = (levels[i].State & LevelState.ShouldStart) == LevelState.ShouldStart;

            check |= (levels[i].State & LevelState.IsInitialised) == LevelState.IsInitialised;

            if(!check)
                continue;


            // Calculate the level's deltatime

            DeltaTime = dt * levels[i].TimeScale;


            // Broadcast the index of
            // the currently run level

            currentLevelIndex = i;


            // Check, if the update method, or
            // start method of the level should
            // be called

            check = (levels[i].State & LevelState.ShouldStart) == LevelState.ShouldStart;

            nint methodCall = check ? levels[i].OnStart : levels[i].OnUpdate;


            // Reset shouldstart state

            *(LevelState*)&check = levels[i].State & LevelState.ShouldStart;

            levels[i].State ^= *(LevelState*)&check;


            // Check, if the currently picked
            // method should be replaced by end

            check = (levels[i].State & LevelState.ShouldEnd) == LevelState.ShouldEnd;

            // Making sure, that the scene was already initialised,
            // before it ends.
            check &= (levels[i].State & LevelState.ShouldStart) != LevelState.ShouldStart;

            methodCall = check ? levels[i].OnEnd : methodCall;


            // Reset shouldend state

            *(LevelState*)&check = levels[i].State & LevelState.ShouldEnd;

            levels[i].State ^= *(LevelState*)&check;


            // Flag the level as initialised, or not,
            // depending on the outcome of the currently
            // calculated method

            if(methodCall == levels[i].OnStart)
                levels[i].State |= LevelState.IsInitialised;
            else if(methodCall == levels[i].OnEnd)
                levels[i].State ^= LevelState.IsInitialised;


            // Call the calculated method 

            if(methodCall != 0)
                ((delegate*<void>)methodCall)();


            // Call the system scedules

        }


    }


    private static void stepFixedUpdate()
    {


    }


    private static void stepRenderUpdate()
    {

    }


    public static void End()
    {

    }


    // Starts a given level

    public static void StartLevel(Type level)
    {
        // Blare an error, if the given
        // type is not a level

        if(level.GetCustomAttribute<LevelAttribute>() == null)
        {
            Logger.LogWarning(level.FullName + " is NOT a level.");

            return;
        }


        // Iterate through each field
        // in the type

        foreach(FieldInfo f in level.GetFields())
        {
            // Skip to the next
            // iteration, if the
            // current field isn't
            // static

            if(!f.IsStatic)
                continue;

            // Skip to the next iteration,
            // if the current type isn't an int

            if(f.FieldType != typeof(int))
                continue;

            // Skip to the next iteration, if
            // the field's name is not levelid

            if(!string.Equals(f.Name.ToLower(), "levelid"))
                continue;


#pragma warning disable CS8605

            levels[(int)f.GetValue(null)].State |= LevelState.ShouldStart;

#pragma warning restore


            return;
        }


        // Blare an error, that
        // no level ID field was found

        Logger.LogWarning(level.FullName + " is missing it's levelID field.");
    }


    // Ends a given level

    /*public static void EndLevel(Type level)
    {
        // Blare an error, if the given
        // type is not a level

        if(level.GetCustomAttribute<LevelAttribute>() == null)
        {
            Logger.LogWarning(level.FullName + " is NOT a level.");

            return;
        }


        // Iterate through each field
        // in the type

        foreach(FieldInfo f in level.GetFields())
        {
            // Skip to the next
            // iteration, if the
            // current field isn't
            // static

            if(!f.IsStatic)
                continue;

            // Skip to the next iteration,
            // if the current type isn't an int

            if(f.FieldType != typeof(int))
                continue;

            // Skip to the next iteration, if
            // the field's name is not levelid

            if(!string.Equals(f.Name.ToLower(), "levelid"))
                continue;


#pragma warning disable CS8605

            int lID = (int)f.GetValue(null);

#pragma warning restore


            // Set the state if the level

            levels[lID].State |= LevelState.ShouldEnd;


            // Remove the level's old entity list

            for(int i = 0; i < levels[lID].Entities.Length * LockArray<Entity>.ChunkSize; i++)
                if(levels[lID].Entities.Elements[i].Name != null)
                    DeleteEntity(i);


            for(int i = 0; i < ChunkArray.Length(levels[lID].Entities); i++)
            {
                int* ptr = ChunkArray.LockChunk(&levels[lID].Entities, i);

                for(int j = 0; j < ChunkArray.ChunkLength; j++)
                    ;


            }

            ChunkArray.Delete(&levels[lID].Entities);


            // Remove the level's old
            // archetype list

            for(int i = 0; i < levels[lID].Archetypes.Length * LockArray<Archetype>.ChunkSize; i++)
            {
                CompactArray.Delete(levels[lID].Archetypes.Elements[i].Systems);

                NativeMemory.Free(levels[lID].Archetypes.Elements[i].Data);

                NativeMemory.Free(levels[lID].Archetypes.Elements[i].ComponentMask);
            }

            ChunkArray.Delete(&levels[lID].Archetypes);


            // Reset the timescale
            // of the level

            levels[lID].TimeScale = 1;


            // Create the array to hold the entities
            // of the new level

            ChunkArray.Create(&levels[lID].Entities, 0);

            // Create the array to hold the archetypes
            // of the new level

            ChunkArray.Create(&levels[lID].Archetypes, 1);


            // Create the default
            // archetype of the level

            {
                // Get the pointer address of the
                // first zero archetype

                Archetype* zeroArchetype = &levels[lID].Archetypes.Elements[0];


                // Initialise the systems list

                zeroArchetype->Systems = CompactArray.Create<System>(0);


                // Set the data

                zeroArchetype->Data = (byte*)NativeMemory.Alloc(sizeof(long));

                *(long*)zeroArchetype->Data ^= *(long*)zeroArchetype->Data;


                // Set the mask

                int maskSize = getMaskSize();

                zeroArchetype->ComponentMask = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * maskSize));

                for(int i = 0; i < maskSize; i++)
                    zeroArchetype->ComponentMask[i] ^= zeroArchetype->ComponentMask[i];
            }


            return;
        }


        // Blare an error, that
        // no level ID field was found

        Logger.LogWarning(level.FullName + " is missing it's levelID field.");
    }*/
}


// The level attribute.
// Classes fitted with this
// attribute will be checked
// for multiple methods
// like updates, fixed updates
// initialisers aswell as finalisers

[AttributeUsage(AttributeTargets.Class)]
public sealed class LevelAttribute : Attribute
{
    // instance initialiser

    public LevelAttribute(bool isStarter)
        => this.IsStarter = isStarter;

    // Indicates, if the scene
    // should be started at
    // startup of the engine

    public readonly bool IsStarter;
}

// A level is a group
// of specific logic

unsafe struct Level
{
    // A unique ID of
    // the type of the level

    public int LevelID;


    // The state of the level

    public LevelState State;


    // The time scale local
    // to the level

    public float TimeScale;


    // A reference to the
    // start method of of
    // the level

    public nint OnStart;    

    // A reference to the
    // update method of
    // the level

    public nint OnUpdate;

    // A reference to the
    // fixed update method
    // of the level

    public nint OnBroadPhase, OnNarrowPhase;

    // A reference to the
    // render method of
    // the level

    public nint OnPreRender, OnRender, OnPostRender;

    // A reference to the
    // end method of of
    // the level

    public nint OnEnd;


    // A list of entities
    // (Chunk array)

    public Entity* Entities;


    // A list of archetypes
    // (Chunk array)

    public Archetype* Archetypes;
}


// A type that represents
// the different states a
// level can assume

[Flags]
public enum LevelState : byte
{
    // Does the scene
    // count as active?

    IsActive = 1,

    // Has the scene
    // been paused?

    IsPaused = 2,

    // Should the
    // scene get started?

    ShouldStart = 4,

    // Should the
    // scene end?

    ShouldEnd = 8,


    // Is the scene
    // already initialised?

    IsInitialised = 16,
}