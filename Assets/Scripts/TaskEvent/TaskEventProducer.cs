using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TaskEvent
{
    /*
     * 1차 함수의 치역을 더하는 이벤트를 생성 후 실행합니다.
     */
    public class TaskEventProducer
    {
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _ct;
        private readonly TaskEventConsumer _taskEventConsumer;
        private readonly ITaskEventPresenter _taskEventPresenter;
        private readonly ProducerView _producerView;

        public int ProducerId { get; }

        public TaskEventProducer(int producerId, TaskEventConsumer taskEventConsumer, 
            ITaskEventPresenter taskEventPresenter, ProducerView producerView, CancellationToken ct)
        {
            ProducerId = producerId;
            _taskEventConsumer = taskEventConsumer;
            _taskEventPresenter = taskEventPresenter;
            _producerView = producerView;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ct = _cts.Token;
        }

        /*
         * 이벤트의 생성을 중지합니다.
         */
        public void Cancel()
        {
            _cts.Cancel();
        }

        /*
         * UI에 설정된 값으로 이벤트를 실행합니다.
         */
        public void DoClickExecuteButton()
        {
            _producerView.OnClickExecuteButton();
        }
        
        /*
         * 1차 함수의 정의역을 기반으로 이벤트를 요청합니다.
         */
        public async UniTask OnClickExecuteButtonAsync(int a, int b, int minX, int maxX, bool useDelay, int delayMs)
        {
            if (useDelay)
            {
                await RunSequenceAsync(a, b, minX, maxX, delayMs);
            }
            else
            {
                await RunAsync(a,b,minX,maxX);
            }
        }

        /*
         * 1차 함수의 정의역을 기반으로 이벤트를 비동기 순차 생성 후 실행합니다.
         */
        private async UniTask RunSequenceAsync(int liner, int constant, int variableMin, int variableMax, int delayMs)
        {
            if (variableMin > variableMax || _ct.IsCancellationRequested)
                return;

            int taskIndex = 0;
            int taskCount = variableMax - variableMin + 1;
            
            // 작업 진행상황 표시용 UI를 생성합니다.
            var activeTaskView = _producerView.CreateActiveTaskView();
            foreach (int i in Enumerable.Range(variableMin, taskCount))
            {
                taskIndex++;
                
                // 작업 진행상황을 UI에 갱신합니다.
                activeTaskView.SetTaskProgress(taskIndex, taskCount);
                
                // i번째 1차 함수 치역을 더하는 이벤트를 생성합니다.
                var command = new AddNumber { Number = i * liner + constant };
                
                // TaskEventProcessor에 이벤트를 요청합니다.
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
            
            // 작업 진행상황 표시용 UI 제거합니다.
            _producerView.ReleaseActiveTaskView(activeTaskView);
        }
    
        /*
         * 1차 함수의 정의역을 기반으로 이벤트를 생성 후 동시에 비동기 실행합니다.
         */
        private async UniTask RunAsync(int liner, int constant, int variableMin, int variableMax)
        {
            if (variableMin > variableMax || _ct.IsCancellationRequested)
                return;
        
            int taskCount = variableMax - variableMin + 1;
            
            // 작업 진행상황 표시용 UI를 생성합니다.
            var activeTaskView = _producerView.CreateActiveTaskView();
            activeTaskView.SetTaskProgress(taskCount, taskCount);
            
            // 1차 함수 치역을 더하는 이벤트를 모두 생성합니다.
            var commands = Enumerable.Range(variableMin, taskCount)
                .Select(i => new AddNumber { Number = i * liner + constant })
                .ToArray();
            
            // 생성된 이벤트를 모두 요청하고 결과를 대기합니다.
            var tasks = commands.Select(command => _taskEventConsumer.ProcessEventAsync(command)).ToArray();
            var results = await UniTask.WhenAll(tasks);
        
            if (_ct.IsCancellationRequested)
                return;
        
            foreach (int i in Enumerable.Range(0, variableMax - variableMin))
            {
                _taskEventPresenter.OnComplete(ProducerId, commands[i], results[i]);
            }
            
            // 작업 진행상황 표시용 UI를 제거합니다.
            _producerView.ReleaseActiveTaskView(activeTaskView);
        }
    }
}