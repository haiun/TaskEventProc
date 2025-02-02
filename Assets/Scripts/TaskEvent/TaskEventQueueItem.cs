using System;
using Cysharp.Threading.Tasks;

namespace TaskEvent
{
    /*
     * TaskEventConsumer에서 수행하고 결과를 기다리는 단위를 정의합니다.
     */
    public class TaskEventQueueItem : IDisposable
    {
        public ICommand Command;
        public UniTaskCompletionSource<ICommandResult> TaskCompletionSource;
    
        /*
         * 작업이 취소되었음을 통지합니다.
         */
        public void Dispose()
        {
            TaskCompletionSource?.TrySetCanceled();
        }
    }
}