# TaskEventProcessor
이 프로젝트는 Unity 클라이언트와 .NET 서버 간 실시간 동기화 시스템을 구현하면서, Producer-Consumer 패턴을 설계했던 방법을 설명합니다.<br>
중요한 포인트는 이벤트 풀링을 항상 하지 않으면서도 높은 반응성을 구현하는 방식입니다.<br>
<br>
## 연구 목표
싱글 기반으로 프로토타이핑을 진행하여 게임성을 검증한 후, 실시간 멀티플레이 게임으로 구조를 확장하는 과정에서 몇 가지 문제를 경험했습니다.

1. 이벤트 큐로 이벤트를 수집하는 로직에서 멀티쓰레드를 통해 동시에 이벤트를 수집해야 했습니다.
2. 이벤트 소모 시, 처음에는 일정한 시간 간격으로 큐를 확인하고 실행했으나, 서버에 적용되면서 분산된 각 방의 이벤트 루프마다 시스템 부하가 증가했습니다. 부하를 줄이기 위해 시간 간격을 줄였지만, 그로 인해 게임의 반응성이 떨어지는 문제가 발생했습니다.
   
이 문제들을 해결하기 위해 아래와 같은 방법으로 구조를 개선하였고, 결과적으로 모든 문제를 해결할 수 있었습니다.<br>
<br>

## 작업 요청 및 완료 결과 대기
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
이벤트 실행 결과를 통지하기 위해 UniTaskCompletionSource를 사용하여 이벤트 큐에 저장하고, 각 결과에 대해 비동기적으로 통지합니다.<br>
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

## 테스트 프로그램 시나리오
<img src="https://github.com/haiun/TaskEventProc/blob/main/ReadMeImage/Ex0.png"/><br>
1차 방정식의 치역을 TaskEventProducer에서 생성해서 TaskEventProcessor에 AccumulatedNumber에 덧셈을 누적 연산하는 프로그램을 작성해서 조건을 조작하여 테스트합니다.<br>
1. 1부터 10까지 수를 더함. 총합 55<br>
2. 1을 10번 더함. 총합 10<br>
3. 1부터 20까지 수를 더함. 총합 210<br>

3개의 시나리오를 모두 더해서 누적합이 275 (55+10+210)이 되는지 확인합니다.<br>
<br>
#### 1. Client-Server간 RPC Protocol을 모방<br>

```csharp
private async UniTask RunSequenceAsync(int liner, int constant, int variableMin, int variableMax, int delayMs)
{
   if (variableMin > variableMax)
       return;
   
   if (_ct.IsCancellationRequested)
       return;

   int taskIndex = 0;
   int taskCount = variableMax - variableMin + 1;
   var activeTaskView = _producerView.CreateActiveTaskView();
   foreach (int i in Enumerable.Range(variableMin, taskCount))
   {
       taskIndex++;
       
       activeTaskView.SetTaskProgress(taskIndex, taskCount);
       
       var command = new AddNumber { Number = i * liner + constant };
       var result = await _taskEventConsumer.ProcessEventAsync(command);
       if (_ct.IsCancellationRequested)
           return;
   
       _taskEventPresenter.OnComplete(ProducerId, command, result);
   
       if (delayMs <= 0)
           continue;
   
       await UniTask.Delay(delayMs, cancellationToken:_ct);
       if (_ct.IsCancellationRequested)
           return;
   }
   _producerView.ReleaseActiveTaskView(activeTaskView);
}
```
<br>
<img src="https://github.com/haiun/TaskEventProc/blob/main/ReadMeImage/Ex1.gif"/><br>
<br>
#### 2. 모든 작업을 즉시 처리 요청<br>

```csharp
private async UniTask RunAsync(int liner, int constant, int variableMin, int variableMax)
{
   if (variableMin > variableMax)
       return;
   
   if (_ct.IsCancellationRequested)
       return;

   int taskCount = variableMax - variableMin + 1;
   var activeTaskView = _producerView.CreateActiveTaskView();
   activeTaskView.SetTaskProgress(taskCount, taskCount);
   var commands = Enumerable.Range(variableMin, taskCount)
       .Select(i => new AddNumber { Number = i * liner + constant })
       .ToArray();
   var tasks = commands.Select(command => _taskEventConsumer.ProcessEventAsync(command)).ToArray();
   var results = await UniTask.WhenAll(tasks);

   if (_ct.IsCancellationRequested)
       return;

   foreach (int i in Enumerable.Range(0, variableMax - variableMin))
   {
       _taskEventPresenter.OnComplete(ProducerId, commands[i], results[i]);
   }
   _producerView.ReleaseActiveTaskView(activeTaskView);
}
```
<br>
<img src="https://github.com/haiun/TaskEventProc/blob/main/ReadMeImage/Ex2.gif"/><br>
