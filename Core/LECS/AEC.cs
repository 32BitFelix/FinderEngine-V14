

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

    public int* Children;


    // The ID of the archetype,
    // that the entity currently
    // belongs to

    public int ArchetypeID;


    // The index at which the
    // entity's components reside
    // at the corresponding archetype

    public int ArchetypeIndex;
}


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

    public System* Systems;


    // References the array
    // that hold the components,
    // aswell as related entity IDs

    public byte* Data;


    // A mask representing the
    // components composing this
    // archetype

    public long* ComponentMask;
}