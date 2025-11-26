
using System.Runtime.InteropServices;
using Core.MemoryManagement;

namespace IO.Logging;


// The logger keeps track
// of notable events in
// the engine, like warnings
// and errors. All logs will
// be saved, as soon as the
// engine reaches it's end

// TODO: Maybe make string
// concatenation low level,
// there is some room for improvement

public unsafe static class Logger
{
    // Type initialiser

    static Logger()
    {
        logs = CompactArray.Create<Log>(0);

        logsLock = 0;

        HasError = false;
    }


    // The list of all logs
    // made throughout runtime
    // (Compact array)

    private static Log* logs;

    // The lock of the logs array

    private static int logsLock;


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

        nMsg = string.Concat(nMsg, "\n");


        // Create the new log

        Log nLog = new((char*)Marshal.StringToHGlobalUni(nMsg));


        {
            // Retrieve the ID of the current thread

            int currentThreadID = Environment.CurrentManagedThreadId;

            
            // Get the lock of the logs
            // until successful

            repeat:
            
            Interlocked.CompareExchange(ref logsLock, currentThreadID, 0);

            if(logsLock != currentThreadID)
                goto repeat;
        }    


        // Resize the logs array

        fixed(Log** ptr = &logs)
            CompactArray.Resize(ptr, CompactArray.Length(logs) + 1);


        // Save the new log

        logs[CompactArray.Length(logs) - 1] = nLog;


        // Release the lock

        logsLock = 0;
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

        nMsg = string.Concat(nMsg, "\n");


        // Create the new log

        Log nLog = new((char*)Marshal.StringToHGlobalUni(nMsg));


        {
            // Retrieve the ID of the current thread

            int currentThreadID = Environment.CurrentManagedThreadId;

            
            // Get the lock of the logs
            // until successful

            repeat:
            
            Interlocked.CompareExchange(ref logsLock, currentThreadID, 0);

            if(logsLock != currentThreadID)
                goto repeat;
        }    


        // Resize the logs array

        fixed(Log** ptr = &logs)
            CompactArray.Resize(ptr, CompactArray.Length(logs) + 1);


        // Save the new log

        logs[CompactArray.Length(logs) - 1] = nLog;


        // Release the lock

        logsLock = 0;
    }


    // A global field, that shows,
    // if an error has been logged before

    public static bool HasError {get; private set;}

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

        nMsg = string.Concat(nMsg, "\n");


        // Create the new log

        Log nLog = new((char*)Marshal.StringToHGlobalUni(nMsg));


        {
            // Retrieve the ID of the current thread

            int currentThreadID = Environment.CurrentManagedThreadId;

            
            // Get the lock of the logs
            // until successful

            repeat:
            
            Interlocked.CompareExchange(ref logsLock, currentThreadID, 0);

            if(logsLock != currentThreadID)
                goto repeat;
        }    


        // Resize the logs array

        fixed(Log** ptr = &logs)
            CompactArray.Resize(ptr, CompactArray.Length(logs) + 1);


        // Save the new log

        logs[CompactArray.Length(logs) - 1] = nLog;


        // Release the lock

        logsLock = 0;
    }


    // Saves the logs onto a file
    // when called

    public static void SaveLogs()
    {
        // Open a stream to the
        // file to log to

        using(FileStream fs = File.Create("./EngineEvents.Log"))
        {
            // Iterate through each log

            for(int i = 0; i < CompactArray.Length(logs); i++)
            {
                // Iterate through each character
                // in the message of the log

                for(int c = 0; ; c++)
                {
                    if(logs[i].Sentence[c] == '\0')
                        break;

                    byte* b = (byte*)&logs[i].Sentence[c];

                    fs.WriteByte(b[0]);

                    fs.WriteByte(b[1]);
                }
            }


        }
    }


    // Holds a log and
    // it's severity,
    // aswell as the
    // time it was created

    private unsafe struct Log(char* _sentence)
    {
        // The content of the log

        public readonly char* Sentence = _sentence;
    }


    // Represents the severity
    // of a log

    private enum LogSeverity : byte
    {
        // A simple message.
        // Just important enough
        // to get a mention

        Message = 0,

        // Something important
        // has happened or there
        // is an error on the verge

        Warning = 1,

        // Something went wrong.
        // The engine will end at
        // the nearest possibility

        Error = 2
    }
}