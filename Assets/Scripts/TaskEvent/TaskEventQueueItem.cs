using System;
using Cysharp.Threading.Tasks;

namespace TaskEvent
{
    public class TaskEventQueueItem : IDisposable
    {
        public ICommand Command;
        public UniTaskCompletionSource<ICommandResult> TaskCompletionSource;
    
        public void Dispose()
        {
            TaskCompletionSource?.TrySetCanceled();
        }
    }
}