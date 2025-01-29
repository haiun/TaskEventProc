using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TaskEvent;

public class TaskEventProducer
{
    private readonly int _producerId;
    private readonly CancellationToken _ct;
    private readonly TaskEventConsumer _taskEventConsumer;
    private readonly ITaskEventPresenter _taskEventPresenter;
    
    public int ProducerId => _producerId;

    public TaskEventProducer(int producerId, TaskEventConsumer taskEventConsumer, ITaskEventPresenter taskEventPresenter, CancellationToken ct)
    {
        _producerId = producerId;
        _taskEventConsumer = taskEventConsumer;
        _taskEventPresenter = taskEventPresenter;
        _ct = ct;
    }

    public async UniTask RunSequenceAsync(int delay)
    {
        if (_ct.IsCancellationRequested)
            return;
        
        foreach (int i in Enumerable.Range(1, 100))
        {
            var command = new AddNumber { Number = i };
            var result = await _taskEventConsumer.ProcessEventAsync(command);
            if (_ct.IsCancellationRequested)
                return;
            
            _taskEventPresenter.OnComplete(_producerId, command, result);
            
            if (delay <= 0)
                continue;
            
            await Task.Delay(delay, cancellationToken:_ct);
            if (_ct.IsCancellationRequested)
                return;
        }
        
        foreach (int i in Enumerable.Range(1, 100))
        {
            var command = new AddNumber { Number = -i };
            var result = await _taskEventConsumer.ProcessEventAsync(command);
            if (_ct.IsCancellationRequested)
                return;
            
            _taskEventPresenter.OnComplete(_producerId, command, result);
            
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
            _taskEventPresenter.OnComplete(_producerId, commands[i], results[i]);
        }
    }
}