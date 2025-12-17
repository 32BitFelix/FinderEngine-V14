

using Core.MemoryManagement;

namespace Core.LECS;


// Entities represent the
// objects in the level

public unsafe struct Entity
{
    // The name of the entity
    // (As some COM thingy, idk)

    public char* Name;


    // The entity ID of
    // the entity's parent

    public int Parent;

    // The IDs of all entities
    // parented by the entity
    // (Chunk array)

    public SplitArray<int> Children;

    // A helper constant for keeping
    // track of the total amount of child references
    // within a chunk
    public const int ChildrenPerChunk = 8;


    // The ID of the archetype,
    // that the entity currently
    // belongs to

    public int ArchetypeID;


    // The index at which the
    // entity's components reside
    // at the corresponding archetype

    public int ArchetypeIndex;
}


// Structs fit with this attribute
// will be treated as components by the engine

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ComponentAttribute : Attribute;

// Components represent individual
// types of data, that collectively
// define the behaviour of entities

public unsafe struct Component
{
    // References to the initialisers
    // and finalisers of the component type

    public nuint Init, Fin;

    // The size of the component
    // type in bytes
    
    public int Size;

    // Stores the sizes
    // of each non static field of the
    // component type
    // (Compact array)

    public int* FieldSizes;
}


// Archetpes represent a collection
// of components, that are uniquely
// processed by systems depending
// on their composition of components

public unsafe struct Archetype
{
    // References to systems,
    // that have a component mask
    // fitting to the one that the
    // archetype has
    // (Compact array)

    public System* Systems;


    // A simple lock to indicate,
    // that the data array of the
    // archetype is being used

    public int Processed;


    // References the array
    // that hold the components,
    // aswell as related entity IDs
    //
    // Memory layout is as follows:
    // -------------------------------------------------------------------------------------------
    // length (int) | entity IDs (int * simdOps) | Components | entity IDs (int * simdOps) | . . .
    // -------------------------------------------------------------------------------------------

    public byte* Data;


    // A precalculated stride
    // for skipping a component chunk

    public int ComponentChunkStride;


    // A mask representing the
    // components composing this
    // archetype

    public ulong* ComponentMask;
}