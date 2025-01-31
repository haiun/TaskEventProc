using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TMPro;
using UnityEngine;

public class Main : MonoBehaviour, ITaskEventPresenter
{
    private CancellationToken _ct;
    private TaskEventProcessor _eventProcessor;
    private readonly List<TaskEventProducer> _eventProducers = new List<TaskEventProducer>();
    private readonly List<ProducerView> _producerViews = new List<ProducerView>();

    [SerializeField]
    private ProducerView _producerViewPrefab;
    
    [SerializeField]
    private TextMeshProUGUI _processorValue;

    [SerializeField]
    private Transform _producerViewParent;

    void Awake()
    {
        _ct = this.GetCancellationTokenOnDestroy();
        _eventProcessor = new TaskEventProcessor(0, this, _ct);
        _eventProducers.AddRange(Enumerable.Range(1, 10).Select(i => new TaskEventProducer(i, _eventProcessor, this, _ct)));
    }

    void OnDestroy()
    {
        _eventProcessor.Dispose();
    }

    public void OnProcessed(ICommand command, ICommandResult result)
    {
        switch (result)
        {
            case AddNumberResult addNumberResult:
                _processorValue.text = addNumberResult.AfterNumber.ToString();
                break;
            
            case SetNumberResult setNumberResult:
                _processorValue.text = setNumberResult.AfterNumber.ToString();
                break;
        }
    }

    public void OnComplete(int producerId, ICommand command, ICommandResult result)
    {
        switch (result)
        {
            case AddNumberResult addNumberResult:
                Debug.Log($"OnComplete {producerId} {addNumberResult.GetType()} {addNumberResult.BeforeNumber} -> {addNumberResult.AfterNumber}");
                break;
            
            case SetNumberResult setNumberResult:
                break;
        }
    }

    public void OnClickSetZeroButton()
    {
        _eventProcessor.EnqueueEvent(new SetNumber { Number = 0 });
    }

    public void OnClickAddFunctionButton()
    {
        int producerId = _eventProducers.Count;
        var taskEventProducer = new TaskEventProducer(producerId, _eventProcessor, this, _ct);
        _eventProducers.Add(taskEventProducer);
        
        var gameObj = Instantiate(_producerViewPrefab.gameObject, _producerViewParent);
        var producerView = gameObj.GetComponent<ProducerView>();
        if (producerView == null)
            return;
        
        producerView.Initialize(taskEventProducer, 1, 0, 1, 10);
        _producerViews.Add(producerView);
    }

    public void OnClickExecuteAllButton()
    {
        foreach (var producerView in _producerViews)
        {
            producerView.OnClickExecuteButton();
        }
    }
}