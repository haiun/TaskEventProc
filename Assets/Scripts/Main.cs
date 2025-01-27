using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TaskEventResult;
using UnityEngine;

public class Main : MonoBehaviour, ITaskEventPresenter
{
    private CancellationToken _ct;
    private TaskEventProcessor _eventProcessor;
    private TaskEventProducer[] _eventProducers;
    
    void Awake()
    {
        _ct = this.GetCancellationTokenOnDestroy();
        _eventProcessor = new TaskEventProcessor(0, this, _ct);

        _eventProducers = Enumerable.Range(1, 10).Select(i => new TaskEventProducer(i, _eventProcessor, this, _ct)).ToArray();
        foreach (var producer in _eventProducers)
        {
            producer.RunSequenceAsync().Forget();
        }
    }

    void OnDestroy()
    {
        _eventProcessor.Dispose();
    }

    public void OnProcessed(ICommand command, ICommandResult result)
    {
        if (result is AddNumberResult addNumberResult)
        {
            Debug.Log($"OnProcessed {addNumberResult.GetType()} {addNumberResult.BeforeNumber} -> {addNumberResult.AfterNumber}");
            return;
        }
        Debug.Log($"OnProcessed {result.GetType()}");
    }

    public void OnComplete(int producerId, ICommand command, ICommandResult result)
    {
        if (result is AddNumberResult addNumberResult)
        {
            Debug.Log($"OnComplete {producerId} {addNumberResult.GetType()} {addNumberResult.BeforeNumber} -> {addNumberResult.AfterNumber}");
            return;
        }
        Debug.Log($"OnComplete {producerId} {result.GetType()}");
    }
}