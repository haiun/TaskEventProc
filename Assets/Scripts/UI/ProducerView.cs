using System.Threading;
using Cysharp.Threading.Tasks;
using TaskEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProducerViewInit
{
    public TaskEventProducer EventProducer;
    public int A;
    public int B;
    public int MinX;
    public int MaxX;
}

public class ProducerView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _producerTitle;
    [SerializeField]
    private TMP_InputField _paramA;
    [SerializeField]
    private TMP_InputField _paramB;
    [SerializeField]
    private TMP_InputField _paramMinX;
    [SerializeField]
    private TMP_InputField _paramMaxX;
    [SerializeField]
    private Toggle _toggleDelay;
    [SerializeField]
    private TextMeshProUGUI _delayText;

    [SerializeField]
    private GameObject _activeTaskViewPrefab;

    [SerializeField]
    private int _taskDelayMs = 10;

    private ProducerViewInit _initData;
    private CancellationToken _ct;
    
    void Awake()
    {
        _ct = gameObject.GetCancellationTokenOnDestroy();
        _delayText.text = $"Use Delay ({_taskDelayMs}ms)";
    }
    
    public void Initialize(ProducerViewInit initData)
    {
        _initData = initData;
        _paramA.text = initData.A.ToString();
        _paramB.text = initData.B.ToString();
        _paramMinX.text = initData.MinX.ToString();
        _paramMaxX.text = initData.MaxX.ToString();
        UpdateViewTitleText();
    }

    public void UpdateViewTitleText()
    {
        _producerTitle.text = $"ProducerId : {_initData.EventProducer.ProducerId}\nA * X + B\n{_paramA.text} * ({_paramMinX.text}~{_paramMaxX.text}) + {_paramB.text}";
    }

    public ActiveTaskView CreateActiveTaskView()
    {
        var view = Instantiate(_activeTaskViewPrefab, transform);
        return view.GetComponent<ActiveTaskView>();
    }

    public void ReleaseActiveTaskView(ActiveTaskView activeTaskView)
    {
        Destroy(activeTaskView.gameObject);
    }
    
    public void OnClickExecuteButton()
    {
        int a = int.Parse(_paramA.text);
        int b = int.Parse(_paramB.text);
        int minX = int.Parse(_paramMinX.text);
        int maxX = int.Parse(_paramMaxX.text);
        bool toggledDelay = _toggleDelay.isOn;
        
        _initData.EventProducer.OnClickExecuteButtonAsync(a,b,minX, maxX, toggledDelay, _taskDelayMs).Forget();
    }

    public void OnClickCloseButton()
    {
        _initData.EventProducer.Cancel();
        Destroy(gameObject);
    }
}
