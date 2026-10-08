using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace Core.LECSSimple;


// Archetypes hold certain combinations
// of components, aswell as references
// to related entities

public unsafe struct Archetype
{
    // A mask that represents the
    // components stored in the archetype

    public ulong* ComponentMask;


    // The array, that holds
    // the components and related entity references

    public ComponentCollection collection;


    // The stride in bytes
    // for skipping a
    // component chunk of
    // the archetype

    public int Stride;


    // Reference to the array
    // holding the systems that
    // can process the archetype
    // (Compact array)

    public System** Systems;
}


// NOTE: Always start a related
// loop on the outer array on index one.
// Index zero and one will be treated the same
// and might cause undesired outcomes!

/// <summary>
/// Holds the components of an archetype
/// in a split array that grows exponentially
/// without any reallocation whatsoever.
/// Based on the concept posed by
/// https://philosopherdeveloper.com/posts/how-to-build-a-thread-safe-lock-free-resizable-array.html
/// </summary>

public struct ComponentCollection
{
    // Holds the references to the
    // individual pieces of the collection,
    // aswell as their collective lengths
    // and a counter for the systems
    // Length (int) | Arrays (pointer * 32)

    public nuint Data;
}


// Holds extension methods
// for the component collection struct

public unsafe static class ComponentCollectionExt
{
    /// <summary>
    /// Helper method for creating
    /// a component collection.
    /// </summary>

    public static void Create(this ref ComponentCollection collection)
    {
        // Allocate the memory for the
        // outer array

        collection.Data = (nuint)NativeMemory.Alloc((nuint)(sizeof(int) + sizeof(nuint) * 32));


        // Default everything

        *(int*)collection.Data ^= *(int*)collection.Data;

        nuint start = collection.Data + sizeof(int);

        for(int i = 0; i < 32; i++)
            ((ulong*)start)[i] ^= ((ulong*)start)[i];
    }


    /// <summary>
    /// Returns the collective length
    /// of the component collection.
    /// </summary>

    public static int Length(this ref ComponentCollection collection)
        => *(int*)collection.Data;


    /// <summary>
    /// Returns the components at the
    /// given index.
    /// </summary>
    /// <param name = "stride">
    /// The size in bytes of each
    /// group of components
    /// </param>
    /// <param name = "index">
    /// The index of the group of components
    /// to return
    /// </param>

    public static void* GetElement(this ref ComponentCollection collection, int stride, int index)
    {
        // Evaluate the indices of the
        // array and element

        byte arrayIndex = (byte)BitOperations.Log2((uint)index);

        int elementIndex = index ^ (1 << arrayIndex);

        {
            bool isNotZero = arrayIndex != 0;

            // The bitwise and may seem unnecessary, but i heard some architectures
            // might return 255 as true

            elementIndex *= *(byte*)&isNotZero & 1;
        }


        // Get the address of the array
        
        nuint array = ((nuint*)(collection.Data + sizeof(int)))[arrayIndex];
        

        // Return the address of the element
        // relative to the evaluated array

        return (void*)(array + (nuint)(stride * elementIndex));
    }


    /// <summary>
    /// Frees the unmanaged
    /// resources of a component collection
    /// </summary>

    public static void Delete(this ref ComponentCollection collection)
    {
        // Dispose of the arrays within the
        // outer array

        for(int i = 0; i < 32; i++)
        {
            // Get the address of the current array

            nuint array = ((nuint*)(collection.Data + sizeof(int)))[i];

            // Skip the current array,
            // if it has no unmanaged resources

            if(array == 0)
                continue;

            // Free the resources of the
            // current array

            NativeMemory.Free((void*)array);
        }


        // Free the outer array

        NativeMemory.Free((void*)collection.Data);
    }


    /// <summary>
    /// Returns the address of an
    /// array based on the given index.
    /// (Must be in the range between 0 to 31.)
    /// </summary>

    public static void* GetArray(this ref ComponentCollection collection, int arrayIndex)
        => (void*)((nuint*)(collection.Data + sizeof(int)))[arrayIndex];


    /// <summary>
    /// Allocates memory for one
    /// of the indexed arrays.
    /// </summary>
    /// <param name = "stride">
    /// The size of a group of components.
    /// </param>
    /// <param name = "arrayIndex">
    /// The index of the array to allocate.
    /// </param>

    public static void AllocateArray(this ref ComponentCollection collection, int stride, int arrayIndex)
    {
        // Evaluate the length of the
        // upcoming array and add it to
        // the collective length

        nuint toAlloc = (nuint)1 << arrayIndex;

        *(int*)collection.Data += (int)toAlloc;


        // Evaluate the memory in bytes to allocate
        // for the upcoming array

        toAlloc *= (nuint)stride;


        // Allocate the array

        ((nuint*)(collection.Data + sizeof(int)))[arrayIndex] = (nuint)NativeMemory.Alloc(toAlloc);


        // Set the entity references
        // to zero

        for(int i = (1 << arrayIndex) - 1; i > -1; i--)
        {
            // Evaluate the address of the
            // current entity reference

            nuint entRef = ((nuint*)(collection.Data + sizeof(int)))[arrayIndex] + (nuint)(stride * i);


            // Set the reference to zero

            *(int*)entRef ^= *(int*)entRef;
        }
    }
}


// A helper structure for systems, which helps
// through the components of an archetype

public unsafe struct ArchetypeIterator 
{
    // The reference of the archetype
    // to iterate through

