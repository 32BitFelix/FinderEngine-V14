

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

        threads = new Thread[Environment.ProcessorCount - 1];


    }


    // Blocks, until the given job
    // can be run by another thread

    public static void RunJob(Job job)
    {
        // A constant representing the
        // state a thread should have,
        // in order to take up a new job

        const ThreadState desiredState = ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted; 


        // Try to find a free job

        for(int i = 0; i < threads.Length; i++)
        {
            // Run the job on the current thread,
            // if it isn't running anything

            if(threads[i] == null || ((threads[i].ThreadState & desiredState) != ThreadState.Running))
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
        // A constant representing the
        // state a thread should have,
        // in order to take up a new job

        const ThreadState desiredState = ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted; 


        // Try to find a free job

        for(int i = 0; i < threads.Length; i++)
        {
            // Run the job on the current thread,
            // if it isn't running anything

            if(threads[i] != null)
                if((threads[i].ThreadState & desiredState) == ThreadState.Running)
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
