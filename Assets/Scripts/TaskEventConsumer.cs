using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TaskEventResult;

public abstract class TaskEventConsumer
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
    
    public void EnqueueEvent(IEventBase eventItem)
    {
        if (IsCancellationRequested)
            return;
        
        _eventQueue.Enqueue(new TaskEventQueueItem { Event = eventItem });
        int overlaps = Interlocked.Increment(ref _eventOverlaps);
        if (overlaps != 1)
            return;
        
        ConsumeQueueAsync().Forget();
    }

    public async UniTask<IEventResult> ProcessEventAsync(IEventBase eventItem)
    {
        if (IsCancellationRequested)
            return ErrorResult.Default;
        
        var eventQueueItem = new TaskEventQueueItem
        {
            Event = eventItem,
            TaskCompletionSource = new UniTaskCompletionSource<IEventResult>()
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
}