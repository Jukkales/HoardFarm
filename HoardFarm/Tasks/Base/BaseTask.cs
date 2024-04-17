using System.Diagnostics.CodeAnalysis;

namespace HoardFarm.Tasks.Base;

public abstract class BaseTask
{
    public int Timeout { get; private set; }

    protected BaseTask(int timeout) 
    {
        Timeout = timeout * 1000;
    }
    
    protected BaseTask()
    {
        Timeout = 10 * 1000;
    }

    public abstract bool? Run();
}
