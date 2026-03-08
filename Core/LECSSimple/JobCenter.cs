

using System.Runtime.InteropServices;

namespace Core;


// The jobcenter is 

public unsafe static class JobCenter
{
    // The list to hold all
    // threads, that the currently
    // running CPU can manage

    private static Thread[] threads;


    // Type initialiser

    static JobCenter()
    {
        // Initialise the threads array

        threads = new Thread[Environment.ProcessorCount];


    }


    // Blocks, until the given job
    // can be run by another thread

    public static void RunJob(Job job)
    {
        // Try to find a free job

        for(int i = 0; i < threads.Length; i++)
        {
            // Run the job on the current thread,
            // if it isn't running anything

            if(threads[i] == null || ((threads[i].ThreadState & ThreadState.Stopped) == ThreadState.Stopped))
            {                
                threads[i] = new Thread(() => job.Method(job.Overload));

                threads[i].Start();

                return;
            }


            // Make sure, that the loop restarts,
            // if there was no free thread found

            i %= threads.Length;
        }
    }


    // Blocks, until the given job
    // can be run by another thread

    public static bool TryRunJob(Job job)
    {
        // Try to find a free job

        for(int i = 0; i < threads.Length; i++)
        {
            // Run the job on the current thread,
            // if it isn't running anything

            if(threads[i] != null)
                if((threads[i].ThreadState & ThreadState.Stopped) != ThreadState.Stopped)
                    continue;
            

            // Set up the thread and start it
            
            threads[i] = new Thread(() => job.Method(job.Overload));

            threads[i].Start();

            
            // A free thread has been found

            return true;
        }


        // A free thread hasn't been found

        return false;
    }


    // Counts the amount of threads,
    // that are free, but stalls, as
    // long as there isn't at least
    // one free thread

    public static int CountFreeThreads()
    {
        // Count the threads, that are
        // currently free and return the result

        int cnt = 0;

        for(int i = 0; i < threads.Length; i++)
        {
            if(threads[i] == null || (threads[i].ThreadState & ThreadState.Stopped) == ThreadState.Stopped)
                cnt++;
        }

        if(cnt != 0)
            return cnt;


        // If there were no free threads
        // counted, stall until one is free

        for(int i = 0; i < threads.Length; i++, i %= threads.Length)
        {
            // End the loop, if the current
            // iteration is free

            if(threads[i] == null || (threads[i].ThreadState & ThreadState.Stopped) == ThreadState.Stopped)
                break;
        }
        
        return 1;
    }


    // Counts the amount of threads,
    // that are free

    public static int TryCountFreeThreads()
    {
        // Count the threads, that are
        // currently free and return the result

        int cnt = 0;

        for(int i = 0; i < threads.Length; i++)
        {
            if(threads[i] == null || (threads[i].ThreadState & ThreadState.Stopped) == ThreadState.Stopped)
                cnt++; 
        }

        return cnt;
    }
}


// Holds a method and and overload
// to be run within a seperate thread

public unsafe struct Job
{
    // The reference to the method to run

    public delegate*<nuint, void> Method;

    // The reference to overload to the method

    public nuint Overload;
}
