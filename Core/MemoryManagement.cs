
using System.Numerics;
using System.Runtime.CompilerServices;
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


// Sketch for alternate lock array. Consult for next iteration
// ------------------------------------------------------------------------------------------------------------
// absolute thread ID (4 bytes) | array length (4 bytes) | lock (4 bytes) | elements (???) | lock (4 bytes) . . .
// ------------------------------------------------------------------------------------------------------------


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


