


using System.Runtime.InteropServices;
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
    // Sorts the given compact array
    // with the radix sorting algorithm

    public static void RadixSortST(int* array)
    {

        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        int* swapBuffer = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * CompactArray.Length(array)));


        // Initialise the counter for keeping
        // track of multiple of a certain byte value
        
        int* counter = stackalloc int[256];

        // Initialise the table for keeping
        // track of at what index the values
        // will be set for each iteration

        int* offsetTable = stackalloc int[256];


        // The loop to iterate through each pass

        for(byte p = 0; p < 3; p++)
        {
            // Reset the counter

            for(byte i = 0; i < 256 / 2; i++)
                ((long*)counter)[i] ^= ((long*)counter)[i];


            // Reset the offset table

            offsetTable[0] ^= offsetTable[0];


            // Evaluate the source and target array

            int* source;

            int* target;

            {
                bool swapIndicator = (p & 1) == 0;

                source = swapIndicator ? array : swapBuffer;

                target = swapIndicator ? swapBuffer : array;
            }


            // Count the instances

            for(int i = 0; i < CompactArray.Length(array); i++)
            {
                byte val = (byte)(source[i] >> (8 * p));                

                counter[val]++;
            }


            // Build the offset table

            for(int i = 1; i < 256; i++)
                offsetTable[i] = offsetTable[i - 1] + counter[i - 1];

            
            // Save the values at their
            // orderly index

            for(int i = 0; i < CompactArray.Length(array); i++)
            {
                byte val = (byte)(source[i] >> (8 * p));                

                target[offsetTable[val]++] = source[i];
            }
        }


        // The final pass


        // Reset the counter

        for(byte i = 0; i < 256 / 2; i++)
            ((long*)counter)[i] ^= ((long*)counter)[i];


        // Count the instances

        for(int i = 0; i < CompactArray.Length(array); i++)
        {
            byte val = (byte)(swapBuffer[i] >> 24);                

            counter[val]++;
        }


        // Count the amount of negative values

        int numNeg = 0;

        for(int i = 128; i < 256; i++)
            numNeg += counter[i];

        
        // Reset the offset table
        // for the positive portion

        offsetTable[0] = numNeg;

        // Build the offset table
        // for the positive values

        for(int i = 1; i < 128; i++)
            offsetTable[i] = offsetTable[i - 1] + counter[i - 1];    


        // Reset the offset table
        // for the negative portion

        offsetTable[128] ^= offsetTable[128];

        // Build the offset table
        // for the positive values

        for(int i = 129; i < 256; i++)
            offsetTable[i] = offsetTable[i - 1] + counter[i - 1]; 


        // Save the values at their
        // orderly index

        for(int i = 0; i < CompactArray.Length(array); i++)
        {
            byte val = (byte)(swapBuffer[i] >> 24);                

            array[offsetTable[val]++] = swapBuffer[i];
        }   


        // Free the swapbuffer

        NativeMemory.Free(swapBuffer);
    }


    // Sorts the given compact array
    // with the radix sorting algorithm

    public static void RadixSortMT(int* array)
    {

        // This and the given array will
        // be used interchangeably for storing
        // the results of each radix

        int* swapBuffer = (int*)NativeMemory.Alloc((nuint)(sizeof(int) * CompactArray.Length(array)));


        // Initialise the counter for keeping
        // track of multiple of a certain byte value
        
        int* counter = stackalloc int[256];

        // Initialise the table for keeping
        // track of at what index the values
        // will be set for each iteration

        int* offsetTable = stackalloc int[256];


        countInstanceOverload* cIOverloads;

        orderOverload* oOverload;

        int freeThreads = JobCenter.FreeThreadCount();




        // The loop to iterate through each pass

        for(byte p = 0; p < 3; p++)
        {
            // Reset the counter

            for(byte i = 0; i < 256 / 2; i++)
                ((long*)counter)[i] ^= ((long*)counter)[i];


            // Reset the offset table

            offsetTable[0] ^= offsetTable[0];


            // Evaluate the source and target array

            int* source;

            int* target;

            {
                bool swapIndicator = (p & 1) == 0;

                source = swapIndicator ? array : swapBuffer;

                target = swapIndicator ? swapBuffer : array;
            }


            /*// Count the instances

            for(int i = 0; i < CompactArray.Length(array); i++)
            {
                byte val = (byte)(source[i] >> (8 * p));                

                counter[val]++;
            }*/


            // Build the offset table

            for(int i = 1; i < 256; i++)
                offsetTable[i] = offsetTable[i - 1] + counter[i - 1];

            
            /*// Save the values at their
            // orderly index

            for(int i = 0; i < CompactArray.Length(array); i++)
            {
                byte val = (byte)(source[i] >> (8 * p));                

                target[offsetTable[val]++] = source[i];
            }*/
        }


        // The final pass


        // Reset the counter

        for(byte i = 0; i < 256 / 2; i++)
            ((long*)counter)[i] ^= ((long*)counter)[i];


        // Count the instances

        for(int i = 0; i < CompactArray.Length(array); i++)
        {
            byte val = (byte)(swapBuffer[i] >> 24);                

            counter[val]++;
        }


        // Count the amount of negative values

        int numNeg = 0;

        for(int i = 128; i < 256; i++)
            numNeg += counter[i];

        
        // Reset the offset table
        // for the positive portion

        offsetTable[0] = numNeg;

        // Build the offset table
        // for the positive values

        for(int i = 1; i < 128; i++)
            offsetTable[i] = offsetTable[i - 1] + counter[i - 1];    


        // Reset the offset table
        // for the negative portion

        offsetTable[128] ^= offsetTable[128];

        // Build the offset table
        // for the positive values

        for(int i = 129; i < 256; i++)
            offsetTable[i] = offsetTable[i - 1] + counter[i - 1]; 


        // Save the values at their
        // orderly index

        for(int i = 0; i < CompactArray.Length(array); i++)
        {
            byte val = (byte)(swapBuffer[i] >> 24);                

            array[offsetTable[val]++] = swapBuffer[i];
        }   


        // Free the swapbuffer

        NativeMemory.Free(swapBuffer);

        
        // A job for counting instances

        static void countInstance(countInstanceOverload* overload)
        {
            for(int i = overload->length - 1; i > -1; i--)
            {
                byte val = (byte)(overload->array[i] >> overload->shift);                

                overload->counter[val]++;
            }
        }


        // A job for ordering the elements

        static void order(orderOverload* overload)
        {
            for(int i = overload->length - 1; i > -1; i--)
            {
                byte val = (byte)(overload->source[i] >> overload->shift);                

                overload->target[overload->offsetTable[val]++] = overload->source[i];
            }   
        }
    }


    private struct countInstanceOverload
    {
        public int* counter;
        
        public int* array;
        
        public int length;
        
        public byte shift;
    }


    private struct orderOverload
    {
        public int* offsetTable;

        public int* source;

        public int* target;

        public int length;

        public byte shift;
    }
}