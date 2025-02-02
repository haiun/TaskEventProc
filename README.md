# TaskEventProcessor
이 프로젝트는 Unity 클라이언트와 .NET 서버를 이용하여 실시간 게임의 로직 동기화 시스템을 구현했던 방법에 대해 설명하고 있습니다.<br>
중요한 포인트는 이벤트 풀링을 항상 하지 않으면서 높은 반응성을 구현하는 방식인 점입니다.<br>

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
이벤트 실행 결과를 통지하기위한 UniTaskCompletionSource를 짝지어 이벤트큐에 저장해서 각 결과에대한 비동기 통지를 합니다.<br>

```csharp
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
```
이벤트큐에 이벤트를 추가 한뒤 Interlocked.Increment함수를 통해 대기중인 이벤트 수를 _eventOverlaps에 갱신합니다.<br>
이벤트큐의 Count메서드로 계산하지 않는 이유는 여러 쓰레드에서 병렬로 실행 시 오차가 생길 수 있습니다.<br>
-그림삽입-<br>


```csharp
private async UniTask ConsumeQueueAsync()
{
    // 유니티 함수를 호출할 예정이므로, 유니티루프의 업데이트시점까지 대기
    await UniTask.Yield();

    if (_ct.IsCancellationRequested)
        return;

    while (_eventQueue.TryDequeue(out TaskEventQueueItem queueItem))
    {
        // 이벤트 실행
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
```
이벤트큐를 순차적으로 이벤트를 소모하고 Interlocked.Decrement함수를 통해 대기중인 이벤트 수를 _eventOverlaps에 갱신합니다.<br>
_eventOverlaps가 0이 되어 ConsumeQueueAsync함수가 종료될 때 이벤트큐에 요소가 있는 경우가 있지만,
요소를 추가한 EnqueueItemAndTryConsumeQueue함수에서 바로 ConsumeQueueAsync함수가 실행됩니다.<br>

