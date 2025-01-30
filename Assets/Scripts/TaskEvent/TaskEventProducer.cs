using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace TaskEvent
{
    /*
     * 1차함수의 결과값를 일정 시간간격으로 더하도록 요청합니다.
     */
    public class TaskEventProducer
    {
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _ct;
        private readonly TaskEventConsumer _taskEventConsumer;
        private readonly ITaskEventPresenter _taskEventPresenter;
    
        public int ProducerId { get; }
        public int Coefficient { get; set; } = 1;
        public int Constant { get; set; } = 0;
        public int VariableMin { get; set; } = 1;
        public int VariableMax { get; set; } = 10;

        public TaskEventProducer(int producerId, TaskEventConsumer taskEventConsumer, ITaskEventPresenter taskEventPresenter, CancellationToken ct)
        {
            ProducerId = producerId;
            _taskEventConsumer = taskEventConsumer;
            _taskEventPresenter = taskEventPresenter;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _ct = _cts.Token;
        }

        public async UniTask RunSequenceAsync(int liner, int constant, int delay)
        {
            if (_ct.IsCancellationRequested)
                return;
        
            foreach (int i in Enumerable.Range(1, 10))
            {
                var command = new AddNumber { Number = i * liner + constant };
                var result = await _taskEventConsumer.ProcessEventAsync(command);
                if (_ct.IsCancellationRequested)
                    return;
            
                _taskEventPresenter.OnComplete(ProducerId, command, result);
            
                if (delay <= 0)
                    continue;
            
                await Task.Delay(delay, cancellationToken:_ct);
                if (_ct.IsCancellationRequested)
                    return;
            }
        }
    
        public async UniTask RunAsync()
        {
            if (_ct.IsCancellationRequested)
                return;
        
            var commands = Enumerable.Range(1, 100).Select(i => new AddNumber { Number = i }).ToArray();
            var tasks = commands.Select(command => _taskEventConsumer.ProcessEventAsync(command)).ToArray();
            var results = await UniTask.WhenAll(tasks);
        
            if (_ct.IsCancellationRequested)
                return;
        
            foreach (int i in Enumerable.Range(0, 100))
            {
                _taskEventPresenter.OnComplete(ProducerId, commands[i], results[i]);
            }
        }
    }
}