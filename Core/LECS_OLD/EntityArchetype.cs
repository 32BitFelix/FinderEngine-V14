
using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace Core.LECS_OLD;


public unsafe static partial class Engine
{

    // Creates a specified amount of entities
    // and copies their references to the
    // given int pointer

    public static void CreateEntities(int* saveTo, int length)
    {

        // Iterate through each index
        // that has been specified

        for(int e = 0; e < length; e++)
        {
            // The entity to create

            Entity nEntity;


            // Save the name of the entity

            nEntity.Name = (char*)Marshal.StringToCoTaskMemUni("NONAME");


            // Initialise the array
            // that holds the references
            // to the entity's bound children

            ChunkArray.Create(&nEntity.Children, 0);


            // Set the entity's
            // parent to the root

            nEntity.Parent = 0;


            // Set the archetype ID
            // to the default archetype

            nEntity.ArchetypeID = 0;


            // Set the index of the
            // entity within it's
            // archetype to zero

            nEntity.ArchetypeIndex = 0;
        }


    }


    // Creates an entity

    /*public static int CreateEntity(string name = "NONAME")
    {
        // The entity to create

        Entity nEntity;


        // Save the name of the entity

        nEntity.Name = (char*)Marshal.StringToCoTaskMemUni(name);

        // Allocate an array for
        // holding the references
        // of the entity's children

        LockArray<int>.Create(&nEntity.Children, 0);

        // Make it clear, that
        // the entity isn't a
        // child of a parent... yet

        nEntity.Parent = 0;

        // The entity is part
        // of the default
        // archetype, as of now

        nEntity.ArchetypeID = 0;

        // The index the entity
        // would occupy in it's
        // current archetype

        nEntity.ArchetypeIndex = 0;


        // Try to find a free index
        // in the entity list


        // Get a reference to the entity list
        LockArray<Entity>* ents = & levels[currentLevelIndex].Entities;

        // Retry point of the search, if
        // finding a free index happens to fail
        retry:

        // Iterate through each chunk
        // of the entity list
        for(int i = 0; i < ents->Length; i++)
        {
            // Get the reference of the chunk
            Entity* ptr = LockArray<Entity>.GetLock(ents, i);

            // Iterate through each entity
            // of the current chunk
            for(int j = 0; j < LockArray<Entity>.ChunkSize; j++)
            {
                // Skip to the next entity,
                // if the current index is
                // not free
                if(ptr[j].Name != null)
                    continue;

                // Calculate the true index
                // of the current entity
                int index = i * LockArray<Entity>.ChunkSize + j;

                // The current method of avoiding
                // the null entity is slow and affects
                // all entity creations

                // Skip to the next iteration,
                // if the current index is the
                // null entity
                if(index == 0)
                    continue;

                // Save the new entity
                ptr[j] = nEntity;

                // Free the current chunk
                LockArray<Entity>.ReleaseLock(*ents, i);

                // Return the index
                // of the entity
                return index;
            }

            // Free the current chunk
            LockArray<Entity>.ReleaseLock(*ents, i);
        }


        // If we come this far, the
        // array will be resized
        // and the search for a free
        // index will begin once again


        LockArray<Entity>.Resize(ents, ents->Length + 1);


        goto retry;
    }*/


    // Deletes an entity from the
    // given index

