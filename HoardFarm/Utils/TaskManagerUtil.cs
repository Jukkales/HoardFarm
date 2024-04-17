using System;
using HoardFarm.Tasks;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Utils;

public static class TaskManagerUtil
{
    
    public static void Enqueue(IBaseTaskGroup taskGroup)
    {
        foreach (var o in taskGroup.GetTaskList())
        {
            switch (o)
            {
                case BaseTask task:
                    Enqueue(task);
                    continue;
                case Func<bool?> func:
                    Enqueue(func);
                    continue;
                default:
                    throw new Exception("Invalid task type");
            }
        }

        
    }
    
    public static void Enqueue(BaseTask task, int timeLimitMs = 10000, string? name = null)
    {
        TaskManager.Enqueue(task.Run, timeLimitMs, name);
    }
    
    public static void Enqueue(BaseTask task) => Enqueue(task, 10000);
    public static void Enqueue(BaseTask task, string? name = null) => Enqueue(task, 10000, name);
    
    public static void Enqueue(Func<bool?> task, int timeLimitMs = 10000, string? name = null)
    {
        TaskManager.Enqueue(task, timeLimitMs, name);
    }
    
    public static void Enqueue(Func<bool?> task) => Enqueue(task, 10000);
    public static void Enqueue(Func<bool?> task, string? name = null) => Enqueue(task, 10000, name);

    public static void EnqueueWait(int delayMS)
    {
        TaskManager.DelayNext(delayMS);
    }
    
    public static void EnqueueImmediate(BaseTask task, int timeLimitMs = 10000, string? name = null)
    {
        TaskManager.EnqueueImmediate(task.Run, timeLimitMs, name);
    }
    
    public static void EnqueueImmediate(BaseTask task, string? name = null) => Enqueue(task, 10000, name);
    public static void EnqueueImmediate(BaseTask task) => Enqueue(task, 10000);
    
    public static void EnqueueImmediate(Func<bool?> task, int timeLimitMs = 10000, string? name = null)
    {
        TaskManager.EnqueueImmediate(task, timeLimitMs, name);
    }
    
    public static void EnqueueImmediate(Func<bool?> task, string? name = null) => Enqueue(task, 10000, name);
    public static void EnqueueImmediate(Func<bool?> task) => Enqueue(task, 10000);
    
    public static void EnqueueWaitImmediate(int delayMS)
    {
        TaskManager.DelayNextImmediate(delayMS);
    }
}
