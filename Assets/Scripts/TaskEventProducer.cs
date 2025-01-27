using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;

public class TaskEventProducer
{
    private readonly int _producerId;
    private readonly CancellationToken _ct;
    private readonly TaskEventConsumer _taskEventConsumer;
    private readonly ITaskEventPresenter _taskEventPresenter;

    public TaskEventProducer(int producerId, TaskEventConsumer taskEventConsumer, ITaskEventPresenter taskEventPresenter, CancellationToken ct)
    {
        _producerId = producerId;
        _taskEventConsumer = taskEventConsumer;
        _taskEventPresenter = taskEventPresenter;
        _ct = ct;
    }

    public async UniTask RunSequenceAsync()
    {
        foreach (int i in Enumerable.Range(1, 100))
        {
            var command = new AddNumber { Number = i };
            var result = await _taskEventConsumer.ProcessEventAsync(command);
            _taskEventPresenter.OnComplete(_producerId, command, result);
        }
    }
    
    public async UniTask RunAsync()
    {
        var commands = Enumerable.Range(1, 100).Select(i => new AddNumber { Number = i }).ToArray();
        var tasks = commands.Select(command => _taskEventConsumer.ProcessEventAsync(command)).ToArray();
        var results = await UniTask.WhenAll(tasks);
        foreach (int i in Enumerable.Range(0, 100))
        {
            _taskEventPresenter.OnComplete(_producerId, commands[i], results[i]);
        }
    }
}