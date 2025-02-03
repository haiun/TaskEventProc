# TaskEventProcessor
이 프로젝트는 Unity 클라이언트와 .NET 서버 간 실시간 동기화 시스템을 구현하면서, Producer-Consumer 패턴을 고려한 방법을 설명합니다.<br>
중요한 포인트는 이벤트 풀링을 항상 하지 않으면서도 높은 반응성을 구현하는 방식입니다.<br>
<br>

## 작업 요청 및 완료 결과 대기
이벤트 실행 결과를 통지하기 위해 UniTaskCompletionSource를 사용하여 이벤트 큐에 저장하고, 각 결과에 대해 비동기적으로 통지합니다.<br>
```csharp
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
```
<br>

## 동시성을 고려한 이벤트 큐
```csharp
private void EnqueueItemAndTryConsumeQueue(TaskEventQueueItem eventQueueItem)
{
    _eventQueue.Enqueue(eventQueueItem);
    
    // 대기중인 이벤트의 수를 증가합니다.
    int overlaps = Interlocked.Increment(ref _eventOverlaps);
    if (overlaps != 1)
        return;

    // 0 -> 1이 되는 순간, 이벤트 풀링을 시작합니다.
    ConsumeQueueAsync().Forget();
}
```
EnqueueItemAndTryConsumeQueue 함수에서는 이벤트 큐에 아이템을 추가하고, Interlocked.Increment를 사용하여 대기 중인 이벤트 수인 _eventOverlaps를 갱신합니다.<br>
이때, 이벤트 큐의 Count를 사용하지 않는 이유는 여러 쓰레드에서 병렬로 실행 시 이벤트 개수에 오차가 발생할 수 있기 때문입니다.<br>
<br>
### 이벤트 개수에 오차가 생기는 경우<br>
<img src="https://raw.githubusercontent.com/haiun/TaskEventProc/refs/heads/main/ReadMeImage/queue_insert_count.png"/><br>
이벤트가 추가되고, 그 개수를 확인하는 사이에 컨텍스트 스위치가 발생하면, 이벤트가 소모되지 않는 문제가 발생할 수 있습니다.<br>
이 문제는 Interlocked.Increment와 Interlocked.Decrement로 해결할 수 있습니다.<br>
<br>

## 순차적인 이벤트 소모
```csharp
private async UniTask ConsumeQueueAsync()
{
    // Unity 함수를 호출할 예정이므로, Unity 루프의 업데이트 시점까지 대기합니다.
    await UniTask.Yield();

    if (_ct.IsCancellationRequested)
        return;

    while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
    {
        // 이벤트 실행
        ProcessEventQueueItemImpl(queueItem);
   
        // 대기 중인 이벤트 수가 1 -> 0이 될 경우 함수 종료
        int overlaps = Interlocked.Decrement(ref _eventOverlaps);
        if (overlaps == 0)
            return;

        if (_ct.IsCancellationRequested)
            return; 
    }
}
```
이벤트 큐에서 이벤트를 순차적으로 처리하고, Interlocked.Decrement로 대기 중인 이벤트 수를 갱신합니다.<br>
_eventOverlaps가 0이 되면 ConsumeQueueAsync 함수가 종료되며, 이벤트 큐에 새 아이템이 추가되면 EnqueueItemAndTryConsumeQueue에서 다시 풀링을 시작합니다.<br>
