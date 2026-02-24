
using System.Runtime.InteropServices;
using Core;

namespace IO.Logging;


// The logger keeps track
// of notable events in
// the engine, like warnings
// and errors

// TODO: Maybe make string
// concatenation low level,
// there is some room for improvement

public unsafe static class Logger
{
    // Type initialiser

    static Logger()
    {
        // Dispose of the previous log file

        File.Delete(TargetFile);
    }


    // The path to write the
    // logs to

    private const string TargetFile = "./EngineEvents.log";


    // The lock of the logs array

    private static int logsLock = 0;


    // Saves the given message into logs

    public static void LogMessage(string message)
    {
        // Construct the string
        // of the message
        
        string nMsg = string.Concat("[MESSAGE] [", DateTime.Now.ToShortDateString());

        nMsg = string.Concat(nMsg, ", ");

        nMsg = string.Concat(nMsg, DateTime.Now.ToShortTimeString());

        nMsg = string.Concat(nMsg, "]: ");

        nMsg = string.Concat(nMsg, message);

        nMsg = string.Concat(nMsg, Environment.NewLine);


        // Create the new log

        Job job = new()
        {
            Method = (delegate*<nuint, void>)&writeLog,

            Overload = (nuint)Marshal.StringToHGlobalUni(nMsg)
        };


        // Run the job

        JobCenter.RunJob(job);
    }


    // Saves the given message as a warning

    public static void LogWarning(string message)
    {
        // Construct the string
        // of the message
        
        string nMsg = string.Concat("[WARNING] [", DateTime.Now.ToShortDateString());

        nMsg = string.Concat(nMsg, ", ");

        nMsg = string.Concat(nMsg, DateTime.Now.ToShortTimeString());

        nMsg = string.Concat(nMsg, "]: ");

        nMsg = string.Concat(nMsg, message);

        nMsg = string.Concat(nMsg, Environment.NewLine);


        // Create the new log

        Job job = new()
        {
            Method = (delegate*<nuint, void>)&writeLog,

            Overload = (nuint)Marshal.StringToHGlobalUni(nMsg)
        };


        // Run the job

        JobCenter.RunJob(job);
    }


    // A global field, that shows,
    // if an error has been logged before

    public static bool HasError {get; private set;} = false;

    // Saves the given message as an error
    // and sets the haserror flag to true

    public static void LogError(string message)
    {
        // Set the haserror flag true.
        // I don't care about race conditions,
        // because all writes will set to
        // true anyway

        HasError = true;


        // Construct the string
        // of the message
        
        string nMsg = string.Concat("[ERROR] [", DateTime.Now.ToShortDateString());

        nMsg = string.Concat(nMsg, ", ");

        nMsg = string.Concat(nMsg, DateTime.Now.ToShortTimeString());

        nMsg = string.Concat(nMsg, "]: ");

        nMsg = string.Concat(nMsg, message);

        nMsg = string.Concat(nMsg, Environment.NewLine);


        // Create the new log

        Job job = new()
        {
            Method = (delegate*<nuint, void>)&writeLog,

            Overload = (nuint)Marshal.StringToHGlobalUni(nMsg)
        };


        // Run the job

        JobCenter.RunJob(job);
    }


    // Method that is run asynchronously
    // for the sake of saving a log to the
    // log file

    private static void writeLog(nuint Sentence)
    {
        // Get the lock of the log file

        int mtID = Environment.CurrentManagedThreadId;

        while(logsLock != mtID)
            Interlocked.CompareExchange(ref logsLock, mtID, 0);


        using(FileStream fs = File.Open(TargetFile, FileMode.Append))
        {
            for(int c = 0; ; c++)
            {
                if(((char*)Sentence)[c] == '\0')
                    break;

                byte* b = (byte*)&((char*)Sentence)[c];

                fs.WriteByte(b[0]);

                fs.WriteByte(b[1]);
            }
        }


        // Release the lock to the file

        logsLock ^= logsLock;


        // Release the unmanaged resources

        Marshal.FreeHGlobal((nint)Sentence);
    }
}