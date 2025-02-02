# TaskEventProcessor
이 프로젝트는 Unity 클라이언트와 .NET 서버를 이용하여 실시간 게임의 로직 동기화 시스템을 구현하는 방법에 대해 설명하고 있습니다.
중요한 포인트는 메시지 풀링을 하지 않으면서 높은 반응성을 구현하는 방식에 초점을 맞추고 있다는 점입니다.

```csharp
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
```

```csharp
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
```

```csharp
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
```
