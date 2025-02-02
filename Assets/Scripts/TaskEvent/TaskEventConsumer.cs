using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TaskEvent
{
    /*
     * 입력된 이벤트를 비동기 순차 실행하고 각각의 결과를 반환합니다.
     */
    public abstract class TaskEventConsumer : IDisposable
    {
        private readonly CancellationToken _ct;
        private readonly ConcurrentQueue<TaskEventQueueItem> _eventQueue = new();
        private int _eventOverlaps;

        protected TaskEventConsumer(CancellationToken ct)
        {
            _ct = ct;
        }
    
        protected abstract void ProcessEventQueueItemImpl(TaskEventQueueItem queueItem);
        
        /*
         * 이벤트를 비동기 실행 후 결과를 반환합니다.
         */
        public async UniTask<ICommandResult> ProcessEventAsync(ICommand commandItem)
        {
            if (_ct.IsCancellationRequested)
                return ErrorResult.Default;
        
            var eventQueueItem = new TaskEventQueueItem
            {
                Command = commandItem,
                TaskCompletionSource = new UniTaskCompletionSource<ICommandResult>()
            };

            EnqueueItemAndTryConsumeQueue(eventQueueItem);
        
            return await eventQueueItem.TaskCompletionSource.Task;
        }

        /*
         * 이벤트를 큐에 등록하고, 이벤트를 소비하는 풀링로직을 시작합니다.
         */
        private void EnqueueItemAndTryConsumeQueue(TaskEventQueueItem eventQueueItem)
        {
            _eventQueue.Enqueue(eventQueueItem);
            
            // 대기중인 이벤트의 갯수를 증가합니다.
            int overlaps = Interlocked.Increment(ref _eventOverlaps);
            
            if (overlaps != 1)
                return;

            // 0->1이 되는 순간 이벤트 풀링 시작
            ConsumeQueueAsync().Forget();
        }

        /*
         * 이벤트큐를 순차적으로 실행합니다.
         */
        private async UniTask ConsumeQueueAsync()
        {
            // 유니티 함수를 호출할 예정이므로, 업데이트 주기까지 대기
            await UniTask.Yield();

            if (_ct.IsCancellationRequested)
                return;

            while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
            {
                // 컨텐츠 작업 실행
                ProcessEventQueueItemImpl(queueItem);
           
                // 대기중인 이벤트갯수가 1->0일 경우 함수 종료.
                // _eventQueue에 이벤트가 추가될 때 다시 풀링 시작됩니다.
                int overlaps = Interlocked.Decrement(ref _eventOverlaps);
                if (overlaps == 0)
                    return;

                if (_ct.IsCancellationRequested)
                    return; 
            }
        }

        /*
         * 종료 시 실행되지 못한 이벤트를 모두 취소합니다.
         */
        public void Dispose()
        {
            while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
            {
                queueItem.Dispose();
            }
        }
    }
}