    /*public static void DeleteEntity(int eID)
    {
        // Prematurely end the method,
        // if the given entity isn't real

        if(eID == 0)
            return;


        // Prematurely end the
        // method, if the given
        // entity isn't defined

        if(&levels[currentLevelIndex].Entities.Elements[eID].Name == null)
            return;


        // Get a reference to the
        // entities list

        LockArray<Entity>* ents = &levels[currentLevelIndex].Entities;

        // Get the index of the chunk,
        // that the entity is in

        int chunkIndex = eID / LockArray<Entity>.ChunkSize;


        // Unbind the entity
        // from it's parent,
        // if it has one

        if(ents->Elements[eID].Parent != 0)
            UnbinChild(ents->Elements[eID].Parent, eID);


        // Delete the children of
        // the entity

        for(int i = 0; i < ents->Elements[eID].Children.Length; i++)
            if(ents->Elements[eID].Children.Elements[i] != 0)
                DeleteEntity(ents->Elements[eID].Children.Elements[i]);


        // Delete the components
        // related to the entity

        


        // Set the name of the entity
        // to null, to signalise, that
        // it is free to be reused

        Entity* ptr = LockArray<Entity>.GetLock(&levels[currentLevelIndex].Entities, chunkIndex);

        ptr[eID - chunkIndex * LockArray<Entity>.ChunkSize].Name = null;

        LockArray<Entity>.ReleaseLock(levels[currentLevelIndex].Entities, chunkIndex);
    }*/


    // Binds the given child entity
    // to the given parent entity

    /*public static void BindChild(int parent, int child)
    {
        {
            // Prematurely end the method,
            // if the parent or child aren't real

            bool check = parent == 0;

            check |= child == 0;

            if(check)
                return;


            // Prematurely end the method,
            // if the parent or child aren't defined

            check = levels[currentLevelIndex].Entities.Elements[parent].Name == null;

            check |= levels[currentLevelIndex].Entities.Elements[child].Name == null;

            if(check)
                return;
        }


        // Get a reference to the
        // entities list

        LockArray<Entity>* ents = &levels[currentLevelIndex].Entities;


        // Get the index of the chunk,
        // that the parent is in

        int chunkIndex = parent / LockArray<Entity>.ChunkSize;


        // Lock the chunk, where the
        // parent is in and get the
        // reference of the parent

        Entity* ptr = LockArray<Entity>.GetLock(ents, chunkIndex);

        ptr = &ptr[parent - chunkIndex * LockArray<Entity>.ChunkSize]; // See what i did there?


        // Try to add the child to
        // the parent's children list

        // Point of retry, if the
        // children list had to
        // be resized
        retry:

        // Iterate through each block
        // of the entity's children list
        for(int i = 0; i < ptr->Children.Length; i++)
        {
            // Get a reference to the
            // current block
            int* cPtr = LockArray<int>.GetLock(&ptr->Children, i);

            // Iterate through each child
            // in the chunk
            for(int j = 0; j < LockArray<int>.ChunkSize; j++)
            {
                // Skip to the next iteration,
                // if the current index is not free
                if(cPtr[j] != 0)
                    continue;
                
                // Calculate the new index
                // of the child
                int index = i * LockArray<int>.ChunkSize + j;

                // Save the reference to
                // the child
                ptr->Children.Elements[index] = child;

                // Release the lock of the parent's chunk
                LockArray<Entity>.ReleaseLock(*ents, chunkIndex);

                // Release the lock of the
                // current chunk
                LockArray<int>.ReleaseLock(ptr->Children, i);

                // Prematurely
                // end the method
                return;
            }

            // Release the lock of the
            // current chunk
            LockArray<int>.ReleaseLock(ptr->Children, i);
        }

        // Resize the array,
        // so that it can fit
        // the new children 
        LockArray<int>.Resize(&ptr->Children, ptr->Children.Length + 1);

        // Go back and try
        // to find a free
        // slot once more
        goto retry;
    }*/


    // Returns a heap allocated
    // integer array, that holds
    // a copy of what the given
    // entity had as children at
    // the moment of calling

