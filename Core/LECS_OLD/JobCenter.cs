


using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace Core.LECS;


// The jobcenter is a helper class
// for distributing work across all
// threads. Also adds support for
// simply segregating work

public unsafe static class JobCenter
{
    static JobCenter()
    {
        // Initialize a list of threads

        threads = new Thread[Environment.ProcessorCount];



    }


    // The collection of all availlable threads

    private static Thread[] threads;


    // A method, that blocks,
    // until all tasks in the given
    // job have been occupied by a thread

    public static void RunJob(Job job)
    {
        // Iterate through each task

        for(int i = 0; i < CompactArray.Length(job.Tasks); i++)
            // Iterate through each thread,
            // until a free one has been found
            // given a task
            
            for(int j = 0; ; j++, j %= threads.Length)
            {
                // Skip to the next iteration,
                // if the current thread isn't stopped

                if(threads[j].ThreadState != ThreadState.Stopped)                
                    continue;


                // Get the task as a threadstart
                // and give it to the current iteration
                // as a method to execute

                ThreadStart start = Marshal.GetDelegateForFunctionPointer<ThreadStart>(job.Tasks[i]);

                threads[j] = new Thread(start);

                threads[j].Start();


                // Get to the next task

                break;
            }
    }


    // Runs the given task
    // and supplies it with
    // the given overload

    public static void RunTask<T>(nint task, T overload)
    {
        // Go through each thread

        for(int i = 0; ; i++, i %= threads.Length)
        {
            // Skip to the next iteration,
            // if the current thread
            // hasn't stopped

            if(threads[i].ThreadState != ThreadState.Stopped)
                continue;


            // Get the task as a threadstart
            // and give it to the current iteration
            // as a method to execute

            ThreadStart start = Marshal.GetDelegateForFunctionPointer<ThreadStart>(task);

            threads[i] = new Thread(start);

            threads[i].Start(overload);


            // prematurely end the method

            return;
        }
    }
}


// A job is a collection
// of tasks, which might
// be run differently,
// based on the state
// of the job

public unsafe struct Job
{
    // Behaviour redefining
    // glags

    public JobState State;

    // The individual method pointers
    // that represent the job.
    // (Compact array)

    public nint* Tasks;
}


// Defines unique behvaiour
// a Job can assume

[Flags]
public enum JobState : byte
{
    // All tasks in the
    // job will be run on
    // the same thread

    IsSequential = 1,


}