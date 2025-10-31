
namespace Core.LECS;


public unsafe static partial class Engine
{

    // Creates an entity

    public static int CreateEntity(string name)
    {



        return 0;
    }

}


// An entity is an
// object withing
// the game world

unsafe struct Entity
{
    // The name of the
    // entity

    public char* Name;


    // References of
    // children bound
    // to the entity
    
    public int* Children;


    // References to
    // the entity that
    // this entity is
    // bound to

    public int Parent;


    // The archetype the
    // entity belongs to

    public Archetype* Archetype;
}


// Represents a group
// of components

unsafe struct Archetype
{
    // A list of systems
    // conforming to the
    // archetype

    public readonly System* Systems;


    // The list holding all
    // components and respective
    // entity IDs in sequence.
    // The data is laid in following order:
    // -----------------------------------------------------------------
    // entity ID (int) | Components | entity ID (int) | Components . . .
    // -----------------------------------------------------------------

    public byte* Data;

    // The amount of
    // component combinations

    public int DataLen;


    // The size in bytes of
    // a stride

    public int StrideLen;


    // The indices of the components
    // contained within the archetype

    public int* ComponentIndices;
}