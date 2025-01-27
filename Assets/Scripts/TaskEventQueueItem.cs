using System;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TaskEventResult;

public class TaskEventQueueItem : IDisposable
{
    public ICommand Command;
    public UniTaskCompletionSource<ICommandResult> TaskCompletionSource;
    
    public void Dispose()
    {
        TaskCompletionSource?.TrySetCanceled();
    }
}