


namespace Core.LECS;


public unsafe static class JobCenter
{

}


public unsafe struct Job
{
    public JobState State;

    public nint* Task;
}


public enum JobState : byte
{
    IsSequential = 1,


}