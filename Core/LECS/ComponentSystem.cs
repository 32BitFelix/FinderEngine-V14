
using System.Reflection;
using Core.MemoryManagement;

namespace Core.LECS;


public unsafe static partial class Engine
{

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