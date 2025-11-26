
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static System.Runtime.InteropServices.NativeMemory;

namespace Core.MemoryManagement;


// A helper class for
// managing compact arrays.
//
// Compact arrays are special pointers,
// where they contain the elements and
// it's respective length, like so:
//------------------------------------
// Length (int) | Elements (???)
//------------------------------------
//              ^ Returned pointer address

public static unsafe class CompactArray
{

    // Creates a compact array
    // with the given length

    public static T* Create<T>(int len = 0)
        where T : unmanaged
    {
        // Allocate the pointer

        T* ptr = (T*)Alloc((nuint)((sizeof(T) * len) + sizeof(int)));

        // Save the length
        // of the pointer

        *(int*)ptr = len;

        // Shift the pointer ahead

        *(nuint*)&ptr += sizeof(int);

        // Return the pointer

        return ptr;
    }


    // Creates a compact array
    // with the given length,
    // within aligned memory

    public static T* CreateAligned<T>(int len = 0)
        where T : unmanaged
    {
        // Allocate the pointer

        T* ptr = (T*)AlignedAlloc((nuint)((sizeof(T) * len) + sizeof(int)), (nuint)sizeof(T));

        // Save the length
        // of the pointer

        *(int*)ptr = len;

        // Shift the pointer ahead

        *(nuint*)&ptr += sizeof(int);

        // Return the pointer

        return ptr;
    }


    // A helper method
    // for finding the
    // length of a compact array

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length<T>(T* ptr)
        where T : unmanaged
    {
        // Shift the pointer back

        nuint n = *(nuint*)&ptr -= sizeof(int);

        // Return the length

        return *(int*)n;
    }


    // Resizes the given
    // compact array to
    // the given length. 

    public static void Resize<T>(T** ptr, int nLen)
        where T : unmanaged
    {
        // Get the compact
        // array

        T* normPtr = *ptr;

        // Shift the pointer back

        *(nuint*)&normPtr -= sizeof(int);

        // Check, if it doesn't have
        // the same length as the
        // given length

        if(*(int*)&normPtr == nLen)
            return;

        // Resize the compact array

        normPtr = (T*)Realloc(normPtr, (nuint)(sizeof(T) * nLen + sizeof(int)));

        // Save the new length

        *(int*)normPtr = nLen;

        // Shift the compact
        // array ahead

        *(nuint*)&normPtr += sizeof(int);

        // Save the resized
        // compact array

        *ptr = normPtr;
    }


    // Resizes the given
    // compact array to
    // the given length
    // of aligned memory

    public static void ResizeAligned<T>(T** ptr, int nLen)
        where T : unmanaged
    {
        // Get the compact
        // array

        T* normPtr = *ptr;

        // Shift the pointer back

        *(nuint*)&normPtr -= sizeof(int);

        // Check, if it doesn't have
        // the same length as the
        // given length

        if(*(int*)&normPtr == nLen)
            return;

        // Resize the compact array

        normPtr = (T*)AlignedRealloc(normPtr, (nuint)(sizeof(T) * nLen + sizeof(int)), (nuint)sizeof(T));

        // Save the new length

        *(int*)normPtr = nLen;

        // Shift the compact
        // array ahead

        *(nuint*)&normPtr += sizeof(int);

        // Save the resized
        // compact array

        *ptr = normPtr;
    }


    // Deallocates the given
    // compact pointer

    public static void Delete<T>(T* ptr)
        where T : unmanaged
    {
        // Shift the pointer back

        *(nuint*)&ptr -= sizeof(int);

        // Deallocate the memory
        // of the pointer

        Free(ptr);
    }


    // Deallocates the given
    // compact pointer of aligned memory

    public static void DeleteAligned<T>(T* ptr)
        where T : unmanaged
    {
        // Shift the pointer back

        *(nuint*)&ptr -= sizeof(int);

        // Deallocate the memory
        // of the pointer

        AlignedFree(ptr);
    }
}


// TODO: Make the lockarray
// it's unique type of
// compact array

