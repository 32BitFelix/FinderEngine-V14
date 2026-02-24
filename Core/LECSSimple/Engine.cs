
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Core.MemoryManagement;
using FinderIntrinsics;

namespace Core.LECSSimple;


// The heart of finder engine.
// All relevant memory management
// and update loop stuff happen here

public unsafe static class Finder
{

    // Type initialiser
    static Finder()
    {
        // Initialise the arrays,
        // that'll hold the definitions
        // of levels, components and systems

        levels = CompactArray.Create<Level>();

        components = CompactArray.Create<Component>();

        systems = CompactArray.Create<System>();


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


        // Check for the levels

        foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if(type.GetCustomAttribute<LevelAttribute>() == null)
                continue;

            checkForLevel(type);
        }
    }


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
                    *(nint*)&nLevel.Start = m.MethodHandle.GetFunctionPointer();
                continue;

                case "update":
                    *(nint*)&nLevel.Update = m.MethodHandle.GetFunctionPointer();
                continue;

                case "broadphase":
                    *(nint*)&nLevel.BroadPhase = m.MethodHandle.GetFunctionPointer();
                continue;

                case "narrowphase":
                    *(nint*)&nLevel.NarrowPhase = m.MethodHandle.GetFunctionPointer();
                continue;

                case "prerender":
                    *(nint*)&nLevel.PreRender = m.MethodHandle.GetFunctionPointer();
                continue;

                case "render":
                    *(nint*)&nLevel.Render = m.MethodHandle.GetFunctionPointer();
                continue;

                case "postrender":
                    *(nint*)&nLevel.PostRender = m.MethodHandle.GetFunctionPointer();
                continue;

                case "end":
                    *(nint*)&nLevel.End = m.MethodHandle.GetFunctionPointer();
                continue;
            }
        }


        // Save the new level

        fixed(Level** ptr = &levels)
            CompactArray.Resize(ptr, CompactArray.Length(levels) + 1);

        levels[CompactArray.Length(levels) - 1] = nLevel;


        // Initialise the level,
        // if it is a starter

        if(lAttrib.IsStarter)
            StartLevel(CompactArray.Length(levels) - 1);
    }


    // Starts the level that's
    // indexed at the given level id

    public static void StartLevel(int levelID)
    {
        // Prematurely end the method,
        // if the index of the given
        // ID exceeds the length of the
        // levels array

        if((levelID + 1) > CompactArray.Length(levels))
            return;


        // Prematurely end the method,
        // if the level is already initialised

        if((levels[levelID].State & LevelState.IsInitialised) == LevelState.IsInitialised)
            return;


        // Save the ID of the level,
        // that called this method

        int previousLevelID = currentLevelID;

        // Set the ID of the currently
        // running level to the ID of the
        // given level

        currentLevelID = levelID;


        // Set the time scale of the level

        levels[levelID].TimeScale = 1;


        // Initilise the arrays to hold the
        // entities and archetypes

        levels[levelID].Entities.Create();

        levels[levelID].Archetypes.Create();


        // Allocate the space for the
        // default entity

        levels[levelID].Entities.AllocateArray(0);


        // Define the zero entity

        {
            Entity* zeroEnt = levels[levelID].Entities.GetArray(0);

            zeroEnt->Name = Marshal.StringToCoTaskMemUni("ZERO");

            zeroEnt->Parent ^= zeroEnt->Parent;

            zeroEnt->Children = CompactArray.Create<int>(0);

            zeroEnt->ArchetypeID ^= zeroEnt->ArchetypeID;

            zeroEnt->Index ^= zeroEnt->Index;
        }


        // Define the archetype holding no component types

        {
            // Create the component mask to base
            // the new archetype around

            ulong* mask = (ulong*)NativeMemory.AlignedAlloc((nuint)(sizeof(ulong) * componentMaskSize), sizeof(ulong));

            for(int i = componentMaskSize; i > -1; i--)
                mask[i] ^= mask[i];

            // Create the new archetype

            int temp = 0;
            createArchetype(mask, &temp);

            // Free the memory of the component mask

            NativeMemory.AlignedFree(mask);


            // Allocate some memory for the
            // zero entity of the componentless archetype

            levels[levelID].Archetypes.GetArray(0)->collection.AllocateArray(sizeof(int), 0);
        }


        // Set the state of the level

        levels[levelID].State = LevelState.IsInitialised;


        // Call the initialiser of the level

        if(levels[levelID].Start != 0)
            ((delegate*<void>)levels[levelID].Start)();


        // Set the level ID back to the
        // level, that called this method

        currentLevelID = previousLevelID;
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


        // Prematurely end the method,
        // if the level has already ended

        if((levels[levelID].State & LevelState.IsInitialised) != LevelState.IsInitialised)
            return;


        // Save the level ID of the
        // level that called this method
        // and set the level to end
        // as the current level

        int previousLevelID = currentLevelID;

        currentLevelID = levelID;


        // Call the finaliser of the level,
        // if it is defined

        if(levels[levelID].End != 0)
            ((delegate*<void>)levels[levelID].End)();


        // Dispose the entities of the level

        for(int i = 0; i < 32; i++)
        {
            // Get the address of the
            // current array

            Entity* array = levels[levelID].Entities.GetArray(i);


            if(array == null)
                break;


            // Iterate through each element of the
            // current array

            for(int j = (1 << i) - 1; j > -1; j--)
            {
                if(array[j].Name == 0)
                    continue;

                int eID = (1 << i) | j;

                DeleteEntities(&eID, 1);
            }
        }

        
        // Dispose the archetypes of the level

        for(int i = 0; i < 32; i++)
        {
            // Get the address of the
            // current array

            Archetype* array = levels[levelID].Archetypes.GetArray(i);


            if(array == null)
                break;


            // Iterate through each element of the
            // current array

            for(int j = (1 << i) - 1; j > -1; j--)
            {
                int aID = (1 << i) | j;

                if(array[j].Systems == null)
                    continue;

                deleteArchetype(aID);
            }
        }


        // Reset the state of the level

        levels[levelID].State ^= levels[levelID].State;


        // Set the current level ID
        // back to the ID of the level,
        // that called this method

        currentLevelID = previousLevelID;
    }


    // The collection of all
    // defined components
    // (Compact array)

    private static Component* components;

    // The size of a componentmask in longs

    public static readonly int componentMaskSize;

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


        // Save the size of the component

        nComp.Size = Marshal.SizeOf(c);


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


        // Save the state of the system

        nSys.State = sAttrib.State;


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

                if(m.Name.Equals("init", StringComparison.CurrentCultureIgnoreCase) || m.ReturnType == typeof(int[]))
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
                        *(nint*)&nSys.Update = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "broadphase":
                        *(nint*)&nSys.BroadPhase = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "narrowphase":
                        *(nint*)&nSys.NarrowPhase = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "prerender":
                        *(nint*)&nSys.PreRender = m.MethodHandle.GetFunctionPointer();
                    continue;

                    case "render":
                        *(nint*)&nSys.Render = m.MethodHandle.GetFunctionPointer();
                    break;

                    case "postrender":
                        *(nint*)&nSys.PostRender = m.MethodHandle.GetFunctionPointer();
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


    // The ID of the currently running level

    private static int currentLevelID;


    /// <summary>
    /// The time it takes between each pulse
    /// of the engine.
    /// </summary>

    public static float DeltaTime;


    // TODO: Improve system sceduling

    // The start of an update iteration

    public static void Pulse(float dt)
    {
        // The level events to call

        nuint* levelScedule = stackalloc nuint[6];

        // The amount of level events,
        // that have been saved

        byte levelSceduleLength = 0;


        // Set the deltatime of the engine

        DeltaTime = dt;

        // Add the given delta time
        // to the fixed update counter

        _fixedUpdateCounter += dt;


        // Indicates, if it is time,
        // to call the fixed update

        bool fixedCheck = false;

        // See, if it is time, to
        // call the fixed update

        if(_fixedUpdateCounter >= FixedDeltaTime)
        {
            // Evaluate the fixed update compensation

            _fixedUpdateCompensation = _fixedUpdateCounter % (1f / FixedUpdatesPerSecond);


            // Reset the fixed update counter

            fixed(float* ptr = &_fixedUpdateCounter)
                *(int*)ptr ^= *(int*)ptr;


            // Make sure, that the coming levels
            // know, that it's time to call fixed updates

            fixedCheck = !fixedCheck;
        }


        // Iterate through each level

        for(int i = CompactArray.Length(levels) - 1; i > -1; i--)
        {
            // Skip this iteration,
            // if it isn't initialised
            // or is paused from updating

            {
                byte check = (byte)(levels[i].State & (LevelState.IsPaused | LevelState.IsInitialised));

                if(check != (byte)LevelState.IsInitialised)
                    continue;
            }


            // Create a scedule for the level

            {
                // Update check

                bool check = levels[i].Update != 0;

                *(byte*)&check &= 1;

                levelScedule[0] = levels[i].Update * *(byte*)&check;

                levelSceduleLength += *(byte*)&check;


                // Broad phase check

                check = (levels[i].BroadPhase != 0) && fixedCheck;

                *(byte*)&check &= 1;

                levelScedule[levelSceduleLength] = levels[i].BroadPhase * *(byte*)&check;

                levelSceduleLength += *(byte*)&check;


                // Narrow phase check

                check = (levels[i].NarrowPhase != 0) && fixedCheck;

                *(byte*)&check &= 1;

                levelScedule[levelSceduleLength] = levels[i].NarrowPhase * *(byte*)&check;

                levelSceduleLength += *(byte*)&check;


                // Pre render check

                check = levels[i].PreRender != 0;

                *(byte*)&check &= 1;

                levelScedule[levelSceduleLength] = levels[i].PreRender * *(byte*)&check;

                levelSceduleLength += *(byte*)&check;


                // Render check

                check = levels[i].Render != 0;

                *(byte*)&check &= 1;

                levelScedule[levelSceduleLength] = levels[i].Render * *(byte*)&check;

                levelSceduleLength += *(byte*)&check;


                // Post render check

                check = levels[i].PostRender != 0;

                *(byte*)&check &= 1;

                levelScedule[levelSceduleLength] = levels[i].PostRender * *(byte*)&check;

                levelSceduleLength += *(byte*)&check;
            }

            
            // Iterate through each evaluated level event

            for(byte u = 0; u < levelSceduleLength; u++)
                ((delegate*<void>)levelScedule[u])();


            // Iterate through each archetype
            
            for(int a = levels[i].Archetypes.GetLength(); a > 0; a--)
            {
                // Get a pointer reference of the
                // current archetype

                Archetype* arch = levels[i].Archetypes.GetElement(a);


                // Skip the current iteration,
                // if it isn't valid

                if(arch->Systems == null)
                    continue;


                // Build the iterator

                ArchetypeIterator iterator = new ArchetypeIterator
                {
                    // Save the reference of the
                    // current archetype to the iterator

                    Archetype = arch
                };


                // Initialise the component offsets array

                {
                    int popcnt = 0;

                    for(int c = 0; c < componentMaskSize; c++)
                        popcnt += BitOperations.PopCount(arch->ComponentMask[c]);

                    iterator.ComponentOffsets = CompactArray.Create<int>(popcnt);
                }

                // Evaluate the component offsets

                {
                    int offset = sizeof(int);

                    int ind = 0;

                    for(int c = 0; c < componentMaskSize; c++)
                    {
                        ulong ignoreMask = 0;


                        for(int b = BitOperations.PopCount(arch->ComponentMask[c]) - 1; b > -1; b--)
                        {
                            ulong componentID = (arch->ComponentMask[c] ^ ignoreMask) & 0-(arch->ComponentMask[c] ^ ignoreMask);

                            ignoreMask |= componentID;

                            componentID = (ulong)(BitOperations.Log2(componentID) + c * 64);


                            iterator.ComponentOffsets[ind++] = offset;


                            offset += components[componentID].Size;
                        }
                    }
                }


                // Create the system scedule

                nuint* sysSchedule = (nuint*)NativeMemory.Alloc((nuint)(sizeof(nuint) * 6 * CompactArray.Length((nuint*)arch->Systems)));

                int sysScheduleLen = 0;

                for(int s = CompactArray.Length((nuint*)arch->Systems) - 1; s > -1; s--)
                {
                    // Update check

                    bool check = arch->Systems[s]->Update != null;

                    *(byte*)&check &= 1;

                    sysSchedule[sysScheduleLen] = (nuint)arch->Systems[s]->Update * *(byte*)&check;

                    sysScheduleLen += *(byte*)&check;


                    // Broad phase check

                    check = arch->Systems[s]->BroadPhase != null && fixedCheck;

                    *(byte*)&check &= 1;

                    sysSchedule[sysScheduleLen] = (nuint)arch->Systems[s]->BroadPhase * *(byte*)&check;

                    sysScheduleLen += *(byte*)&check;


                    // Narrow phase check

                    check = arch->Systems[s]->NarrowPhase != null && fixedCheck;

                    *(byte*)&check &= 1;

                    sysSchedule[sysScheduleLen] = (nuint)arch->Systems[s]->NarrowPhase * *(byte*)&check;

                    sysScheduleLen += *(byte*)&check;


                    // Pre render check

                    check = arch->Systems[s]->PreRender != null;

                    *(byte*)&check &= 1;

                    sysSchedule[sysScheduleLen] = (nuint)arch->Systems[s]->PreRender * *(byte*)&check;

                    sysScheduleLen += *(byte*)&check;


                    // Render check

                    check = arch->Systems[s]->Render != null;

                    *(byte*)&check &= 1;

                    sysSchedule[sysScheduleLen] = (nuint)arch->Systems[s]->Render * *(byte*)&check;

                    sysScheduleLen += *(byte*)&check;


                    // Post render check

                    check = arch->Systems[s]->PostRender != null;

                    *(byte*)&check &= 1;

                    sysSchedule[sysScheduleLen] = (nuint)arch->Systems[s]->PostRender * *(byte*)&check;

                    sysScheduleLen += *(byte*)&check;
                }


                // Iterate through each piece of the
                // current archetype

                for(; iterator.Shift < 32; iterator.Shift++)
                {
                    // End the loop, if the end of the
                    // current archetype's array has been reached

                    if(arch->collection.GetArray(iterator.Shift) == null)
                        break;


                    // See, if the current iteration
                    // of the iterator is the last for
                    // the coming systems

                    iterator.IsLast = (iterator.Shift == 31) || (arch->collection.GetArray(iterator.Shift + 1) == null);


                    // Iterate through each scheduled system

                    for(int s = sysScheduleLen - 1; s > -1; s--)
                        ((delegate*<ArchetypeIterator*, void>)sysSchedule[s])(&iterator);
                }


                // Free the system schedule

                NativeMemory.Free(sysSchedule);
            }


            // Reset the amount of schedules
            // for the level

            levelSceduleLength ^= levelSceduleLength;
        }
    }


    /// <summary>
    /// The intended time between each fixed update frame in seconds.
    /// If the computer fails to call the fixed updates in the intended
    /// time frame, a compensation will be added.
    /// </summary>

    public static float FixedDeltaTime => 1 / (float)FixedUpdatesPerSecond + _fixedUpdateCompensation;


    // A compensation for the case,
    // that a fixed update is called
    // way after it's intended time

    private static float _fixedUpdateCompensation;


    // Counts the time until the
    // next fixed update

    private static float _fixedUpdateCounter;


    /// <summary>
    /// The amount of times the fixed updates
    /// are called per second.
    /// </summary>

    public static int FixedUpdatesPerSecond = 10;


    // TODO: Add slipping to the entity creation

    /// <summary>
    /// Initialises a given amount of entities
    /// and saves their IDs to the given array.
    /// </summary>
    /// <param name = "entities">
    /// The array to save the IDs of the new entities to.
    /// </param>
    /// <param name = "amount">
    /// The amount of entities to initialise.
    /// </param>

    public static void CreateEntities(int* entities, int amount)
    {
        // The counter for the amount of
        // entities, that have been slipped
        // into unoccupied indices

        int slipped = 0;


        // See, if there are free slots to occupy

        for(int i = 0; i < 32; i++)
        {
            // Get the reference to the current array

            Entity* array = levels[currentLevelID].Entities.GetArray(i);
        

            // Initialise the array,
            // if it hasn't been

            if(array == null)
            {
                levels[currentLevelID].Entities.AllocateArray(i);

                array = levels[currentLevelID].Entities.GetArray(i);

                for(int j = (1 << i) - 1; j > -1; j--)
                    array[j].Name ^= array[j].Name;
            }


            // Iterate through each element in the
            // current array and try to find a free slot

            for(int j = (1 << i) - 1; j > -1; j--)
            {
                // Skip the current element,
                // if it is occupied

                if(array[j].Name != 0)
                    continue;


                // Initialise the entity
                
                array[j].Name = Marshal.StringToCoTaskMemUni("NONAME");

                array[j].Parent ^= array[j].Parent;

                array[j].Children = CompactArray.Create<int>(0);

                array[j].ArchetypeID ^= array[j].ArchetypeID;

                array[j].Index ^= array[j].Index;


                // Save the ID of the
                // currently created entity

                entities[slipped] = (1 << i) | j;


                // Prematurely end the method,
                // if enough entities have been slipped
            
                slipped++;

                if(slipped == amount)
                    return;
            }
        }
    }


    /// <summary>
    /// Finalises the given list of entities.
    /// </summary>
    /// <param name = "entities">
    /// The list of entities to finalise.
    /// </param>
    /// <param name = "amount">
    /// The amount of entities, that are on the list.
    /// </param>

    public static void DeleteEntities(int* entities, int amount)
    {
        // Iterate through each entity to finalise

        for(int e = 0; e < amount; e++)
        {
            // Get the pointer reference
            // to the current entity

            Entity* ent = levels[currentLevelID].Entities.GetElement(entities[e]);


            // Skip to the next iteration,
            // if the entity at the given index
            // is already finalised

            if(ent->Name == 0)
                continue;


            // Dispose of the children of the entity

            DeleteEntities(ent->Children, CompactArray.Length(ent->Children));

            CompactArray.Delete(ent->Children);


            // Get the reference to the
            // entity'S archetype

            Archetype* arch = levels[currentLevelID].Archetypes.GetElement(ent->ArchetypeID);


            {
                // Get the address of the entity's
                // components

                nuint addr = (nuint)arch->collection.GetElement(arch->Stride, ent->Index) + sizeof(int);


                // Iterate through each component
                // of the entity

                for(int c = 0; c < componentMaskSize; c++)
                    // Iterate through each component mask part
                {
                    // A mask used for ignoring
                    // the bits of the previous
                    // iterations

                    ulong ignoreMask = 0;

                    for(int i = BitOperations.PopCount(arch->ComponentMask[c]) - 1; i > -1; i--)
                        // Iterate through each set bit of the component mask
                    {
                        // Evaluate the least significant,
                        // not ignored and set bit

                        ulong componentIndex = (arch->ComponentMask[c] ^ ignoreMask) & 0-(arch->ComponentMask[c] ^ ignoreMask);

                        ignoreMask |= componentIndex; // Add the bit to the ignore mask


                        // Evaluate the index of the current component

                        componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                        // Call the finaliser of the current component,
                        // if it exists
                        
                        if(components[componentIndex].Fin != null)
                            components[componentIndex].Fin(entities[e], (void*)addr);


                        // Add the size of the current component
                        // to the address, to get the address of the
                        // next component

                        addr += (nuint)components[componentIndex].Size;
                    }
                }
            }


            // Unbind the entity from it's parent

            if(ent->Parent != 0)
                RemoveChildren(ent->Parent, &entities[e], 1);


            // Dispose the name of the entity

            Marshal.FreeCoTaskMem(ent->Name);

            ent->Name ^= ent->Name;


            // Get the reference of the entity
            // within it's archetype

            nuint ptr = (nuint)arch->collection.GetElement(arch->Stride, ent->Index);


            // Clear the reference

            *(int*)ptr ^= *(int*)ptr;
        }
    }


    /// <summary>
    /// Displays the parent of the entity.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to get the parent ID from.
    /// </param>

    public static int ShowParent(int entityID)
        => levels[currentLevelID].Entities.GetElement(entityID)->Parent;


    /// <summary>
    /// Displays the children of the entity.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to get the children from.
    /// </param>

    public static int** ShowChildren(int entityID)
        => &levels[currentLevelID].Entities.GetElement(entityID)->Children;


    /// <summary>
    /// Adds the given list of entities
    /// as children to the given entity.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to add the children to.
    /// </param>
    /// <param name = "children">
    /// The list of children to add.
    /// </param>
    /// <param name = "childrenAmount">
    /// The length of the list of children.
    /// </param>

    public static void AddChildren(int entityID, int* children, int childrenAmount)
    {
        // Set the parent references of the children

        for(int i = childrenAmount - 1; i > -1; i--)
            levels[currentLevelID].Entities.GetElement(children[i])->Parent = entityID;


        // Get a pointer reference to the
        // given entity

        Entity* ent = levels[currentLevelID].Entities.GetElement(entityID);


        // A counter for tracking how many
        // children could've been slipped
        // into some unoccupied index

        int slipped = 0;

        // Iterate through each child of the
        // given entity, to see, if there is
        // a free index in the children list

        for(int i = CompactArray.Length(ent->Children) - 1; i > -1; i--)
        {            
            // Skip the current iteration,
            // if no child can be slipped in

            if(ent->Children[i] != 0)
                continue;

            // Slip in a child in the current
            // index and increment the slipped counter

            ent->Children[i] = children[slipped++];


            // Prematurely end the method,
            // if slipping in the children
            // sufficed enough

            if(slipped == childrenAmount)
                return;
        }


        // Evaluate the starting index for the coming loop

        int start = CompactArray.Length(ent->Children);

        // Resize the given entity's children list,
        // to fit in the children that couldn't be
        // slipped into some free slots

        CompactArray.Resize(&ent->Children, CompactArray.Length(ent->Children) + (childrenAmount - slipped));

        // Now add the remaining children to the list

        for(; start < CompactArray.Length(ent->Children); start++)
            // Save the reference of the child
            ent->Children[start] = children[slipped++];
    }


    /// <summary>
    /// Removes the given list of entities
    /// as children from the given entity.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to remove the children from.
    /// </param>
    /// <param name = "children">
    /// The list of children to remove.
    /// </param>
    /// <param name = "childrenAmount">
    /// The length of the list of children.
    /// </param>

    public static void RemoveChildren(int entityID, int* children, int childrenAmount)
    {
        // Reset the parent references of the children

        for(int i = childrenAmount - 1; i > -1; i--)
            levels[currentLevelID].Entities.GetElement(children[i])->Parent = 0;


        // Get a pointer reference to the given entity

        Entity* ent = levels[currentLevelID].Entities.GetElement(entityID);


        // Try to find the given children in
        // their parent's child list and
        // remove their references

        for(int c = childrenAmount - 1; c > -1; c--)
            // Iterate through each given child
        {

            for(int i = CompactArray.Length(ent->Children) - 1; i > -1; i--)
                // Iterate through every child bound to the entity
            {
                // Skip the current iteration,
                // if it isn't a child to remove
                // it's reference from

                if(children[c] != ent->Children[i])
                    continue;

                // If control has come this far, this
                // means, that the current iteration is
                // one of the given children. Their
                // reference will be removed

                ent->Children[i] ^= ent->Children[i];

                break;
            }
        }
    }


    /// <summary>
    /// Adds the given components to the entity.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to add the components to.
    /// </param>
    /// <param name = "components">
    /// The IDs of the components to add.
    /// </param>
    /// <param name = "componentAmount">
    /// The amount of components to add.
    /// </param>

    public static void AddComponents(int entityID, int* components, int componentAmount)
    {
        // Get the reference to the current entity

        Entity* ent = levels[currentLevelID].Entities.GetElement(entityID);


        // Get the reference of the
        // current entity's archetype

        Archetype* curArch = levels[currentLevelID].Archetypes.GetElement(ent->ArchetypeID);


        // The component mask, that'll represent
        // the new archetype of the given entity

        ulong* nComponentMask = stackalloc ulong[componentMaskSize];

        // Copy the mask of the current archetype
        // of the entity

        for(int i = componentMaskSize - 1; i > -1; i--)
            nComponentMask[i] = curArch->ComponentMask[i];

        // Add the bits of the new components
        // to the new component mask aswell

        for(int i = componentAmount - 1; i > -1; i--)
            nComponentMask[components[i] / 64] |= (ulong)1 << (components[i] % 64);


        // Try to find the archetype, that
        // fits the new component mask

        int nArchetypeIndex = 0;

        for(int i = 0; i < 32; i++)
        {
            Archetype* array = levels[currentLevelID].Archetypes.GetArray(i);


            if(array == null)
                break;


            for(int j = (1 << i) - 1; j > -1; j--)
            {
                if(array[j].Systems == null)
                    continue;


                for(int c = componentMaskSize - 1; c > -1; c--)
                    if(array[j].ComponentMask[c] != nComponentMask[c])
                        goto skip;

                nArchetypeIndex = (1 << i) | j;

                goto skipAllocArch;

                skip:;
            }
        }


        // If control has come this far,
        // then this means, that there is
        // no archetype with the preciously
        // evaluated component mask.
        // A new one will be created now

        createArchetype(nComponentMask, &nArchetypeIndex);

        // Jumping point for skipping
        // the allocation of a new index
        // in the current level's
        // archetype list

        skipAllocArch:;


        // Get the reference to the
        // new archetype

        Archetype* newArch = levels[currentLevelID].Archetypes.GetElement(nArchetypeIndex);


        int nIndex = 0;

        // Try to find a free index in the
        // data array of the new archetype

        for(byte i = 0; i < 32; i++)
            // Iterate through each array
        {
            // Get the address of the
            // current array

            nuint array = (nuint)newArch->collection.GetArray(i);


            // Allocate the array, if it isn't allocated

            if(array == 0)
            {
                newArch->collection.AllocateArray(newArch->Stride, i);
            
                array = (nuint)newArch->collection.GetArray(i);
            }

        
            for(int j = (1 << i) - 1; j > -1; j--)
                // Iterate through each element in the current array
            {
                // Get the address of the
                // current component group

                nuint addr = array + (nuint)(newArch->Stride * j);
            
                // Skip to the next iteration,
                // if the current component group
                // isn't free

                if(*(int*)addr != 0)
                    continue;

                // Free component group has been found.
                // It's index will be saved and the loop
                // will be ended prematurely

                nIndex = (1 << i) | j;                
            
                goto foundIndex;
            }
        }

        foundIndex:;


        // Remove the entity's reference
        // from it's current archetype

        {
            nuint addr =
                (nuint)curArch->collection.GetElement(curArch->Stride, ent->Index);

            *(int*)addr ^= *(int*)addr;
        }


        // Add the entity's reference
        // to it's new archetype

        {
            nuint addr =
                (nuint)newArch->collection.GetElement(newArch->Stride, nIndex);

            *(int*)addr = entityID;
        }


        // The offset for the next
        // components in the current
        // archetype's data array

        int cOffset = 0;

        // The offset for the next
        // components in the new
        // archetype's data array

        int nOffset = 0;

        // Now copy over components from
        // the current archetype to the
        // new archetype

        for(int c = 0; c < componentMaskSize; c++) // Iterate through each component mask part
        {
            // Combine the component masks
            // of the current and new archetype

            ulong combined = newArch->ComponentMask[c] | curArch->ComponentMask[c];


            // A mask for ignoring
            // the bits of the coming iterations

            ulong ignored = 0;


            // Iterate through each set bit of the combined mask

            for(int i = BitOperations.PopCount(combined) - 1; i > -1; i--)
            {
                // Evaluate the least significant,
                // set and not ignored bit

                ulong componentIndex = (combined ^ ignored) & 0-(combined ^ ignored);

                // Add the currently evaluated bit to
                // the ignore mask

                ignored |= componentIndex;


                // See, if the current archetype has the
                // currently evaluated component

                bool cHas = (curArch->ComponentMask[c] & componentIndex) == componentIndex;

                // See, if the new archetype has the
                // currently evaluated component

                bool nHas = (newArch->ComponentMask[c] & componentIndex) == componentIndex;


                // Evaluate the index of the current component

                componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                // Component will be copied

                if(cHas && nHas)
                {
                    // Evaluate the adress of the component to copy from

                    nuint from =
                        (nuint)curArch->collection.GetElement(curArch->Stride, ent->Index);

                    from += sizeof(int) + (nuint)cOffset;


                    // Evluate the address to copy the component to

                    nuint to = (nuint)newArch->collection.GetElement(newArch->Stride, nIndex);

                    to += sizeof(int) + (nuint)nOffset;


                    // Call the method to copy

                    FloatFast.Copy(from, to, Finder.components[componentIndex].Size);
                }


                // Add the offsets

                cOffset += Finder.components[componentIndex].Size * (*(byte*)&cHas & 1);

                nOffset += Finder.components[componentIndex].Size * (*(byte*)&nHas & 1);
            }
        }


        // Temporarily stores the
        // ID of the current archetype

        int lastArchetypeIndex = ent->ArchetypeID;


        // Save the entity's new archetype and index

        ent->ArchetypeID = nArchetypeIndex;

        ent->Index = nIndex;  


        // The offsets for the
        // next components of the
        // following iteration

        int offset = 0;


        // See, which components need to be initialised

        for(int c = 0; c < componentMaskSize; c++)
        {
            // Combine the component masks
            // of the last and new archetype

            ulong combined = newArch->ComponentMask[c] | curArch->ComponentMask[c];


            // A mask for ignoring 
            // the bits of the following iterations

            ulong ignored = 0;


            // Iterate through each set bit of
            // the combined mask

            for(int i = BitOperations.PopCount(combined) - 1; i > -1; i--)
            {
                // Evaluate the least significant,
                // set and not ignored bit

                ulong componentIndex = (combined ^ ignored) & 0-(combined ^ ignored);

                // Add the currently evaluated bit
                // to the ignore mask

                ignored |= componentIndex;


                // See, if the last archetype has the
                // current component

                bool lHas = (curArch->ComponentMask[c] & componentIndex) == componentIndex;

                // See, if the new archetype has the
                // current component

                bool nHas = (newArch->ComponentMask[c] & componentIndex) == componentIndex;


                // Evaluate the index of the component

                componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                // Component will be initialised

                if(!lHas && nHas)
                {
                    // Evaluate the address of the
                    // component itself

                    nuint addr = (nuint)newArch->collection.GetElement(newArch->Stride, nIndex);

                    addr += sizeof(int) + (nuint)offset;


                    // Call the initialiser of the
                    // component, if it exists

                    if(Finder.components[componentIndex].Init != null)
                        Finder.components[componentIndex].Init(entityID, (void*)addr);
                }


                // Add the offset for
                // accessing the next component

                offset += (*(byte*)&nHas & 1) * Finder.components[componentIndex].Size;
            }
        }    
    }


    /// <summary>
    /// Removes the given components from the entity.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to remove the components from.
    /// </param>
    /// <param name = "components">
    /// The IDs of the components to remove.
    /// </param>
    /// <param name = "componentAmount">
    /// The amount of components to remove.
    /// </param>

    public static void RemoveComponents(int entityID, int* components, int componentAmount)
    {
        // Get the reference of the given entity

        Entity* ent = levels[currentLevelID].Entities.GetElement(entityID);


        // Get the reference of the
        // given entity's archetype

        Archetype* curArch = levels[currentLevelID].Archetypes.GetElement(ent->ArchetypeID);


        // The component mask, that'll represent
        // the new archetype of the given entity

        ulong* nComponentMask = stackalloc ulong[componentMaskSize];

        // Copy the mask of the current archetype
        // of the entity

        for(int i = componentMaskSize - 1; i > -1; i--)
            nComponentMask[i] = curArch->ComponentMask[i];

        // Now in the component mask, unset the
        // bits of the components to remove

        for(int i = componentAmount - 1; i > -1; i--)
            nComponentMask[components[i] / 64] ^= (ulong)1 << (components[i] % 64);


        // Try to find the archetype, that
        // fits the new component mask

        int nArchetypeIndex = 0;

        for(int i = 0; i < 32; i++)
        {
            Archetype* array = levels[currentLevelID].Archetypes.GetArray(i);


            if(array == null)
                break;


            for(int j = (1 << i) - 1; j > -1; j--)
            {
                if(array[j].Systems == null)
                    continue;


                for(int c = componentMaskSize - 1; c > -1; c--)
                    if(array[j].ComponentMask[c] != nComponentMask[c])
                        goto skip;

                nArchetypeIndex = (1 << i) | j;

                goto skipAllocArch;

                skip:;
            }
        }

        // If control has come this far,
        // then this means, that there is
        // no archetype with the preciously
        // evaluated component mask.
        // A new one will be created now

        createArchetype(nComponentMask, & nArchetypeIndex);

        // Jumping point for skipping
        // the allocation of a new index
        // in the current level's
        // archetype list

        skipAllocArch:;


        // Get the reference to the
        // new archetype

        Archetype* newArch = levels[currentLevelID].Archetypes.GetElement(nArchetypeIndex);


        int nIndex = 0;

        // Try to find a free index in the
        // data array of the new archetype

        for(byte i = 0; i < 32; i++)
            // Iterate through each array
        {
            // Get the address of the
            // current array

            nuint array = (nuint)newArch->collection.GetArray(i);


            // Allocate the array, if it isn't allocated

            if(array == 0)
            {
                newArch->collection.AllocateArray(newArch->Stride, i);
            
                array = (nuint)newArch->collection.GetArray(i);
            }

        
            for(int j = (1 << i) - 1; j > -1; j--)
                // Iterate through each element in the current array
            {
                // Get the address of the
                // current component group

                nuint addr = array + (nuint)(newArch->Stride * j);
            
                // Skip to the next iteration,
                // if the current component group
                // isn't free

                if(*(int*)addr != 0)
                    continue;

                // Free component group has been found.
                // It's index will be saved and the loop
                // will be ended prematurely

                nIndex = (1 << i) | j;                
            
                goto foundIndex;
            }
        }

        foundIndex:;


        // The offsets for the
        // next components of the
        // following iteration

        int offset = 0;


        // See, which components need to be finalised

        for(int c = 0; c < componentMaskSize; c++)
        {
            // Combine the component masks
            // of the current and new archetype

            ulong combined = newArch->ComponentMask[c] | curArch->ComponentMask[c];


            // A mask for ignoring 
            // the bits of the following iterations

            ulong ignored = 0;


            // Iterate through each set bit of
            // the combined mask

            for(int i = BitOperations.PopCount(combined) - 1; i > -1; i--)
            {
                // Evaluate the least significant,
                // set and not ignored bit

                ulong componentIndex = (combined ^ ignored) & 0-(combined ^ ignored);

                // Add the currently evaluated bit
                // to the ignore mask

                ignored |= componentIndex;


                // See, if the current archetype has the
                // current component

                bool cHas = (curArch->ComponentMask[c] & componentIndex) == componentIndex;

                // See, if the new archetype has the
                // current component

                bool nHas = (newArch->ComponentMask[c] & componentIndex) == componentIndex;


                // Evaluate the index of the component

                componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                // Component will be finalised

                if(cHas && !nHas)
                {
                    // Evaluate the address of the
                    // component itself

                    nuint addr =
                        (nuint)curArch->collection.GetElement(curArch->Stride, nIndex);

                    addr += sizeof(int) + (nuint)offset;


                    // Call the finaliser of the
                    // component, if it exists

                    if(Finder.components[componentIndex].Fin != null)
                        Finder.components[componentIndex].Fin(entityID, (void*)addr);
                }


                // Add the offset for
                // accessing the next component

                offset += (*(byte*)&cHas & 1) * Finder.components[componentIndex].Size;
            }
        }


        // Remove the entity's reference
        // from it's current archetype

        {
            nuint addr =
                (nuint)curArch->collection.GetElement(curArch->Stride, ent->Index);

            *(int*)addr ^= *(int*)addr;
        }


        // Add the entity's reference
        // to it's new archetype

        {
            nuint addr =
                (nuint)newArch->collection.GetElement(newArch->Stride, nIndex);

            *(int*)addr = entityID;
        }


        // The offset for the next
        // components in the current
        // archetype's data array

        int cOffset = 0;

        // The offset for the next
        // components in the new
        // archetype's data array

        int nOffset = 0;

        // Now copy over components from
        // the current archetype to the
        // new archetype

        for(int c = 0; c < componentMaskSize; c++) // Iterate through each component mask part
        {
            // Combine the component masks
            // of the current and new archetype

            ulong combined = newArch->ComponentMask[c] | curArch->ComponentMask[c];


            // A mask for ignoring
            // the bits of the coming iterations

            ulong ignored = 0;


            // Iterate through each set bit of the combined mask

            for(int i = BitOperations.PopCount(combined) - 1; i > -1; i--)
            {
                // Evaluate the least significant,
                // set and not ignored bit

                ulong componentIndex = (combined ^ ignored) & 0-(combined ^ ignored);

                // Add the currently evaluated bit to
                // the ignore mask

                ignored |= componentIndex;


                // See, if the current archetype has the
                // currently evaluated component

                bool cHas = (curArch->ComponentMask[c] & componentIndex) == componentIndex;

                // See, if the new archetype has the
                // currently evaluated component

                bool nHas = (newArch->ComponentMask[c] & componentIndex) == componentIndex;


                // Evaluate the index of the current component

                componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                // Component will be copied

                if(cHas && nHas)
                {
                    // Evaluate the adress of the component to copy

                    nuint from =
                        (nuint)curArch->collection.GetElement(curArch->Stride, ent->Index);

                    from += sizeof(int) + (nuint)cOffset;


                    // Evluate the address to copy the component to

                    nuint to = (nuint)newArch->collection.GetElement(newArch->Stride, nIndex);

                    to += sizeof(int) + (nuint)nOffset;


                    // Call the method to copy

                    FloatFast.Copy(from, to, Finder.components[componentIndex].Size);
                }


                // Add the offsets

                cOffset += Finder.components[componentIndex].Size * (*(byte*)&cHas & 1);

                nOffset += Finder.components[componentIndex].Size * (*(byte*)&nHas & 1);
            }
        }


        // Temporarily stores the
        // ID of the current archetype

        int lastArchetypeIndex = ent->ArchetypeID;


        // Save the entity's new archetype and index

        ent->ArchetypeID = nArchetypeIndex;

        ent->Index = nIndex;  
    }


    /// <summary>
    /// Returns true, if the given entity
    /// contains the given components.
    /// </summary>
    /// <param name = "entityID">
    /// The entity to check the components from.
    /// </param>
    /// <param name = "components">
    /// The list of the components to check for.
    /// </param>
    /// <param name = "componentAmount">
    /// The amount of component IDs, that are on the list.
    /// </param>

    public static bool HasComponents(int entityID, int* components, int componentAmount)
    {
        // Get a reference to the given entity

        Entity* ent = levels[currentLevelID].Entities.GetElement(entityID);

        // Get a reference to the given entity's archetype

        Archetype* arch = levels[currentLevelID].Archetypes.GetElement(ent->ArchetypeID);


        // Iterate through each given component ID

        for(int i = componentAmount - 1; i > -1; i--)
        {
            ulong mask = (ulong)1 << (components[i] % 64);

            if((arch->ComponentMask[components[i] / 64] & mask) != mask)
                return false;
        }

        // If control has come this far,
        // this means, that all the given
        // component IDs are present at the
        // entity

        return true;
    }


    /// <summary>
    /// Returns the address of the given entity's component
    /// </summary>
    /// <param name = "entityID">
    /// The entity to get the component from.
    /// </param>
    /// <param name = "componentID">
    /// The component to get the address of.
    /// </param>

    public static void* GetComponent(int entityID, int componentID)
    {
        // Get a reference to the given entity

        Entity* ent = levels[currentLevelID].Entities.GetElement(entityID);

        // Get a reference to the given entity's archetype

        Archetype* arch = levels[currentLevelID].Archetypes.GetElement(ent->ArchetypeID);


        // Iterate through each component of the
        // entity's archetype, until the wanted
        // component has been found

        int offset = 0;

        for(int c = 0; c < componentMaskSize; c++) // Iterate through each component mask part
        {
            // The ignore mask is used
            // for ignoring the bits of
            // the previous iterations

            ulong ignoreMask = 0;


            // Iterate through each bit
            // of the current part of the
            // archetype's component mask

            for(int i = BitOperations.PopCount(arch->ComponentMask[c]) - 1; i > -1; i--)
            {
                // Evaluate the least significant, set and
                // not ignored bit

                ulong componentIndex = (arch->ComponentMask[c] ^ ignoreMask) & 0-(arch->ComponentMask[c] ^ ignoreMask);

                ignoreMask |= componentIndex;


                // Calculate the index of the
                // current component

                componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                // Component found

                if((int)componentIndex == componentID)
                {
                    // Evaluate the address of the component

                    nuint addr = (nuint)arch->collection.GetElement(arch->Stride, ent->Index);

                    addr += sizeof(int) + (nuint)offset; 


                    // Return the address
                    // of the component

                    return (void*)addr;
                }


                // Add the offset for accessing
                // the next component

                offset += components[componentIndex].Size;
            }
        }


        // Component not found

        return null;
    }


    // A helper method for creating
    // a new archetype

    private static void createArchetype(ulong* componentMask, int* index)
    {
        // The address to save the
        // new archetype at

        Archetype* arch = null;


        // Iterate through each currently
        // allocated element

        for(int i = 0; i < 32; i++)
        {
            // Get the reference of the
            // current array

            arch = levels[currentLevelID].Archetypes.GetArray(i);


            // Allocate the array, if
            // it hasn't been yet

            if(arch == null)
            {
                levels[currentLevelID].Archetypes.AllocateArray(i);

                arch = levels[currentLevelID].Archetypes.GetArray(i);

                for(int j = (1 << i) - 1; j > -1; j--)
                    arch[j].Systems = null;
            }


            // See, if there is a free
            // element in the current array.
            // If there is, the index will be saved
            // and the search ends completely

            for(int j = (1 << i) - 1; j > -1; j--)
            {
                if(arch[j].Systems != null)
                    continue;

                *index = (1 << i) | j;
            
                arch = &arch[j];

                goto endSearch;
            }
        }

        endSearch:;


        // Allocate and save the new
        // archetype's component mask

        arch->ComponentMask = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * (nuint)componentMaskSize, sizeof(ulong));

        for(int i = componentMaskSize - 1; i > -1; i--)
            arch->ComponentMask[i] = componentMask[i];


        // Calculate the stride

        arch->Stride = sizeof(int);        

        for(int c = 0; c < componentMaskSize; c++) // Iterate through each part of the component mask
        {
            // A mask for ignoring the bits
            // of in the following loop

            ulong ignoreMask = 0;


            // Iterate through each set bit of
            // the given component mask

            for(int i = BitOperations.PopCount(componentMask[c]) - 1; i > -1; i--)
            {
                // Evaluate the least significant, set and
                // not ignored bit of the current part
                // of the given component mask

                ulong componentIndex = (componentMask[c] ^ ignoreMask) & 0-(componentMask[c] ^ ignoreMask);


                // Add the currently evaluated bit
                // to the ignore mask

                ignoreMask |= componentIndex;


                // Evaluate the index of the component

                componentIndex = (ulong)(BitOperations.Log2(componentIndex) + c * 64);


                // Add the size of the component
                // to the stride of the archetype

                arch->Stride += components[componentIndex].Size;
            }
        }


        // Initialise the array,
        // that'll hold the component data and
        // references to entities related to
        // this archetype

        arch->collection.Create();


        // Preallocate the array,
        // that holds the systems
        // compatible with the
        // given type of archetype

        arch->Systems = (System**)CompactArray.Create<nuint>(0);


        // Now try to find the systems,
        // that fit the archetype

        {
            // A stencil mask for seeing,
            // what component mask a system
            // should at least have, to be accepted
            // by the archetype.
            // Will only be set by systems,
            // that have the denylesser flag

            ulong* lesserStencil = null;


            // Find step

            for(int i = 0; i < CompactArray.Length(systems); i++)
            {
                // Compare the masks for
                // any sort of overlap

                for(int c = 0; c < componentMaskSize; c++)
                {
                    // Combine the mask of the archetype
                    // and the system, to see, if the
                    // system expects more components,
                    // than the archetype has

                    ulong combined = componentMask[c] | systems[i].ComponentMask[c];


                    // Skip the current system,
                    // if the system expects components,
                    // that the archetype doesn't have

                    if(BitOperations.PopCount(combined) > BitOperations.PopCount(componentMask[c]))
                        goto skipSystem;


                    // Skip the step of checking
                    // the current system's inferiority,
                    // if there isn't a reference to go off of

                    if(lesserStencil == null)
                        continue;

                    
                    // Overlap the mask of the current
                    // system with the mask of the lesser
                    // component mask reference, to see,
                    // if the current system is actually lesser

                    combined = systems[i].ComponentMask[c] & lesserStencil[c];


                    // Skip the current system,
                    // if it has less component flags,
                    // than the lesser component system reference

                    if(combined != lesserStencil[c])
                        goto skipSystem;
                }


                // See, if the system can only
                // run on archetypes with the
                // same component mask

                if((systems[i].State & SystemState.RunExactArchetype) == SystemState.RunExactArchetype)

                    // Direclty compare the component masks
                    // of the new archetype and current system.
                    // Skip to the next system, if
                    // both masks aren't the same

                    for(int c = 0; c < componentMaskSize; c++)
                        if(systems[i].ComponentMask[c] != componentMask[c])
                            goto skipSystem;                


                // See, if the current system
                // doesn't want systems, that
                // have lesser component masks

                if((systems[i].State & SystemState.DenyLesserSystems) == SystemState.DenyLesserSystems)
                {
                    // Initialise the lesser
                    // component mask reference
                    // and skip to the next iteration

                    if(lesserStencil == null)
                    {
                        // Allocate the memory

                        lesserStencil = (ulong*)NativeMemory.AlignedAlloc(sizeof(ulong) * (nuint)componentMaskSize, sizeof(ulong));


                        // Copy the component mask of the
                        // current system to the lesser
                        // component mask reference

                        for(int c = 0; c < componentMaskSize; c++)
                            lesserStencil[c] = systems[i].ComponentMask[c];


                        // Skip to the next system

                        goto skipDLSCheck;
                    }   


                    // Overlap the mask of the
                    // current system's component mask and the
                    // lesser component mask reference

                    for(int c = 0; c < componentMaskSize; c++)
                        lesserStencil[c] |= systems[i].ComponentMask[c];


                    // Jumping point for
                    // skipping the rest of this step

                    skipDLSCheck:;
                }


                // Save the system to the
                // new archetype's array
                // of system references

                CompactArray.Resize((nuint**)&arch->Systems, CompactArray.Length((nuint*)arch->Systems) + 1);

                arch->Systems[CompactArray.Length((nuint*)arch->Systems) - 1] = &systems[i];


                // Jumping point to
                // skipping the current system

                skipSystem:;
            }


            // Skip the exclude step,
            // if there is no lesser
            // stencil to speak of

            if(lesserStencil == null)
                goto skipExlude;


            // Exclude step


            // The index, at which valid
            // systems can be moved to

            int saveTo = 0;

            
            // Now iterate through each
            // system reference saved in
            // the new archetype

            for(int i = 0; i < CompactArray.Length((nuint*)arch->Systems); i++)
            {
                // See, if the current iteration's
                // component mask is lesser than
                // the lesser component mask reference

                for(int c = 0; c < componentMaskSize; c++)
                {
                    // Overlap the component mask of
                    // the current system and the
                    // lesser component mask reference,
                    // to see, if the current system
                    // is considered lesser

                    ulong overlapped = arch->Systems[i]->ComponentMask[c] & lesserStencil[c];

                    if(overlapped == lesserStencil[c])
                        continue;


                    // If we have come this far, this
                    // means, that the current system
                    // is lesser and will be skipped

                    goto skipNext;
                }


                // Save the valid system reference
                // to the lowest free space

                arch->Systems[saveTo++] = arch->Systems[i];


                // Jumping point for
                // skipping to the
                // next iteration

                skipNext:;
            }


            // Resize the array, that
            // holds all the system references

            if(CompactArray.Length((nuint*)arch->Systems) > saveTo)
                CompactArray.Resize((nuint**)&arch->Systems, saveTo);


            // Jumping point to skipping
            // the excluding of systems

            skipExlude:;


            // Free the lesser stencil array,
            // if it was allocated

            NativeMemory.AlignedFree(lesserStencil);
        }
    }


    // Helper method for disposing of archetypes

    private static void deleteArchetype(int index)
    {
        Archetype* arch = levels[currentLevelID].Archetypes.GetElement(index);


        NativeMemory.AlignedFree(arch->ComponentMask);

        arch->collection.Delete();

        CompactArray.Delete((nuint*)arch->Systems);
    }


    /// <summary>
    /// An end method for making
    /// sure all resources of the
    /// engine are freed.
    /// </summary>

    public static void End()
    {
        // Finalise each running level

        for(int i = CompactArray.Length(levels) - 1; i > -1; i--)
            EndLevel(i);


        // Dispose of the levels list

        CompactArray.Delete(levels);


        // Dispose the systems list

        CompactArray.Delete(systems);


        // Dispose the components list

        CompactArray.Delete(components);
    }
}


