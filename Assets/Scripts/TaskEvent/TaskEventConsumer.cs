using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TaskEvent
{
    /*
     * 동시다발적으로 받은 이벤트를 순차적으로 실행하고, 각 실행결과를 반환합니다.
     */
    public abstract class TaskEventConsumer : IDisposable
    {
        private readonly CancellationToken _ct;
        private readonly ConcurrentQueue<TaskEventQueueItem> _eventQueue = new ConcurrentQueue<TaskEventQueueItem>();
        public bool IsCancellationRequested => _ct.IsCancellationRequested;
        private int _eventOverlaps;

        protected TaskEventConsumer(CancellationToken ct)
        {
            _ct = ct;
        }
    
        protected abstract void ProcessEventQueueItemImpl(TaskEventQueueItem queueItem);
    
        public void EnqueueEvent(ICommand commandItem)
        {
            if (IsCancellationRequested)
                return;
        
            _eventQueue.Enqueue(new TaskEventQueueItem { Command = commandItem });
            int overlaps = Interlocked.Increment(ref _eventOverlaps);
            if (overlaps != 1)
                return;
        
            ConsumeQueueAsync().Forget();
        }

        public async UniTask<ICommandResult> ProcessEventAsync(ICommand commandItem)
        {
            if (IsCancellationRequested)
                return ErrorResult.Default;
        
            var eventQueueItem = new TaskEventQueueItem
            {
                Command = commandItem,
                TaskCompletionSource = new UniTaskCompletionSource<ICommandResult>()
            };
            _eventQueue.Enqueue(eventQueueItem);
            int overlaps = Interlocked.Increment(ref _eventOverlaps);
            if (overlaps == 1)
            {
                ConsumeQueueAsync().Forget();
            }
        
            return await eventQueueItem.TaskCompletionSource.Task;
        }

        private async UniTask ConsumeQueueAsync()
        {
            await UniTask.Yield();

            if (IsCancellationRequested)
                return;

            while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
            {
                ProcessEventQueueItemImpl(queueItem);
           
                int overlaps = Interlocked.Decrement(ref _eventOverlaps);
                if (overlaps == 0)
                    return;

                if (IsCancellationRequested)
                    return; 
            }
        }

        public void Dispose()
        {
            while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
            {
                queueItem.Dispose();
            }
        }
    }
}