// Helper class for lock arrays
// A lock array holds locks
// for chunks of another array.
// (Extension of the compact array.
// Compact array must be aligned!)

public unsafe static class LockArrayExt
{
    // The amount of elements
    // affected by a lock.
    // Ideally, it should be
    // a multiple of 2, so that
    // the compiler can optimize
    // the block index calculation

    public const byte BlockSize = 16;


    // Blocks the calling thread,
    // until the lock of the desired
    // block is lifted. If the block
    // is free, it will be locked by
    // the calling thread, until it
    // releases it

    public static void GetLock(int index, int* lockArray)
    {
        // Calculate the index of the
        // accessed block

        int blockIndex = index / BlockSize + 1;


        // Save the managed thread ID
        // of the current thread 

        int currentThreadID = Environment.CurrentManagedThreadId;


        // Basic implementation of the compare-and-swap lock


        repeat:

        // Check, if the lock
        // of the given index
        // is released. If that is
        // the case, set the lock
        // to the current thread's ID.

        Interlocked.CompareExchange(ref lockArray[blockIndex], currentThreadID, 0);

        // See, if the current thread has
        // taken hold of the lock. If not,
        // the process will be repeated

        if(lockArray[blockIndex] != currentThreadID)
            goto repeat;
    }


    // The calling thread, that occupied the
    // lock of the desired block will release
    // the lock for other threads to occupy

    public static void ReleaseLock(int index, int* lockArray)
    {
        // Calculate the index of the
        // released block

        int blockIndex = index / BlockSize + 1;

        // Release the lock of
        // the given block

        lockArray[blockIndex] = 0;
    }


    // Blocks the calling thread,
    // until all locks have been
    // occupied by the calling thread

    public static void GetAllLocks(int* lockArray)
    {
        // Save the managed thread ID
        // of the current thread 

        int currentThreadID = Environment.CurrentManagedThreadId;


        // Block the method, until
        // absolute ownership has
        // been taken

        repeatAO:

        Interlocked.CompareExchange(ref lockArray[0], currentThreadID, 0);

        if(lockArray[0] != currentThreadID) 
            goto repeatAO;


        // A counter to keep track
        // of the occupied locks

        int occupiedCount = 0;


        // The following loop goes through
        // all locks, until they have been
        // occupied by the calling thread

        int i = 0;

        repeat:

        i++;


        Interlocked.CompareExchange(ref lockArray[i], currentThreadID, 0);

        if(lockArray[i] == currentThreadID)
            occupiedCount++;


        i %= CompactArray.Length(lockArray) - 1;


        // Repeat the loop, if all locks
        // haven't been occupied by the
        // calling thread

        if(occupiedCount != CompactArray.Length(lockArray) - 1)
            goto repeat;
    }


    // The specified array will be freed
    // from the absolute occupation of
    // the calling thread

    public static void ReleaseAllLocks(int* lockArray)
    {   
        // Freeing the blocks from occupation
        // in reverse order, to make sure all
        // locks are freed before lifting the
        // absolute occupation of the calling thread

        for(int i = CompactArray.Length(lockArray) - 1; i >= 0; i--)
            lockArray[i] = 0;
    }
} 


// Represents an array, whose
// elements are split into chunks,
// that can be locked with their
// individual locks.
// The locks of the compact array
// are always aligned