    public Archetype* Archetype;

    // The offsets of the components
    // to iterate through

    public int* ComponentOffsets;


    // Some unique state, that
    // an iterator could have

    public byte State;


    // Indicates, if the given iteration
    // of the system is the last in the
    // current level's cycle

    public bool IsLast => (State & 1) == 1;


    // Indicates, if the given iteration
    // of the system is the first in the
    // current level's cycle

    public bool IsFirst => (State & 2) == 2;


    // Represents the exponent
    // to calulate the length
    // of the currently saved
    // piece of array and the
    // index of the current array
    // to iterate through

    public byte Shift;
}


// Methods for the archetype iterator

public unsafe static class ArchetypeIteratorExt
{
    // Return the amount of archetypes
    // to iterate through

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(this ArchetypeIterator iter)
        => 1 << iter.Shift;


    // Returns the total size of the
    // archetype, that the iterator
    // processes

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TotalLength(this ArchetypeIterator iter)
        => iter.Archetype->collection.Length();


    // Returns the address of the
    // component from the current archetype

    public static void* GetComponent(this ArchetypeIterator iter, int index, int componentID)
    {
        // Get the address of the
        // array, to retrieve the
        // component from

        nuint array = (nuint)iter.Archetype->collection.GetArray(iter.Shift);


        // Get the index of the
        // componentoffset to retrieve

        int compOffsetInd = 0;

        for(int c = componentID / 64 - 1; c > -1; c--)
            compOffsetInd = BitOperations.PopCount(iter.Archetype->ComponentMask[c]);
        
        {
            byte highBitsToElim = (byte)(63 - (byte)componentID);

            ulong result = iter.Archetype->ComponentMask[componentID / 64] << highBitsToElim;

            compOffsetInd += BitOperations.PopCount(result);

            compOffsetInd -= compOffsetInd == 0 ? 0 : 1;
        }


        // Return the address of the
        // desired component

        return (void*)(array + (nuint)(index * iter.Archetype->Stride + iter.ComponentOffsets[compOffsetInd]));
    }


    // Returns the ID of the
    // entity related to the
    // current archetype

    public static int GetEntityID(this ArchetypeIterator iter, int index)
    {
        // Get the address of the
        // array, to retrieve the
        // entity ID from

        nuint array = (nuint)iter.Archetype->collection.GetArray(iter.Shift);


        // Return the address of the
        // desired entity ID

        return *(int*)(array + (nuint)(index * iter.Archetype->Stride));
    }
}


// Represents an object
// in the gameworld

public unsafe struct Entity
{
    // The name of the entity

    public nint Name;


    // A reference to the
    // parent of the entity

    public int Parent;


    // A list of references to the
    // children of the entity
    // (Compact Array)

    public int* Children;


    // The ID of the archetype,
    // that stores the components
    // of the entity

    public int ArchetypeID;


    // The index at which the
    // components of the entity
    // are stored at

    public int Index;
}


// Represents the relevant
// information of a component

public unsafe struct Component
{
    // The size of the component in bytes

    public int Size;


    // Reference to the initialiser
    // of the component

    public delegate*<int, void*, void> Init;

    
    // Reference to the finaliser
    // of the component

    public delegate*<int, void*, void> Fin;
}

/// <summary>
/// Structs that are given this
/// attribute will count as components.
/// </summary>

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ComponentAttribute : Attribute;


// Processes the data of archetypes

public unsafe struct System
{
    // A mask that represents the
    // components, that the system
    // is capable to process

    public ulong* ComponentMask;


    // The state of the system

    public SystemState State;


    // Update is called every frame

    public delegate*<ArchetypeIterator*, void> Update;

    // Broad and narrow phase are called
    // every physics frame

    public delegate*<ArchetypeIterator*, void> BroadPhase, NarrowPhase;

    // Pre render, render and post render
    // are called every update

    public delegate*<ArchetypeIterator*, void> PreRender, Render, PostRender;
}


// Methods that are given this
// attribute will count as systems

[AttributeUsage(AttributeTargets.Class)]
public unsafe sealed class SystemAttribute : Attribute
{
    /// Instance initialiser
    /// <param name="runExactArchetype">
    /// If true, the system will be forced to
    /// run on archetypes that have the exact
    /// components as the system.
    /// </param>
    /// <param name="denyLesserSystems">
    /// If true, the system will be used as a
    /// baseline for all archetypes, that have
    /// more, or the exact components as the system.
    /// Systems that have a few of the same components
    /// as the archetype, but less than the baseline system
    /// will be discarded by the archetype. 
    /// </param>

    public SystemAttribute(bool runExactArchetype, bool denyLesserSystems)
    {
        State |= *(SystemState*)&runExactArchetype & SystemState.RunExactArchetype;

        State |= (SystemState)(*(byte*)&denyLesserSystems << 1) & SystemState.DenyLesserSystems;
    }


    // The state of the system

    public readonly SystemState State;
}


// Represents the different states,
// that a system can assume

[Flags]
public enum SystemState : byte
{
    // Defines a lack of state for the respective system.

    None = 0,

    // Systems with this state will only run
    // on archetypes, that have the same exact
    // component mask as themselves

    RunExactArchetype = 1,

    // Systems with this state will become a baseline
    // for all archetypes, that have a component mask, where
    // the system can fit into. Systems with less components
    // will be discarded from archetypes.

    DenyLesserSystems = 2,


}