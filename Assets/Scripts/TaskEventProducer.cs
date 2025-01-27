using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class TaskEventProducer
{
    private int _producerId;
    
    public TaskEventProducer(int producerId)
    {
        _producerId = producerId;
    }

    public async UniTask Producer()
    {
        await UniTask.CompletedTask;
    }
}