public unsafe struct LockArray<T>
    where T : unmanaged
{
    // The ID of the thread
    // assuming total occupation
    // of this array

    public int AbsoluteLock;

    // The length of the
    // array in chunks

    public int Length;

    // A pointer reference
    // to the elements of the array

    public T* Elements;

    // A pointer reference
    // to the locks of all
    // chunks of the array

    public int* Locks;


    // The size in elements
    // of a chunk.
    // Ideally, the size would
    // be a power of two, to
    // enable the compiler for
    // minor optimisations

    public const int ChunkSize = 16;


    // Creates a new lock array
    // instance to the given address

    public static void Create(LockArray<T>* array, int nLen = 0)
    {
        // Reset the absolute lock

        array->AbsoluteLock ^= array->AbsoluteLock;


        // Save the array's
        // length in chunks

        array->Length = nLen;


        // Allocate memory for the elements

        array->Elements = (T*)Alloc((nuint)(sizeof(T) * ChunkSize * nLen));

        // Set the elements to default

        for(int i = nLen * ChunkSize - 1; i >= 0; i--)
            array->Elements[i] = default;


        // Allocate memory for the locks
        // of the chunks

        array->Locks = (int*)AlignedAlloc((nuint)(sizeof(int) * nLen), sizeof(int));

        // Set the locks to zero

        for(int i = nLen; i >= 0; i--)
            array->Locks[i] ^= array->Locks[i];
    }   


    // Deletes the given lock array,
    // as soon as it isn't used.
    // Blocks, until the resources
    // have been freed

    public static void Delete(LockArray<T>* array)
    {
        // Try to get the
        // absolute lock

        int threadID = Environment.CurrentManagedThreadId;

        while(array->AbsoluteLock != threadID)
            Interlocked.CompareExchange(ref array->AbsoluteLock, threadID, 0);


        // Get the locks of
        // all the chunks

        {
            int occupiedCount = 0;

            for(int i = 0; occupiedCount < array->Length; i++, i %= array->Length)
            {
                Interlocked.CompareExchange(ref array->Locks[i], threadID, 0);

                if(array->Locks[i] == threadID)
                    occupiedCount++;
            }
        }


        // Finally, free the resources

        Free(array->Elements);

        AlignedFree(array->Locks);
    }


    // Resizes the given lock array
    // to the new length in chunks.
    // Blocks, until the array has
    // been successfully resized

    public static void Resize(LockArray<T>* array, int nLen)
    {
        // Prematurely end the method,
        // if the new length is the
        // same as the current one

        //if(nLen == array->Length)
        //    return;


        // Try to get the
        // absolute lock

        int threadID = Environment.CurrentManagedThreadId;

        while(array->AbsoluteLock != threadID)
            Interlocked.CompareExchange(ref array->AbsoluteLock, threadID, 0);


        // Get the locks of
        // all the chunks

        {
            int occupiedCount = 0;

            for(int i = 0; occupiedCount < array->Length; i++, i %= array->Length)
            {
                Interlocked.CompareExchange(ref array->Locks[i], threadID, 0);

                if(array->Locks[i] == threadID)
                    occupiedCount++;
            }
        }


        // Resize the element array
        // and default the new ones

        array->Elements = (T*)Realloc(array->Elements, (nuint)(sizeof(T) * ChunkSize * nLen));

        for(int i = array->Length * ChunkSize; i < nLen * ChunkSize; i++)
            array->Elements[i] = default;

        // Resize the lock array
        // and zero the new ones

        array->Locks = (int*)AlignedRealloc(array->Locks, sizeof(int) * (nuint)nLen, sizeof(int));

        for(int i = array->Length; i < nLen; i++)
            array->Locks[i] ^= array->Locks[i];


        // Save the new length
        // of the locked array

        array->Length = nLen;


        // Release the locks
        // of the chunks

        for(int i = 0; i < nLen; i++)
            array->Locks[i] = 0;


        // Release the absolute lock

        array->AbsoluteLock = 0;
    }


    // Returns the address of the
    // specified chunk, as soon
    // as the current thread
    // receives it's ownership

    public static T* GetLock(LockArray<T>* array, int index)
    {
        // Prematurely end the method,
        // if the index exceeds the bounds
        // of the given array

        //if((index < 0) || (index > array->Length))
        //    return;


        // Try to get the lock
        // of the requested chunk

        int threadID = Environment.CurrentManagedThreadId;

        while(array->Locks[index] != threadID)
            Interlocked.CompareExchange(ref array->Locks[index], threadID, 0);


        // Return the address of
        // the requested chunk

        return &array->Elements[ChunkSize * index];
    }


    // Releases the lock of the
    // chunk at the specified index
    // from the occupation of the
    // current thread

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReleaseLock(LockArray<T> array, int index)
    {
        // Prematurely end the method,
        // if the index exceeds the bounds
        // of the given array

        //if((index < 0) || (index > array.Length))
        //    return;


        // Prematurely end the method,
        // if the specified chunk's
        // lock wasn't even locked by the
        // current thread

        //int threadID = Environment.CurrentManagedThreadId;

        //if(array.Locks[index] != threadID)
        //    return;

        
        // Release the lock of
        // the chunk at the
        // given index

        array.Locks[index] = 0;
    }


    // The currently running thrread
    // will take absolute ownership
    // of all chunks in the lock array.
    // Blocks, until total ownership
    // has been taken

    public static void GetAbsoluteLock(LockArray<T>* array)
    {
        // Try to get the
        // absolute lock

        int threadID = Environment.CurrentManagedThreadId;

        while(array->AbsoluteLock != threadID)
            Interlocked.CompareExchange(ref array->AbsoluteLock, threadID, 0);


        // Get the locks of
        // all the chunks

        {
            int occupiedCount = 0;

            for(int i = 0; occupiedCount < array->Length; i++, i %= array->Length)
            {
                Interlocked.CompareExchange(ref array->Locks[i], threadID, 0);

                if(array->Locks[i] == threadID)
                    occupiedCount++;
            }
        }
    }


    // Releases the locks from all
    // chunks occupied by the
    // currently running thread.

    public static void ReleaseAbsoluteLock(LockArray<T>* array)
    {
        // Prematurely end the method,
        // if the array wasn't even
        // absolutely locked by the
        // current thread

        //int threadID = Environment.CurrentManagedThreadId;

        //if(array->AbsoluteLock != threadID)
        //    return;


        // Release the locks
        // of the chunks

        for(int i = 0; i < array->Length; i++)
            array->Locks[i] = 0;


        // Release the absolute lock

        array->AbsoluteLock = 0;
    }
}

