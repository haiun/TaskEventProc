using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TaskEventResult;
using TMPro;
using UnityEngine;

public class Main : MonoBehaviour, ITaskEventPresenter
{
    private CancellationToken _ct;
    private TaskEventProcessor _eventProcessor;
    private readonly List<TaskEventProducer> _eventProducers = new List<TaskEventProducer>();

    [SerializeField]
    private TextMeshProUGUI _processorValue;

    void Awake()
    {
        _ct = this.GetCancellationTokenOnDestroy();
        _eventProcessor = new TaskEventProcessor(0, this, _ct);
        _eventProducers.AddRange(Enumerable.Range(1, 10).Select(i => new TaskEventProducer(i, _eventProcessor, this, _ct)));
    }
    
    public void RunSequence() => RunSequenceAsync(10).Forget();
    private async UniTask RunSequenceAsync(int delay)
    {
        if (_ct.IsCancellationRequested)
            return;
        
        await UniTask.WhenAll(_eventProducers.Select(producer => producer.RunSequenceAsync(delay)));
    }

    void OnDestroy()
    {
        _eventProcessor.Dispose();
    }

    public void OnProcessed(ICommand command, ICommandResult result)
    {
        if (result is AddNumberResult addNumberResult)
        {
            //Debug.Log($"OnProcessed {addNumberResult.GetType()} {addNumberResult.BeforeNumber} -> {addNumberResult.AfterNumber}");
            return;
        }
        //Debug.Log($"OnProcessed {result.GetType()}");
    }

    public void OnComplete(int producerId, ICommand command, ICommandResult result)
    {
        if (result is AddNumberResult addNumberResult)
        {
            _processorValue.text = addNumberResult.AfterNumber.ToString();
            //Debug.Log($"OnComplete {producerId} {addNumberResult.GetType()} {addNumberResult.BeforeNumber} -> {addNumberResult.AfterNumber}");
            return;
        }
        //Debug.Log($"OnComplete {producerId} {result.GetType()}");
    }
}