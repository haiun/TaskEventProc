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
            
            TryConsumeQueue();
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

            TryConsumeQueue();
        
            return await eventQueueItem.TaskCompletionSource.Task;
        }

        private void TryConsumeQueue()
        {
            // 함수의 중첩호출에 대한 횟수를 측정
            int overlaps = Interlocked.Increment(ref _eventOverlaps);
            
            // 0->1이 되는 순간 메세지 풀링 시작
            if (overlaps == 1)
            {
                ConsumeQueueAsync().Forget();
            }
        }

        private async UniTask ConsumeQueueAsync()
        {
            // 유니티 함수를 호출할 예정이므로, 업데이트 주기까지 대기
            await UniTask.Yield();

            if (IsCancellationRequested)
                return;

            while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
            {
                // 컨텐츠 작업 실행
                ProcessEventQueueItemImpl(queueItem);
           
                // 중첩호출횟수 감소. 1->0일 경우 함수 종료.
                // _eventQueue 선조작 후 _eventOverlaps를 조작하기 때문에
                // _eventQueue에 값이 추가될 때 다시 풀링시 시작
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