// Experimental approach
// to a truly contigous
// array, that can be locked
// and is split into chunks.
public unsafe static class ChunkArray
{
    // The length of a chunk in elements
    public const byte ChunkLength = 16;


    // Represents a chunk
    private struct Chunk<T>
    {
        // The lock of a chunk
        public int ChunkLock;

        // The elements of a chunk
        public ChunkElements<T> Elements;
    }

    // Represents the elements
    // of a chunk
    [InlineArray(ChunkLength)]
    private struct ChunkElements<T>
    {
        // The value of a chunk
        public T Value;
    }


    // Represents the header
    // of a chunk array
    private struct Header
    {   
        // The length of a chunkarray
        // in chunks
        public int Length;

        // The absolute lock
        // of a chunk array
        public int AbsoluteLock;
    }


    // Creates a chunk array
    // with the given length
    // in chunks
    public static void Create<T>(T** array, int length = 0)
        where T : unmanaged
    {
        // Allocate the memory
        // of the chunk array

        *array = (T*)AlignedAlloc((nuint)(sizeof(Chunk<T>) * (length + 1)), sizeof(int));


        // Save the size of the
        // chunk array and reset
        // it'S lock

        ((Header*)*array)->Length = length;

        ((Header*)*array)->AbsoluteLock ^= ((Header*)*array)->AbsoluteLock;


        // Reset the locks of the chunks,
        // aswell as the elements

        for(int i = length; i >= 1; i--)
        {
            ((Chunk<T>*)*array)[i].ChunkLock ^= ((Chunk<T>*)*array)[i].ChunkLock;

            ((Chunk<T>*)*array)[i].Elements = default;
        }
    }


    // Resizes a chunk array
    // to the given length in chunks

