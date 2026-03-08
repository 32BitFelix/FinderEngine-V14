


using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Core.MemoryManagement;

namespace Core.Algorithms;


public struct EntityPoint
{
    public int Entity;

    public float Point;
}


// A helper class for sporting
// sorting algorithms

public unsafe static class Sorting
{
    // A slightly less space efficient
    // approach to radix sort, inspired
    // by Michael Herf's approach

    public static void RadixSort(EntityPoint* array, int length)
    {
        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        EntityPoint* swapBuffer = (EntityPoint*)NativeMemory.Alloc((nuint)(sizeof(EntityPoint) * length));


        int* histogram0 = stackalloc int[256];

        histogram0[0] = -1;

        int* histogram1 = stackalloc int[256];

        histogram1[0] = -1;

        int* histogram2 = stackalloc int[256];

        histogram2[0] = -1;

        int* histogram3 = stackalloc int[256];

        histogram3[128] = -1;


        Sse.PrefetchNonTemporal(array);


        // Set the records of the histograms

        for(int i = length - 1; i > -1; i--)
        {
            int val = *(int*)&array[i].Point;


            histogram0[(byte)val]++;

            val >>= 8;


            histogram1[(byte)val]++;

            val >>= 8;


            histogram2[(byte)val]++;

            val >>= 8;


            histogram3[(byte)val]++;
        }


        // Sum the records of the histograms
        // with their previous ones

        for(int i = 1; i < 256; i++)
        {
            histogram0[i] += histogram0[i - 1];

            histogram1[i] += histogram1[i - 1];

            histogram2[i] += histogram2[i - 1];

            histogram3[(byte)(i + 128)] += histogram3[(byte)((byte)(i + 128) - 1)];
        }


        Sse.PrefetchNonTemporal(array);

        Sse.PrefetchNonTemporal(swapBuffer);


        // The first pass

        Sse.Prefetch0(histogram0);

        for(int i = length - 1; i > -1; i--)
        {
            byte val = (byte)*(int*)&array[i].Point;

            swapBuffer[histogram0[val]--] = array[i];
        }

        // The second pass

        Sse.Prefetch0(histogram1);

        for(int i = length - 1; i > -1; i--)
        {
            byte val = (byte)(*(int*)&swapBuffer[i].Point >> 8);

            array[histogram1[val]--] = swapBuffer[i];
        }

        // The third pass

        Sse.Prefetch0(histogram2);

        for(int i = length - 1; i > -1; i--)
        {
            byte val = (byte)(*(int*)&array[i].Point >> 16);

            swapBuffer[histogram2[val]--] = array[i];
        }

        // The fourth pass

        Sse.Prefetch0(histogram3);

        for(int i = length - 1; i > -1; i--)
        {
            byte val = (byte)(*(int*)&swapBuffer[i].Point >> 24);

            array[histogram3[val]--] = swapBuffer[i];
        }


        // Free the swapbuffer

        NativeMemory.Free(swapBuffer);
    }


    // A slightly less space efficient
    // approach to radix sort, inspired
    // by Michael Herf's approach

