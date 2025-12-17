

namespace Core.LECS;


// All classes or structs fit with this
// attribute will be seen as systems
// by the engine

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class SystemAttribute : Attribute
{
    // Instance constructor 

    public unsafe SystemAttribute(bool ExactArchetype = false, bool DenyLesser = false)
    {
        // Set if the system should run
        // on an archetype with an exact
        // same component mask

        State |= *(SystemState*)&ExactArchetype;

        // Set if the system should deny
        // systems with less corresponding
        // component masks from processing
        // it's assigned archetype

        State |= (SystemState)(*(byte*)&DenyLesser << 1);
    }

    
    // The state the system will have

    public readonly SystemState State;
}


// Processes archetypes that
// fit it's component mask

public unsafe struct System
{
    // Behaviour specifications

    public SystemState State;


    // Called on every update frame

    public nuint OnUpdate;


    // Broad phase and narrow phase
    // are called every physics step.
    // The broad phase is always called
    // before the narrow phase 

    public nuint OnBroadPhase, OnNarrowPhase;


    // Pre render, render and post render
    // are called every render frame.
    // The pre render is always called
    // before render and render is always
    // called before post render

    public nuint OnPreRender, OnRender, OnPostRender;


    // Displays which components
    // are stored within the archetype

    public ulong* ComponentMask;
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
    // fitting it's assigned component mask

    RunExactArchetype = 1,

    // Systems, that have an inferior
    // archetype, will not run on the
    // assigned archetype of this system

    DenyLesserSystems = 2,


}