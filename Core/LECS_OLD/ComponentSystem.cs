
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace Core.LECS_OLD;


public unsafe static partial class Engine
{


    // Returns the size in dwords
    // of a component mask

    private static int getMaskSize()
    {
        // Check, if the following
        // equation should be incremented

        bool add = CompactArray.Length(components) % 32 > 0;

        // Finally, calculate how big in
        // dwords the component mask is

        return CompactArray.Length(components) / 32 + *(byte*)&add;
    }


    // Binds a component of the given type
    // to the given entity

    /*public static void AddComponent<T>(int cID, T value, int eID)
    {
        // Save the size of a mask

        int maskSize = getMaskSize();


        // Get the ID of the given entity's
        // archetype

        int aID = levels[currentLevelIndex].Entities.Elements[eID].ArchetypeID;


        // Allocate memory for the
        // component mask resembling
        // the desired archetype of
        // the entity

        int* componentMask = stackalloc int[maskSize];

        for(int i = maskSize - 1; i >= 0; i--)
            componentMask[i] = levels[currentLevelIndex].Archetypes.Elements[aID].ComponentMask[i];
        
        componentMask[cID / 32] |= 1 << cID % 32;


        // Get the ID of the targeted archetype

        int nAID = 0;

        for(int i = 0; i < levels[currentLevelIndex].Archetypes.Length * LockArray<Architecture>.ChunkSize; i++)
        {
            // See if the current archetype's
            // component mask matches the 
            // evaluated component mask

            for(int j = maskSize - 1; j >= 0; j--)
                if(componentMask[j] != levels[currentLevelIndex].Archetypes.Elements[i].ComponentMask[j])
                    goto next;


            break;

            next:
                ;
        }
    }*/
}


// Contains necessary
// aspects of a specific component             

unsafe struct Component
{
    // Reference to the
    // component type's
    // initialiser

    public delegate*<int, void> Initialiser;

    // Reference to the
    // component type's
    // finaliser

    public delegate*<int, void> Finaliser;


    // The size in bytes
    // of the component's type

    public int Size;

    // The index of the
    // component

    public int BitIndex;
}


// The component attribute.
// Gives the struct type fitted
// with this attribute an unique ID

[AttributeUsage(AttributeTargets.Struct)]
public class ComponentAttribute : Attribute;


// The system attribute.
// Classes fitted with this attribute
// will be checked for a method called
// "OnUpdate" to update the given
// series of components with

[AttributeUsage(AttributeTargets.Class)]
public class SystemAttribute : Attribute
{
    // Instance initialiser

    public SystemAttribute(SystemState state)
    {
        State = state;
    }

    // The state of the system assigned
    // by the user

    public readonly SystemState State;
}


// Stores necessary
// information of
// a system

unsafe struct System
{
    // Holds the info of the
    // offsets of each type

    public OffsetInfo OffsetInfo;


    // An unique state
    // to the system

    public SystemState State;


    // The update method, that
    // is called every update frame

    public nint OnUpdate;
    
    // The fixed update methods.
    // They're called sequentially
    // every fixed update frame

    public nint OnBroadPhase, OnNarrowPhase;
    
    // The render update methods.
    // They're called sequentially
    // every render update frame

    public nint OnPreRender, OnRender, OnPostRender;
}


// Defines different
// states, that a
// system can assume

[Flags]
public enum SystemState : byte
{
    // No state

    None = 0,

    // Makes it so, that
    // the system only runs
    // on an archetype exactly
    // fitting it's assigned archetype

    RunExactArchetype = 1,

    // Systems, that have an inferior
    // archetype, will not run on the
    // assigned archetype of this system

    DenyLesserSystems = 2,


}


// Holds offset information

public unsafe struct OffsetInfo
{
    // Instance initialiser

    public OffsetInfo(int[] componentIDs)
    {
        // Allocate space for the
        // ComponentIDs array and
        // copy the given values over

        ComponentIDs = CompactArray.Create<int>(componentIDs.Length);

        for(int i = componentIDs.Length - 1; i > -1; i--)
            ComponentIDs[i] = componentIDs[i];
    }


    // An array to hold
    // the IDs of the used
    // components of the system
    // (Compact array)

    public int* ComponentIDs;
}