    public static void Resize<T>(T** array, int length = 0)
        where T : unmanaged
    {
        // Prematurely end the method,
        // if the current and new lengths
        // are the same

        //if(length == ((Header*)*array)->Length)
        //    return;


        // Get the thread if of the
        // currently running thread

        int MTID = Environment.CurrentManagedThreadId;


        // Try to get the absolute lock

        while(((Header*)*array)->AbsoluteLock != MTID)
            Interlocked.CompareExchange(ref ((Header*)*array)->AbsoluteLock, MTID, 0);

        
        // Try to get each chunk lock

        {
            int occupied = 0;


            for(int i = 1; occupied < ((Header*)*array)->Length; i &= ((Header*)*array)->Length, i++)
            {
                Interlocked.CompareExchange(ref ((Chunk<T>*)*array)[i].ChunkLock, MTID, 0);

                if(((Chunk<T>*)*array)[i].ChunkLock == MTID)
                    occupied++;
            }
        }


        // Resize the array to the new desired size

        *array = (T*)AlignedRealloc(*array, (nuint)(sizeof(Chunk<T>) * (length + 1)), sizeof(int));


        // Clear the new chunks,
        // if there are any

        for(int i = ((Header*)*array)->Length; i < length; i++)
            ((Chunk<T>*)*array)[i + 1].Elements = default;


        // Save the new length of the array

        ((Header*)*array)->Length = length;


        // Release the absolute lock

        ((Header*)*array)->AbsoluteLock ^= ((Header*)*array)->AbsoluteLock;


        // Release all chunk locks

        for(int i = length; i >= 1; i--)
            ((Chunk<T>*)*array)[i].ChunkLock ^= ((Chunk<T>*)*array)[i].ChunkLock; 
    }


    // Frees the given chunk array.
    // First gets the lock of all
    // chunks, before releasing
    // all resources
    public static void Delete<T>(T** array)
        where T : unmanaged
    {
        // Get the thread if of the
        // currently running thread

        int MTID = Environment.CurrentManagedThreadId;


        // Try to get the absolute lock

        while(((Header*)*array)->AbsoluteLock != MTID)
            Interlocked.CompareExchange(ref ((Header*)*array)->AbsoluteLock, MTID, 0);

        
        // Try to get each chunk lock

        {
            int occupied = 0;


            for(int i = 1; occupied < ((Header*)*array)->Length; i &= ((Header*)*array)->Length, i++)
            {
                Interlocked.CompareExchange(ref ((Chunk<T>*)*array)[i].ChunkLock, MTID, 0);

                if(((Chunk<T>*)*array)[i].ChunkLock == MTID)
                    occupied++;
            }
        }


        // Finally, free the resources of the array

        AlignedFree(*array);
    }


    // Returns the length of the
    // given chunk array

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length<T>(T* array)
        where T : unmanaged

        => ((Header*)array)->Length;


    // Returns the address of the elements
    // of the given chunk in the array, for
    // only reading purposes
    public static T* ReadChunk<T>(T** array, int chunkIndex)
        where T : unmanaged
        
        => (T*)&((Chunk<T>*)*array)[chunkIndex + 1].Elements;


    // Locks a chunk and returns
    // the address to the elements
    // of the chunk. Blocks until
    // the lock of the chunk is
    // free to be occupied
    public static T* LockChunk<T>(T** array, int chunkIndex)
        where T : unmanaged
    {
        // Get the thread if of the
        // currently running thread

        int MTID = Environment.CurrentManagedThreadId;


        // Try to get the lock of
        // the given chunk

        while(((Chunk<T>*)*array)[chunkIndex + 1].ChunkLock != MTID)
            Interlocked.CompareExchange(ref ((Chunk<T>*)*array)[chunkIndex + 1].ChunkLock, MTID, 0);


        // Finally, return the
        // address of the first
        // element of the chunk

        return (T*)&((Chunk<T>*)*array)[chunkIndex + 1].Elements;
    }


    // Releases the given chunk
    // from the lock of the
    // calling thread
    public static void ReleaseChunk<T>(T** array, int chunkIndex)
        where T : unmanaged

        => ((Chunk<T>*)*array)[chunkIndex + 1].ChunkLock = 0;
}