// Classes that are given this
// atrribute, will be seen as levels

[AttributeUsage(AttributeTargets.Class)]
public sealed class LevelAttribute : Attribute
{   
    // Instance initialiser

    /// <param name="isStarter">
    /// If true, the level will start
    /// as soon as the engine starts.
    /// </param>

    public LevelAttribute(bool isStarter)
        => IsStarter = isStarter;

    
    // Signifies, if the level is a starter

    public readonly bool IsStarter;
}


// Stores necessary information
// for a level

public unsafe struct Level
{
    // Called at the initialisation of the level

    public nuint Start;

    // Called at the finalisation of the level

    public nuint End;

    // Called every update frame

    public nuint Update;

    // Called every fixed update frame

    public nuint BroadPhase, NarrowPhase;

    // Called every update frame

    public nuint PreRender, Render, PostRender;


    // The scale, at which time
    // passes for the scene

    public float TimeScale;


    // The list of entities
    // in this level

    public ExponentialArray<Entity> Entities;


    // The list of archetypes
    // in this level

    public ExponentialArray<Archetype> Archetypes;


    // The current state of the level

    public LevelState State;
}


// Represents the different states,
// that a level can assume

[Flags]
public enum LevelState : byte
{
    // Level has no state

    None = 0,


    // The level is initialised

    IsInitialised = 4,


    // The level is paused

    IsPaused = 8,
}