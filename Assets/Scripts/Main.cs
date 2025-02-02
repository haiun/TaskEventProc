using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TMPro;
using UnityEngine;

public class Main : MonoBehaviour, ITaskEventPresenter
{
    private CancellationToken _ct;
    private TaskEventProcessor _eventProcessor;
    private readonly List<TaskEventProducer> _taskEventProducers = new List<TaskEventProducer>();
    private int _nextTaskProducerId = 0;

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
        
        AddTaskEventProducer(1, 0, 1, 10);
        AddTaskEventProducer(0, 1, 1, 10);
        AddTaskEventProducer(1, 0, -10, 10);
    }

    void OnDestroy()
    {
        _eventProcessor?.Dispose();
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
        _eventProcessor.ProcessEventAsync(new SetNumber { Number = 0 }).Forget();
    }

    private void AddTaskEventProducer(int a, int b, int minX, int maxX)
    {
        var gameObj = Instantiate(_producerViewPrefab.gameObject, _producerViewParent);
        var producerView = gameObj.GetComponent<ProducerView>();
        if (producerView == null)
            return;
        
        int producerId = _nextTaskProducerId;
        _nextTaskProducerId++;
        var taskEventProducer = new TaskEventProducer(producerId, _eventProcessor, this, producerView,_ct);
        _taskEventProducers.Add(taskEventProducer);
        
        producerView.Initialize(new ProducerViewInit
        {
            EventProducer = taskEventProducer,
            A = a,
            B = b,
            MinX = minX,
            MaxX = maxX
        });
    }

    public void OnClickAddFunctionButton()
    {
        AddTaskEventProducer(1, 0, 1, 20);
    }

    public void OnClickExecuteAllButton()
    {
        foreach (var taskEventProducer in _taskEventProducers)
        {
            taskEventProducer.DoClickExecuteButton();
        }
    }
}