    /*public static int* ShowChildren(int eID)
    {
        // Prematurely end the method,
        // if the given entity isn't real

        if(eID == 0)
            return null;


        // Prematurely end the
        // method, if the given
        // entity isn't defined

        if(&levels[currentLevelIndex].Entities.Elements[eID].Name == null)
            return null;


        // Get a reference to the
        // entities list

        LockArray<Entity>* ents = &levels[currentLevelIndex].Entities;


        // Get the index of the chunk,
        // that the parent is in

        int chunkIndex = eID / LockArray<Entity>.ChunkSize;


        // Lock the chunk, where the
        // parent is in and get the
        // reference of the parent

        Entity* ptr = LockArray<Entity>.GetLock(ents, chunkIndex);

        ptr = &ptr[eID - chunkIndex * LockArray<Entity>.ChunkSize]; // See what i did there?  


        int* toReturn = (int*)NativeMemory.Alloc(sizeof(int) * (nuint)(ptr->Children.Length * LockArray<int>.ChunkSize + 1));

        toReturn[0] = ptr->Children.Length * LockArray<int>.ChunkSize;


        for(int i = 0; i < ptr->Children.Length; i++)
        {
            int* cPtr = LockArray<int>.GetLock(&ptr->Children, i);


            for(int j = 0; j < LockArray<int>.ChunkSize; j++)
                toReturn[i * LockArray<int>.ChunkSize + j + 1] = cPtr[j];


            LockArray<int>.ReleaseLock(ptr->Children, i);
        } 


        LockArray<Entity>.ReleaseLock(*ents, chunkIndex);


        return toReturn;
    }*/


    // Unbind the given child entity
    // from the given parent entity

    /*public static void UnbinChild(int parent, int child)
    {
        {
            // Prematurely end the method,
            // if the parent or child aren't real

            bool check = parent == 0;

            check |= child == 0;

            if(check)
                return;


            // Prematurely end the method,
            // if the parent or child aren't defined

            check = levels[currentLevelIndex].Entities.Elements[parent].Name == null;

            check |= levels[currentLevelIndex].Entities.Elements[child].Name == null;

            if(check)
                return;
        }


        // Get a reference to the
        // entities list

        LockArray<Entity>* ents = &levels[currentLevelIndex].Entities;


        // Get the index of the chunk,
        // that the parent is in

        int chunkIndex = parent / LockArray<Entity>.ChunkSize;


        // Lock the chunk, where the
        // parent is in and get the
        // reference of the parent

        Entity* ptr = LockArray<Entity>.GetLock(ents, chunkIndex);

        ptr = &ptr[parent - chunkIndex * LockArray<Entity>.ChunkSize]; // See what i did there?


        // Iterate through each chunk
        // of the parent's child list
        for(int i = 0; i < ptr->Children.Length; i++)
        {
            // Get the reference of the
            // current chunk
            int* cPtr = LockArray<int>.GetLock(&ptr->Children, i);

            // Iterate through each child
            // in the current chunk
            for(int j = 0; j < LockArray<int>.ChunkSize; j++)
            {
                // Skip to the next iteration,
                // if the current child isn't the
                // same as the given one
                if(cPtr[j] != child)
                    continue;

                // Remove the reference
                // to the child
                cPtr[j] = 0;

                // Release the lock of the
                // paren't chunk
                LockArray<Entity>.ReleaseLock(*ents, chunkIndex);

                // Release the lock of the
                // current chunk
                LockArray<int>.ReleaseLock(ptr->Children, i);

                return;
            }

            // Release the lock of the
            // currnet chunk
            LockArray<int>.ReleaseLock(ptr->Children, i);
        }

        // Release the lock of the
        // parent's chunk
        LockArray<Entity>.ReleaseLock(*ents, chunkIndex);


        // Blare an error


    }*/
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
    // (Chunk array)
    
    public int* Children;


    // References to
    // the entity that
    // this entity is
    // bound to

    public int Parent;


    // The archetype the
    // entity belongs to

    public int ArchetypeID;


    // The index, in the
    // archetype, that the
    // entity belongs to

    public int ArchetypeIndex;
}


// Represents a group
// of components

unsafe struct Archetype
{
    // A list of systems
    // conforming to the
    // archetype
    // (Compact array)

    public System* Systems;


    // The list holding all
    // components and respective
    // entity IDs in sequence.
    // The data is laid in following order:
    // --------------------------------------------------------------------------------
    // Length (int) | entity ID (int) | Components | entity ID (int) | Components . . .
    // --------------------------------------------------------------------------------

    public byte* Data;


    // The mask shows which
    // components are
    // related to this archetype

    public int* ComponentMask; 
}