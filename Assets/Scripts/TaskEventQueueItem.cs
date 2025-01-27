using System;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TaskEventResult;

public class TaskEventQueueItem : IDisposable
{
    public IEventBase Event;
    public UniTaskCompletionSource<IEventResult> TaskCompletionSource;
    
    public void Dispose()
    {
        TaskCompletionSource?.TrySetCanceled();
    }
}