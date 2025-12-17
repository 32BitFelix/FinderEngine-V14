using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.MemoryManagement;
using FinderIntrinsics;

namespace Core.LECS;


// The heart of LECS.
// All processing and manageent
// are conducted here

public unsafe static class Engine
{
    // Type initialiser

    static Engine()
    {
        // Initialise the arrays,
        // that'll hold the definitions
        // of levels, components and systems

        levels = CompactArray.Create<Level>();

        components = CompactArray.Create<Component>();

        systems = CompactArray.Create<System>();


        // Check for the levels

        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute<LevelAttribute>() == null)
                continue;

            checkForLevel(type);
        }


        // Check for the components

        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute<ComponentAttribute>() == null)
                continue;

            checkForComponent(type);
        }


        // Calculate the size of
        // the component mask

        componentMaskSize = CompactArray.Length(components) / (sizeof(ulong) * 8);

        componentMaskSize += CompactArray.Length(components) % (sizeof(ulong) * 8) == 0 ? 0 : 1;


        // Check for the systems

        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute<SystemAttribute>() == null)
                continue;

            checkForSystem(type);
        }
    }


    // A lock that counts for all
    // static fields of the engine

    private static int engineFieldLock;


    // Helper method for getting the lock
    // of the engine's fields

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void getEngineFieldLock()
    {
        {
            // Get the thrad id of the
            // currently running thread

            int MTID = Environment.CurrentManagedThreadId;


            // Try to get the engine field lock

            while(engineFieldLock != MTID)
                Interlocked.CompareExchange(ref engineFieldLock, MTID, 0);
        }
    }


    // Helper method for releasing
    // the lock of the engine's fields

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void releaseEngineFieldLock()
        => engineFieldLock = 0;


    // The ID of the currently running level

    public static int CurrentLevelID {get; private set;}


    // The ID of the currently running
    // archetype of a thread

    [ThreadStatic]
    private static int _curArchID;

    // A property for getting the
    // currently running archetype
    // ID of a thread

    public static int CurrentArchetypeID => _curArchID;



    // The collection of all
    // defined levels
    // (Compact array)

    private static Level* levels;

    // Checks, if the given type
    // classifies as a level

    private static void checkForLevel(Type l)
    {
        // Prematurely end the
        // method, if the given
        // type isn't sealed
        // and doesn't have the
        // level attribute

        if(!l.IsSealed)
            return;

        LevelAttribute? lAttrib = l.GetCustomAttribute<LevelAttribute>();

        if(lAttrib == null)
            return;


        // Check, if the level has
        // a level id field

        {
            // Clue for if the level
            // has an id field

            bool hasIDField = false;

            // See, if the level has
            // a public and static level ID
            // integer field

            foreach(FieldInfo f in l.GetFields())
            {
                // Skip to the next iteration,
                // if the name of the field
                // doesn't match the target
                // and the field itself isn't
                // public, static or an integer  

                if(!f.Name.Equals("levelid", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if(!f.IsStatic || !f.IsPublic)
                    continue;

                if(f.FieldType != typeof(int))
                    continue;


                // We've reached this point,
                // which means, that the
                // level does have a level id field.
                // Set the level id of the level,
                // while were at it

                hasIDField = !hasIDField;

                f.SetValue(null, CompactArray.Length(levels));

                break;
            }


            // Prematurely end the method,
            // because no level id field was found

            if(!hasIDField)
                return;
        }


        // Finally, we set up the
        // reference to the level


        Level nLevel = new Level();


        // Set, if the level should
        // start right after engine
        // initialisation has finished

        fixed(bool* ptr = &lAttrib.IsStarter)
            nLevel.State = *(LevelState*)ptr;


        // Get the references to
        // the level's callbacks

        foreach(MethodInfo m in l.GetMethods())
        {
            // Skip to the next iteration,
            // if the method isn't static

            if(!m.IsStatic)
                continue;


            switch(m.Name.ToLower())
            {
                case "start":
                    *(nint*)&nLevel.OnStart = m.MethodHandle.GetFunctionPointer();
                continue;

                case "update":
                    *(nint*)&nLevel.OnUpdate = m.MethodHandle.GetFunctionPointer();
                continue;

                case "broadphase":
                    *(nint*)&nLevel.OnBroadPhase = m.MethodHandle.GetFunctionPointer();
                continue;

                case "narrowphase":
                    *(nint*)&nLevel.OnNarrowPhase = m.MethodHandle.GetFunctionPointer();
                continue;

                case "prerender":
                    *(nint*)&nLevel.OnPreRender = m.MethodHandle.GetFunctionPointer();
                continue;

                case "render":
                    *(nint*)&nLevel.OnRender = m.MethodHandle.GetFunctionPointer();
                continue;

                case "postrender":
                    *(nint*)&nLevel.OnPostRender = m.MethodHandle.GetFunctionPointer();
                continue;

                case "end":
                    *(nint*)&nLevel.OnEnd = m.MethodHandle.GetFunctionPointer();
                continue;
            }
        }


        // Save the new level

        fixed(Level** ptr = &levels)
            CompactArray.Resize(ptr, CompactArray.Length(levels) + 1);

        levels[CompactArray.Length(levels) - 1] = nLevel;
    }


    // Starts the level that's
    // indexed at the given level id

    public static void StartLevel(int levelID)
    {
        // Prematurely end the method,
        // if the method, if the given
        // ID exceeds the length of the
        // levels array

        if((levelID + 1) > CompactArray.Length(levels))
            return;


        // See if the shouldend
        // or isinitialised bits
        // are true

        LevelState check = levels[levelID].State & (LevelState.ShouldEnd | LevelState.IsInitialised); 

        *(bool*)&check = check == LevelState.None;


        // If the previous calculation is true,
        // the level's state will also be set to
        // "should start", otherwise, it'll stay as is

        *(byte*)&levels[levelID].State |= (byte)((byte)LevelState.ShouldStart * *(byte*)&check);
    }


    // Ends the level that's
    // indexed at the given level id

    public static void EndLevel(int levelID)
    {
        // Prematurely end the method,
        // if the method, if the given
        // ID exceeds the length of the
        // levels array

        if((levelID + 1) > CompactArray.Length(levels))
            return;


        // See if the shouldstart
        // bit is true

        LevelState check = levels[levelID].State & LevelState.ShouldStart; 

        *(bool*)&check = check == LevelState.None;


        // If the previous calculation is true,
        // the level's state will also be set to
        // "should end", otherwise, it'll stay as is

        *(byte*)&levels[levelID].State |= (byte)((byte)LevelState.ShouldEnd * *(byte*)&check);
    }


    // The real method that starts a level

    private static void createLevel(Level* lvl)
    {
        // Allocate the arrays, for the level

        lvl->Entities.Create(1, Level.EntitiesPerChunk);

        lvl->Archetypes.Create(1, Level.ArchetypesPerChunk);


        // Set the zero entity

        {
            // Define the zero entity

            Entity zeroEnt;

            zeroEnt.Name = (char*)Marshal.StringToCoTaskMemUni("ZERO");

            zeroEnt.Parent = 0;

            zeroEnt.Children = new SplitArray<int>();

            zeroEnt.Children.Create(1, Entity.ChildrenPerChunk);

            zeroEnt.ArchetypeID = 0;

            zeroEnt.ArchetypeIndex = 0;


            // Save the zero entity

            lvl->Entities.ReadChunk(0)[0] = zeroEnt;
        }


        // Set the level's state, that it's
        // initialised and ready to run

        lvl->State ^= LevelState.ShouldStart;

        lvl->State |= LevelState.IsInitialised;


        // Set the time scale
        // of the level

        lvl->TimeScale = 1f;


        // Create the standard archetype

        Archetype* sA = lvl->Archetypes.ReadChunk(0);

        // Set the rest of the archetypes to null

        for(int i = 1; i < ChunkArray.ChunkLength; i++)
            *(nint*)&sA->Systems ^= *(nint*)&sA->Systems;


        // Define the systems collection

        sA->Systems = CompactArray.Create<System>(0);

        // Define the data collection

        sA->Data = (byte*)NativeMemory.Alloc(sizeof(int));

        *(int*)sA->Data ^= *(int*)sA->Data;

        // Define the component mask

        sA->ComponentMask = (ulong*)NativeMemory.Alloc((nuint)(sizeof(ulong) * componentMaskSize));

        for(int i = 0; i < componentMaskSize; i++)
            sA->ComponentMask[i] ^= sA->ComponentMask[i];


        // Call the start method of the level,
        // if it exists

        if(lvl->OnStart != 0)
            ((delegate*<void>)lvl->OnStart)();
    }


    // The real method that ends a level

    private static void deleteLevel(Level* lvl)
    {
        // Call the end method of the level,
        // if it exists

        if(lvl->OnEnd != 0)
            ((delegate*<void>)lvl->OnEnd)();        


        // Dispose of the entities

        for(int i = 0; i < lvl->Entities.Length(); i++)
        {
            Entity* ent = lvl->Entities.ReadChunk(i);


            for(int j = 0; j < Level.EntitiesPerChunk; j++)
            {
                if(ent[j].Name == null)
                    continue;

                Marshal.FreeCoTaskMem((nint)ent[j].Name);

                ent[j].Children.Delete();
            }
        }

        lvl->Entities.Delete();


        // Dispose of the archetypes

        for(int i = 0; i < lvl->Archetypes.Length(); i++)
        {
            Archetype* arc = lvl->Archetypes.ReadChunk(i);

            for(int j = 0; j < ChunkArray.ChunkLength; j++)
            {
                if(arc[j].Systems == null)
                    continue;

                CompactArray.Delete(arc[j].Systems);

                NativeMemory.Free(arc[j].Data);

                NativeMemory.Free(arc[j].ComponentMask);
            }
        }

        lvl->Archetypes.Delete();
    }


    // The collection of all
    // defined components
    // (Compact array)

    private static Component* components;

    // The size of a componentmask in longs

    private static readonly int componentMaskSize;

    // Checks, of the given type
    // classifies as a component

    public static void checkForComponent(Type c)
    {
        // Prematurely end the method,
        // if the given type is not
        // a value type and doesn't
        // have the component attribute

        if(!c.IsValueType)
            return;

        if(c.GetCustomAttribute<ComponentAttribute>() == null)
            return;


        // Check for the component id
        // field in the component

        {
            bool hasIDField = false;


            foreach(FieldInfo f in c.GetFields())
            {
                // Skip to the next iteration,
                // if the name of the field
                // doesn't match the target
                // and the field itself isn't
                // public, static or an integer  

                if(!f.Name.Equals("componentid", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                if(!f.IsStatic || !f.IsPublic)
                    continue;

                if(f.FieldType != typeof(int))
                    continue;


                // We've reached this point,
                // which means, that the
                // level does have a level id field.
                // Set the component id of the level,
                // while were at it

                hasIDField = !hasIDField;

                f.SetValue(null, CompactArray.Length(components));

                break;
            }


            // Prematurely end the method,
            // if the component doesn't have
            // the component ID field

            if(!hasIDField)
                return;
        }


        // Make the reference
        // to the component

        Component nComp = new Component();

        nComp.Size = Marshal.SizeOf(c);

        nComp.FieldSizes = CompactArray.Create<int>(0);


        // Get the references of the
        // component's callbacks

        foreach(MethodInfo m in c.GetMethods())
        {
            switch(m.Name.ToLower())
            {
                case "init":
                    *(nint*)&nComp.Init = m.MethodHandle.GetFunctionPointer();
                continue;

                case "fin":
                    *(nint*)&nComp.Fin = m.MethodHandle.GetFunctionPointer();
                continue;
            }
        }


        // Iterate through each
        // field in the given type,
        // to store their sizes

        foreach(FieldInfo f in c.GetFields())
        {
            if(f.IsStatic)
                continue;

            CompactArray.Resize(&nComp.FieldSizes, CompactArray.Length(nComp.FieldSizes) + 1);

            nComp.FieldSizes[CompactArray.Length(nComp.FieldSizes) - 1] = Marshal.SizeOf(f.FieldType);            
        }


        // Save the new reference
        // to the component

        fixed(Component** ptr = &components)
            CompactArray.Resize(ptr, CompactArray.Length(components) + 1);

        components[CompactArray.Length(components) - 1] = nComp;
    }
    

    // The collection of all
    // defined systems     
    // (Compact array)

    private static System* systems;

    // Checks, if the given type
    // classifies as a system

    public static void checkForSystem(Type s)
    {
        // Prematurely end the method,
        // if the type isn't sealed
        // or doesn't have the system attribute

        if(!s.IsSealed)
            return;

        SystemAttribute? sAttrib = s.GetCustomAttribute<SystemAttribute>();

        if(sAttrib == null)
            return;

        
        // Create a reference
        // to the new system

        System nSys = new System();


        // Allocate memory for the
        // componentmask of the
        // current system and
        // set it'S values to zero

        nSys.ComponentMask = (ulong*)NativeMemory.Alloc((nuint)componentMaskSize * sizeof(ulong));

        for(int i = 0; i < componentMaskSize; i++)
            nSys.ComponentMask[i] ^= nSys.ComponentMask[i];


        // Get the callbacks
        // of the system and
        // see, if the system
        // has the initialiser

        {
            bool hasInit = false;


            foreach(MethodInfo m in s.GetMethods())
            {
                // Skip to the next iteration,
                // if the current is not static

                if(!m.IsStatic)
                    continue;


                // Call the initialiser
                // of the system

                if(m.Name.ToLower() == "init" || m.ReturnType == typeof(int[]))
                {
                    int[] componentIDs = ((delegate*<int[]>)m.MethodHandle.GetFunctionPointer())();
                
                
                    for(int i = 0; i < componentIDs.Length; i++)
                        nSys.ComponentMask[componentIDs[i] / 64] |= (ulong)1 << componentIDs[i];


                    hasInit = !hasInit;

                    continue;
                }


                // Save the callbacks
                // of the system

                switch(m.Name.ToLower())
                {
                    case "update":
                        *(nint*)&nSys.OnUpdate = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "broadphase":
                        *(nint*)&nSys.OnBroadPhase = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "narrowphase":
                        *(nint*)&nSys.OnNarrowPhase = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "prerender":
                        *(nint*)&nSys.OnPreRender = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "render":
                        *(nint*)&nSys.OnRender = m.MethodHandle.GetFunctionPointer();
                    break;

                    case "postrender":
                        *(nint*)&nSys.OnPostRender = m.MethodHandle.GetFunctionPointer();
                    break; 
                }
            }


            // Prematurely end the
            // method, if the given
            // system has no initialiser

            if(!hasInit)
                return;
        }


        // Finally, save the new
        // system reference

        fixed(System** ptr = &systems)
            CompactArray.Resize(ptr, CompactArray.Length(systems) + 1);

        systems[CompactArray.Length(systems) - 1] = nSys;
    }


    // Callback for a window manager

    public static void Pulse(float dt)
    {   
        _deltaTime = dt;

        update();        


        _fixedDeltaTime += dt;

        if(FixedDeltaTime >= 1f / _fixedUpdatesPerSecond)
        {
            fixedUpdate();

            fixed(float* ptr = &_fixedDeltaTime)
                *(int*)ptr ^= *(int*)ptr;
        }


        renderUpdate();
    }


    // Returns the deltatime of the current level
    // relative to it's time scale

    public static float Deltatime => _deltaTime * levels[CurrentLevelID].TimeScale;


    // THe global deltatime

    private static float _deltaTime;


    // Called every frame

    private static void update()
    {
        // Iterate through each level

        for(int i = 0; i < CompactArray.Length(levels); i++)
        {

            // Evaulate the state score

            byte stateScore = 0;

            {
                // Check, if the level should start

                bool check = (levels[i].State & LevelState.ShouldStart) == LevelState.ShouldStart;

                stateScore += *(byte*)&check;


                // Check, if the level should end

                check = (levels[i].State & LevelState.ShouldEnd) == LevelState.ShouldEnd;

                stateScore += (byte)(*(byte*)&check * 2);
            }


            // See, which state method to
            // call based on the level's state

            {
                // Skip this step, if
                // there is no management
                // necessary

                if(stateScore == 0)
                    goto end;


                // See, if the level should be created

                bool check = stateScore == 1;

                nuint mCall = (nuint)(delegate*<Level*, void>)&createLevel * *(byte*)&check;


                // See, if the level should be deleted

                check = stateScore > 1;

                mCall += (nuint)(delegate*<Level*, void>)&deleteLevel * *(byte*)&check;


                // Call the evaluated method

                ((delegate*<Level*, void>)mCall)(&levels[i]);


                end:
                    ;
            }
            

            // Skip to the next iteration,
            // if the level 

            if((levels[i].State & LevelState.IsPaused) == LevelState.IsPaused)
                continue;

            
            // Set the ID of the current level

            CurrentLevelID = i;


            // Call the update method of the
            // level, if it's defined

            if(levels[i].OnUpdate != 0)
                ((delegate*<void>)levels[i].OnUpdate)();

            
            // Run the scedules of each archetype
        }
    }


    // Property of the fixedupdate field

    public static int FixedUpdatePerFrame
    {
        // Simply returns the fixed
        // updates per frame. Non blocking

        get => _fixedUpdatesPerSecond;

        // Sets the amount of fixed
        // updates per frame. Blocking

        set
        {
            getEngineFieldLock();

            _fixedUpdatesPerSecond = value;

            releaseEngineFieldLock();
        }
    }

    // The amounts of times a fixed update
    // processes within a second

    private static int _fixedUpdatesPerSecond = 16;

    // Returns the difference of the
    // current and last fixed update frame

    public static float FixedDeltaTime {get => _fixedDeltaTime;}

    // A counter for the next deltatime
    // and serves as a difference between
    // the current and last fixed update frame

    private static float _fixedDeltaTime;



    // Called every fixed update frame

    private static void fixedUpdate()
    {

    }


    // Called every render update frame

    private static void renderUpdate()
    {

    }


    // Ending callback for a window manager

    public static void End()
    {
        // Dispose of the levels

        for(int i = 0; i < CompactArray.Length(levels); i++)
        {
            // End the level, if it is
            // initialised to begin with

            if((levels[i].State & LevelState.IsInitialised) != LevelState.IsInitialised)
                continue;


            // Save the level id of the
            // currnetly run level

            CurrentLevelID = i;


            // Call the method
            // responsible for
            // deleteing levels

            deleteLevel(&levels[i]);
        }
    }


    // Creates the specified amount
    // of entities and saves their
    // references to the given array

    public static void CreateEntities(int* entities, int amount)
    {
        // Iterate through each given slot

        for(int e = 0; e < amount; e++)
        {
            // Define the new entity

            Entity nEntity;

            nEntity.Name = (char*)Marshal.StringToCoTaskMemUni("NONAME");

            nEntity.Parent = 0;

            nEntity.Children = new SplitArray<int>();

            nEntity.Children.Create(0);

            nEntity.ArchetypeID = 0;

            nEntity.ArchetypeIndex = 0;


            // Retry point to find
            // a free slot to save
            // the new entity at

            retry:


            // Try to find a new slot
            // to save the new entity at

            for(int i = 0; i < levels[CurrentLevelID].Entities.Length(); i++)
            {
                // Get a reference to the
                // current chunk

                Entity* ent = levels[CurrentLevelID].Entities.LockChunk(i); 


                // Iterate through each
                // element in the current chunk

                for(int j = 0; j < Level.EntitiesPerChunk; j++)
                {
                    // Skip to the
                    // next iteration,
                    // if the current
                    // is not free

                    if(ent[j].Name != null)
                        continue;

                    // We have found a free slot,
                    // save the new entity to the
                    // current slot at the index

                    ent[j] = nEntity;

                    // Save the reference
                    // to the new entity

                    entities[e] = i * Level.EntitiesPerChunk + j;


                    levels[CurrentLevelID].Entities.ReleaseChunk(i);

                    // We have saved the
                    // current entity to a slot,
                    // time to get to the next
                    // iteration of the topmost loop

                    goto nextEnt;
                }


                // Release the lock
                // of the current chunk

                levels[CurrentLevelID].Entities.ReleaseChunk(i);
            }


            // Resize the array, to
            // fit the new entity

            levels[CurrentLevelID].Entities.Resize(levels[CurrentLevelID].Entities.Length() + 1);


            // The entities array has been
            // resized to fit new entities,
            // the search will begin once more

            goto retry;


            // Jumping point
            // to the next iteration
            // with the topmost loop

            nextEnt:;
        }
    }


    // Deletes the entities
    // that correspond to the
    // given list of references

    public static void DeleteEntities(int* entities, int amount)
    {
        // Iterate through each
        // entity reference

        for(int e = 0; e < amount; e++)
        {
            // Skip the current
            // iteration, if it
            // isn't valid

            if(entities[e] == 0)
                continue;


            Console.WriteLine("ENT " + entities[e]);


            int chunkIndex = entities[e] / Level.EntitiesPerChunk;

            int index = entities[e] % Level.EntitiesPerChunk;


            // Skip the entity,
            // if it isn't valid

            if(levels[CurrentLevelID].Entities.ReadChunk(chunkIndex)[index].Name == null)
                continue;


            // Call the finalisers
            // of the entity's components


            // Remove references of the
            // entity within it's archetype


            // Unbind the entity from
            // it's parent

            {
                // Skip this step altogether,
                // if the entity doesn't
                // have a parent

                if(levels[CurrentLevelID].Entities.ReadChunk(chunkIndex)[index].Parent == 0)
                    goto skip;


                // Evaulate the index of the chunk,
                // that the parent resides in, aswell
                // as the index at which the parent is
                // within it's chunk

                int parentChunkIndex = levels[CurrentLevelID].Entities.ReadChunk(chunkIndex)[index].Parent / Level.EntitiesPerChunk;

                int parentIndex = levels[CurrentLevelID].Entities.ReadChunk(chunkIndex)[index].Parent % Level.EntitiesPerChunk;
                

                // Get the reference to the
                // parent's chunk and lock it

                Entity* par = levels[CurrentLevelID].Entities.LockChunk(parentChunkIndex);


                // Search for the entity's reference
                // in the parent's child list

                for(int i = 0; i < par[parentIndex].Children.Length(); i++)
                {
                    int* children = par[parentIndex].Children.LockChunk(i);


                    for(int j = 0; j < Entity.ChildrenPerChunk; j++)
                    {
                        if(children[j] != entities[e])
                            continue;

                        children[j] ^= children[j];

                        par[parentIndex].Children.ReleaseChunk(i);

                        goto success;
                    }


                    par[parentIndex].Children.ReleaseChunk(i);
                }


                // Jumping point

                success:


                // Release the lock of the
                // parent's chunk

                levels[CurrentLevelID].Entities.ReleaseChunk(parentChunkIndex);


                // Jumping point to
                // skipping this step

                skip:;
            }


            // Remove the children

            {
                // Get a readonly reference
                // of the current entity

                Entity* ent = levels[CurrentLevelID].Entities.ReadChunk(chunkIndex);

                ent = &ent[index];


                // Remove the children of the entity

                for(int i = 0; i < ent->Children.Length(); i++)
                {
                    int* children = ent->Children.LockChunk(i);        


                    DeleteEntities(children, Entity.ChildrenPerChunk);


                    ent->Children.ReleaseChunk(i);
                }
            }


            // Remove the entity itself

            {
                // Lock the chunk, that the
                // current entity was stored in

                Entity* ent = levels[CurrentLevelID].Entities.LockChunk(chunkIndex);


                // Free the array, that
                // held references to the
                // current entity's children

                ent[index].Children.Delete();


                // Free the array, that
                // held the current entity's name

                Marshal.FreeCoTaskMem((nint)ent[index].Name);                

                ent[index].Name = null;


                // Release the chunk,
                // that the currently entity
                // was stored in

                levels[CurrentLevelID].Entities.ReleaseChunk(chunkIndex);
            }


            // Release the lock of
            // the chunk, that the
            // entity was in

            levels[CurrentLevelID].Entities.ReleaseChunk(chunkIndex);
        }
    }


    // Binds the given array
    // of children to the given parent    

    public static void BindChildren(int parent, int* children, int amount)
    {
        // Set the parent reference of the children

        for(int c = 0; c < amount; c++)
        {
            int childChunkIndex = children[c] / Level.EntitiesPerChunk;

            int childIndex = children[c] % Level.EntitiesPerChunk;


            Entity* chiChunk = levels[CurrentLevelID].Entities.LockChunk(childChunkIndex);

            chiChunk[childIndex].Parent = parent; 

            levels[CurrentLevelID].Entities.ReleaseChunk(childChunkIndex);
        }


        // Get a reference to the parent
        // and lock the chunk it's in

        int parentChunkIndex = parent / Level.EntitiesPerChunk;

        int parentIndex = parent % Level.EntitiesPerChunk;


        Entity* par = levels[CurrentLevelID].Entities.LockChunk(parentChunkIndex);


        // Iterate through each given child

        for(int c = 0; c < amount; c++)
        {
            // Retry point for finding
            // a free slot in the parent's
            // child list

            retry:


            // Try to find a free slot
            // from the parent's child list

            for(int i = 0; i < par[parentIndex].Children.Length(); i++)
            {
                // Get the reference to the
                // current chunk of children
                // and lock it

                int* chi = par[parentIndex].Children.LockChunk(i);

                // Iterate through each
                // element in the current slot

                for(int j = 0; j < Entity.ChildrenPerChunk; j++)
                {
                    // Skip to the next iteration,
                    // if the current index is not
                    // free to use

                    if(chi[j] != 0)
                        continue;

                    chi[j] = children[c];

                    par[parentIndex].Children.ReleaseChunk(i);

                    goto next;
                }

                // Release the currently
                // locked chunk

                par[parentIndex].Children.ReleaseChunk(i);
            }


            // Resize the array,
            // to hold the new child

            par[parentIndex].Children.Resize(par[parentIndex].Children.Length() + 1);

            // Now try again at
            // finding a free index

            goto retry;


            // Jumpint point
            // to the next
            // child in the list

            next:;
        }

        // Release the chunk,
        // that stores the parent

        levels[CurrentLevelID].Entities.ReleaseChunk(parentChunkIndex);
    }


    // Unbinds the given array
    // of children from the given parent

    public static void UnbindChildren(int parent, int* children, int amount)
    {
        // Remove the parent reference of the children

        for(int c = 0; c < amount; c++)
        {
            int childChunkIndex = children[c] / Level.EntitiesPerChunk;

            int childIndex = children[c] % Level.EntitiesPerChunk;


            Entity* chiChunk = levels[CurrentLevelID].Entities.LockChunk(childChunkIndex);

            chiChunk[childIndex].Parent ^= chiChunk[childIndex].Parent; 

            levels[CurrentLevelID].Entities.ReleaseChunk(childChunkIndex);
        }


        // Get a reference to the parent
        // and lock the chunk it's in

        int parentChunkIndex = parent / Level.EntitiesPerChunk;

        int parentIndex = parent % Level.EntitiesPerChunk;


        Entity* par = levels[CurrentLevelID].Entities.LockChunk(parentChunkIndex);


        // Iterate through each given child

        for(int c = 0; c < amount; c++)
        {
            // Try to find a free slot
            // from the parent's child list

            for(int i = 0; i < par[parentIndex].Children.Length(); i++)
            {
                // Get the reference to the
                // current chunk of children
                // and lock it

                int* chi = par[parentIndex].Children.LockChunk(i);

                // Iterate through each
                // element in the current slot

                for(int j = 0; j < Entity.ChildrenPerChunk; j++)
                {
                    // Skip to the next iteration,
                    // if the current index is not
                    // the reference to remove

                    if(chi[j] != children[c])
                        continue;

                    chi[j] = 0;

                    par[parentIndex].Children.ReleaseChunk(i);

                    goto next;
                }

                // Release the currently
                // locked chunk

                par[parentIndex].Children.ReleaseChunk(i);
            }


            // Jumping point
            // to the next
            // child in the list

            next:;
        }

        // Release the chunk,
        // that stored 

        levels[CurrentLevelID].Entities.ReleaseChunk(parentChunkIndex);
    }


    // Returns a copy of the
    // given parent's child list

    public static int[] ShowChildren(int parent)
    {
        // Get a reference to the parent
        // and lock the chunk it's in

        int parentChunkIndex = parent / Level.EntitiesPerChunk;

        int parentIndex = parent % Level.EntitiesPerChunk;


        Entity* par = levels[CurrentLevelID].Entities.LockChunk(parentChunkIndex);


        // Allocate the managed array
        // to copy the children to

        int[] chiTo = new int[par[parentIndex].Children.Length() * Entity.ChildrenPerChunk];


        // Copy the references of
        // the parent's children
        // to the managed array

        for(int i = 0; i < par[parentIndex].Children.Length(); i++)
        {
            int* chi = par[parentIndex].Children.LockChunk(i);

            for(int j = 0; j < Entity.ChildrenPerChunk; j++)
                chiTo[i * Entity.ChildrenPerChunk + j] = chi[j];

            par[parentIndex].Children.ReleaseChunk(i);
        }


        // Release the chunk, that
        // the parent resides in
        
        levels[CurrentLevelID].Entities.ReleaseChunk(parentChunkIndex);

        return chiTo;
    }


    // Returns the reference
    // of the parent bound
    // to the given child

    public static int ShowParent(int child)
    {
        // Evaluate the index of the
        // child's chunk and it's
        // position within it

        int entityChunkIndex = child / Level.EntitiesPerChunk;

        int entityIndex = child % Level.EntitiesPerChunk;


        // Cache the id of the
        // child's current parent
        // and return it

        Entity* chiChunk = levels[CurrentLevelID].Entities.LockChunk(entityChunkIndex);

        int toReturn = chiChunk[entityIndex].Parent;

        levels[CurrentLevelID].Entities.ReleaseChunk(entityChunkIndex);


        // Return the cached value

        return toReturn;
    }


    // Adds the given component type
    // to the given entity

    public static void AddComponents(int entityID, int* componentIDs, int amount)
    {
        // Calculate the index of the
        // given entity's chunk aswell
        // as it's position within it

        int entityChunkIndex = entityID / Level.EntitiesPerChunk;

        int entityIndex = entityID % Level.EntitiesPerChunk;


        // Lock the chunk, that the
        // given entity is within

        Entity* ent = levels[CurrentLevelID].Entities.LockChunk(entityChunkIndex);


        // Allocate an array, that'll
        // hold the mask of the new
        // possible archetype

        ulong* newComponentMask = stackalloc ulong[componentMaskSize];


        // Get the reference of the given
        // entity's current archetype

        int cArchetypeChunkIndex = ent[entityIndex].ArchetypeID / Level.ArchetypesPerChunk;

        int cArchetypeIndex = ent[entityIndex].ArchetypeID % Level.ArchetypesPerChunk;

        Archetype* cArch = levels[CurrentLevelID].Archetypes.ReadChunk(cArchetypeChunkIndex);


        // Copy the entity's current
        // archetype's component mask

        for(int i = 0; i < componentMaskSize; i++)
            newComponentMask[i] = cArch[cArchetypeIndex].ComponentMask[i];
        

        // Now set the flags of the
        // new components to the
        // component mask of the
        // possible archetype

        for(int i = 0; i < amount; i++)
            newComponentMask[componentIDs[i] / 64] |= (ulong)1 << (componentIDs[i] % 64);


        // Now search for the archetype,
        // that has a component mask fitting
        // to the previously evaluated component mask

        int fitArchetypeChunkIndex = 0;

        int fitArchetypeIndex = 0;

        // Iterate through each chunk,
        // that stores the archetypes
        // in the currently running level

        for(int i = 0; i < levels[CurrentLevelID].Archetypes.Length(); i++)
        {
            // Iterate through each archetype
            // in the current chunk

            Archetype* arch = levels[CurrentLevelID].Archetypes.ReadChunk(i);

            for(int j = 0; j < Level.ArchetypesPerChunk; j++)
            {
                // Compare the evaluated component mask
                // and the component mask of the
                // current acrehytpe. Skip to the
                // next archetype, if the masks
                // don't compare

                for(int c = 0; c < componentMaskSize; c++)
                    if(newComponentMask[i] != arch->ComponentMask[i])
                        goto toNext;

                
                // Save the indices of the
                // current archetype's chunk,
                // aswell as it's index within
                // it's chunk

                fitArchetypeChunkIndex = i;

                fitArchetypeIndex = j;


                // End the search

                goto found;


                // Jumping point for
                // skipping to the
                // next archetype

                toNext:;
            }
        }


        // At this point, we haven't
        // found a fitting archetype.
        // We need to make one

        createArchetype(newComponentMask, &fitArchetypeChunkIndex, &fitArchetypeIndex);
        

        // Jumping point to
        // when a fitting
        // archetype has been found

        found:;



        /*// Get a reference to the
        // currently running level's
        // entity list

        Entity** ents = &levels[CurrentLevelID].Entities;


        // Calculate the index of the
        // given entity's chunk aswell
        // as it's position within it

        int entityChunkIndex = entityID / ChunkArray.ChunkLength;

        int entityIndex = entityID % ChunkArray.ChunkLength;

        // Get the reference
        // of the given entity's chunk 

        Entity* ent = ChunkArray.LockChunk(ents, entityChunkIndex);

        // Next, get the reference
        // of the entity itself

        ent = &ent[entityIndex];


        // Allocate a mask, that'll
        // represent the mask of the
        // new archetype the entity
        // will belong to

        ulong* newComponentMask = stackalloc ulong[componentMaskSize];


        // Get the reference
        // of the currently running
        // level's archetype list

        Archetype** archs = &levels[CurrentLevelID].Archetypes;

        int archetypeChunkIndex = ent->ArchetypeIndex / ChunkArray.ChunkLength;

        int archetypeIndex = ent->ArchetypeIndex % ChunkArray.ChunkLength;


        // Get the reference of the
        // chunk that the entity's
        // current archetype resides in

        Archetype* arch = ChunkArray.ReadChunk(archs, archetypeChunkIndex);

        // Get the reference of the
        // current archetype´

        arch = &arch[archetypeIndex];


        // Copy the entity's current
        // archetype's component mask
        // to the new component mask

        for(int i = 0; i < componentMaskSize; i++)
            newComponentMask[i] = arch->ComponentMask[i];

        // Add the new components'
        // flag to the new component mask

        for(int i = 0; i < amount; i++)
            newComponentMask[componentIDs[i] / sizeof(ulong)] |= (ulong)1 << componentIDs[i] % sizeof(ulong);


        // Copy the reference of the
        // entity and it's component
        // data from the current
        // archetype to the new archetype

        {
            // Search for the archetype,
            // that fits the new component mask

            int newArchetypeChunkIndex = -1;

            int newArchetypeIndex = -1;


            // Iterate through each
            // archetype chunk

            for(int i = 0; i < ChunkArray.Length(*archs); i++)
            {
                // Get the referenc of the
                // current archetype chunk

                Archetype* nArch = ChunkArray.ReadChunk(archs, i);
                

                // Iterate through archetype
                // in the chunk

                for(int j = 0; j < ChunkArray.ChunkLength; j++)
                {
                    // Compare the evaluated component mask
                    // and the component mask of the current iteration

                    for(int m = 0; m < componentMaskSize; m++)
                        if(newComponentMask[m] != nArch->ComponentMask[m])
                            goto nextArchetype;


                    // Save the index of the
                    // fitting archetype

                    newArchetypeChunkIndex = i;

                    newArchetypeIndex = j;

                    goto endSearch;


                    // Jumping point
                    // to jumpin to
                    // the next archetype

                    nextArchetype:
                        ;
                }
            }

            // Jumping point for
            // ending the archetype search

            endSearch:
            

            // Create a new archetype,
            // if no fitting one has
            // been found

            if(newArchetypeIndex == -1)
            {

                // Create new archetype

            }
        }



        // Release the lock of
        // the given entity's chunk

        ChunkArray.ReleaseChunk(ents, entityChunkIndex);*/
    }


    // A helper method for creating
    // a new archetype

    private static void createArchetype(ulong* componentMask, int* chunkIndex, int* index)
    {
        // The new archetype to create

        Archetype nArchetype;

        // Make sure to show,
        // that the archetype
        // isn't being processed
        // right now

        nArchetype.Processed = 0;


        // Allocate and save the new
        // archetype's component mask

        nArchetype.ComponentMask = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * (nuint)componentMaskSize, sizeof(ulong));

        for(int i = 0; i < componentMaskSize; i++)
            nArchetype.ComponentMask[i] = componentMask[i];


        // Set the stride for each
        // component chunk

        nArchetype.ComponentChunkStride = sizeof(int);

        for(int i = 0; i < componentMaskSize; i++)
        {
            // A mask that stores
            // the flags to ignore
            // for the coming iterations
            // of the following loop

            ulong ignored = 0;

            for(int j = 0; j < BitOperations.PopCount(componentMask[i]); j++)
            {
                // Evaluate the index of the
                // current mask

                int componentIndex = BitOperations.Log2((componentMask[i] ^ ignored) & 0-(componentMask[i] ^ ignored)) + i * 64;


                // Iterate through each field size
                // given in the component at the
                // previously evaluated index

                for(int f = 0; f < CompactArray.Length(components[componentIndex].FieldSizes); f++)
                    nArchetype.ComponentChunkStride += components[componentIndex].FieldSizes[f];

                
                // Add the currently evaluated
                // component to the ignore mask

                ignored |= (ulong)1 << componentIndex;
            }

            ignored ^= ignored;
        }

        // Multiply the stride by the
        // amount of floats, that can
        // be processed at once by the
        // SIMD processors

        nArchetype.ComponentChunkStride *= *FloatFast.OpCount;


        // Preallocate the array,
        // that holds the systems
        // compatible with the
        // given type of archetype

        nArchetype.Systems = CompactArray.Create<System>(0);

        // Now try to find the systems,
        // that fit the archetype

        {
            ulong* lesserStencil = null;


            // Find step

            for(int i = 0; i < CompactArray.Length(systems); i++)
            {
                // Compare the masks for
                // any sort of overlap
            }


            // Skip the exclude step,
            // if there is no lesser
            // stencil to speak of

            if(lesserStencil == null)
                goto skipExlude;


            // Exclude step



            skipExlude:;
        }



        // Preallocate the data array
        // of the archetype with enough
        // memory allocated, to save it's length

        nArchetype.Data = (byte*)NativeMemory.Alloc(sizeof(int));

        *(int*)nArchetype.Data ^= *(int*)nArchetype.Data;



    }


    public static void RemoveComponents(int entityID, int* componentIDs, int amount)
    {



    }


    public static void* GetComponent(int entityID, int componentID)
    {


        return null;
    }


    public static void GetFieldOffset()
    {

    }


    public static void GetComponentOffset()
    {
        
    }
}


// Every class or struct fit with
// this attribute will be treated
// as a level

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class LevelAttribute : Attribute
{
    // Instance initialiser

    public LevelAttribute(bool IsStarter)
        => this.IsStarter = IsStarter;


    // A clue to see if the level
    // should immediately after
    // the engine kicks off

    public readonly bool IsStarter;
}


// A level is a self contained
// space of simulation, fit with
// it's own and custom behvaiour

public unsafe struct Level
{
    // Indicates at which
    // state the level
    // currently is

    public LevelState State;


    // The scale at which
    // the deltatime progressed
    // for the level

    public float TimeScale;


    // Called every time the
    // level gets initialised

    public nuint OnStart;

    // Called every frame

    public nuint OnUpdate;

    // Broad phase is alway called
    // before narrow phase.
    // Both are called every physics frame

    public nuint OnBroadPhase, OnNarrowPhase;

    // Pre render is called before render,
    // render is called before post render.
    // All are called every render frame

    public nuint OnPreRender, OnRender, OnPostRender;

    // Called every time the
    // level gets finalised

    public nuint OnEnd;


    // The collection of
    // entities existing
    // in this level

    public SplitArray<Entity> Entities;

    // A helper constant for keeping
    // track of the total amount of entities
    // within a chunk 
    public const byte EntitiesPerChunk = 64;


    // The collection of
    // archetypes defined
    // within this level

    public SplitArray<Archetype> Archetypes;

    // A helper constant for keeping
    // track of the total amount of archetypes
    // within a chunk
    public const byte ArchetypesPerChunk = 16;
}


// Defines the state
// of a level

[Flags]
public enum LevelState : byte
{
    // There currently
    // is no state
    // on the level

    None = 0,


    // The level should
    // be initialised

    ShouldStart = 1,

    // The level should
    // be finalised

    ShouldEnd = 2,


    // The level's
    // simulations will
    // be paused, except
    // for rendering

    IsPaused = 4,


    // The level has
    // been initialised
    // for use

    IsInitialised = 8,
}