    public static void RadixSort(int* array)
    {
        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        int* swapBuffer = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * CompactArray.Length(array)));


        int* histogram0 = stackalloc int[256];

        histogram0[0] = -1;

        int* histogram1 = stackalloc int[256];

        histogram1[0] = -1;

        int* histogram2 = stackalloc int[256];

        histogram2[0] = -1;

        int* histogram3 = stackalloc int[256];

        histogram3[128] = -1;


        Sse.PrefetchNonTemporal(array);


        // Set the records of the histograms

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            int val = array[i];


            histogram0[(byte)val]++;

            val >>= 8;


            histogram1[(byte)val]++;

            val >>= 8;


            histogram2[(byte)val]++;

            val >>= 8;


            histogram3[(byte)val]++;
        }


        // Sum the records of the histograms
        // with their previous ones

        for(int i = 1; i < 256; i++)
        {
            histogram0[i] += histogram0[i - 1];

            histogram1[i] += histogram1[i - 1];

            histogram2[i] += histogram2[i - 1];

            histogram3[(byte)(i + 128)] += histogram3[(byte)((byte)(i + 128) - 1)];
        }


        Sse.PrefetchNonTemporal(array);

        Sse.PrefetchNonTemporal(swapBuffer);


        // The first pass

        Sse.Prefetch0(histogram0);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)array[i];

            swapBuffer[histogram0[val]--] = array[i];
        }

        // The second pass

        Sse.Prefetch0(histogram1);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(swapBuffer[i] >> 8);

            array[histogram1[val]--] = swapBuffer[i];
        }

        // The third pass

        Sse.Prefetch0(histogram2);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(array[i] >> 16);

            swapBuffer[histogram2[val]--] = array[i];
        }

        // The fourth pass

        Sse.Prefetch0(histogram3);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(swapBuffer[i] >> 24);

            array[histogram3[val]--] = swapBuffer[i];
        }


        // Free the swapbuffer

        NativeMemory.Free(swapBuffer);
    }


    // A slightly less space efficient
    // approach to radix sort, inspired
    // by Michael Herf's approach.
    // This version is multithreaded
    // AND HIGHLY EXPERIMENTAL. It's
    // way slower than the single threaded implementation

    public static void RadixSortMT(int* array)
    {
        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        int* swapBuffer = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * CompactArray.Length(array)));


        int* histogram0 = stackalloc int[256];

        histogram0[0] = -1;

        int* histogram1 = stackalloc int[256];

        histogram1[0] = -1;

        int* histogram2 = stackalloc int[256];

        histogram2[0] = -1;

        int* histogram3 = stackalloc int[256];

        histogram3[128] = -1;


        // Evaluate the amount of currently
        // free threads and the amount of
        // elements each can process

        int threadCount = JobCenter.CountFreeThreads();

        int elementsPerThread = CompactArray.Length(array) / threadCount;


        // A counter for keeping track
        // of the threads completing their jobs

        int completionCounter = 0;


        // Split the elements through the threads

        setHistogramOverload* sHO = (setHistogramOverload*)NativeMemory.Alloc((nuint)(sizeof(setHistogramOverload) * threadCount));

        for(int i = threadCount - 1; i > -1; i--)
        {
            sHO[i].Array = &array[i * elementsPerThread];

            sHO[i].Length = elementsPerThread;

            sHO[i].Histogram0 = histogram0;

            sHO[i].Histogram1 = histogram1;

            sHO[i].Histogram2 = histogram2;

            sHO[i].Histogram3 = histogram3;

            sHO[i].CompletionCounter = &completionCounter;
        }

        // Append leftovers to the
        // last thread

        sHO[threadCount - 1].Length += CompactArray.Length(array) % threadCount;


        // Scedule and run the work

        {
            Job job = new()
            {
                Method = (delegate*<nuint, void>)(delegate*<setHistogramOverload*, void>)&setHistogram
            };

            for (int i = threadCount - 1; i > -1; i--)
            {
                job.Overload = (nuint)(&sHO[i]);

                JobCenter.RunJob(job);
            }
        }


        // Wait for all jobs to finish
        // and reset the counter afterwards

        while(completionCounter != threadCount);

        completionCounter ^= completionCounter;


        // Free the resources of the overload

        NativeMemory.Free(sHO);


        // Sum the records of the histograms
        // with their previous ones

        for(int i = 1; i < 256; i++)
        {
            histogram0[i] += histogram0[i - 1];

            histogram1[i] += histogram1[i - 1];

            histogram2[i] += histogram2[i - 1];

            histogram3[(byte)(i + 128)] += histogram3[(byte)((byte)(i + 128) - 1)];
        }


        // Now, do the ordering


        Sse.PrefetchNonTemporal(array);

        Sse.PrefetchNonTemporal(swapBuffer);


        // The first pass

        Sse.Prefetch0(histogram0);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)array[i];

            swapBuffer[histogram0[val]--] = array[i];
        }

        // The second pass

        Sse.Prefetch0(histogram1);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(swapBuffer[i] >> 8);

            array[histogram1[val]--] = swapBuffer[i];
        }

        // The third pass

        Sse.Prefetch0(histogram2);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(array[i] >> 16);

            swapBuffer[histogram2[val]--] = array[i];
        }

        // The fourth pass

        Sse.Prefetch0(histogram3);

        for(int i = CompactArray.Length(array) - 1; i > -1; i--)
        {
            byte val = (byte)(swapBuffer[i] >> 24);

            array[histogram3[val]--] = swapBuffer[i];
        }


        // Free the swapbuffer

        NativeMemory.Free(swapBuffer);
    }


    private struct orderElementsOverload
    {
        public volatile int* From, To;

        public volatile int* Histogram;

        public volatile int* CompletionCounter;

        public int Length;

        public byte Shift;
    }


    private struct setHistogramOverload
    {
        public int* Array;

        public volatile int* Histogram0, Histogram1, Histogram2, Histogram3;

        public volatile int* CompletionCounter;

        public int Length;
    }

    private static void setHistogram(setHistogramOverload* overload)
    {
        Sse.PrefetchNonTemporal(overload->Histogram0);

        Sse.PrefetchNonTemporal(overload->Histogram1);

        Sse.PrefetchNonTemporal(overload->Histogram2);

        Sse.PrefetchNonTemporal(overload->Histogram3);


        Sse.Prefetch0(overload->Array);


        for(int i = overload->Length - 1; i > -1; i--)
        {
            int val = overload->Array[i];


            Interlocked.Increment(ref overload->Histogram0[(byte)val]);

            val >>= 8;


            Interlocked.Increment(ref overload->Histogram1[(byte)val]);

            val >>= 8;


            Interlocked.Increment(ref overload->Histogram2[(byte)val]);

            val >>= 8;


            Interlocked.Increment(ref overload->Histogram3[(byte)val]);
        }


        Interlocked.Increment(ref Unsafe.AsRef<int>(overload->CompletionCounter));
    }
}