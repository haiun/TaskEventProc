using Cysharp.Threading.Tasks;
using TaskEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private TaskEventProducer _eventProducer;

    public void Initialize(TaskEventProducer eventProducer, int a, int b, int minX, int maxX)
    {
        _eventProducer = eventProducer;
        _paramA.text = a.ToString();
        _paramB.text = b.ToString();
        _paramMinX.text = minX.ToString();
        _paramMaxX.text = maxX.ToString();
    }
    
    public void OnClickExecuteButton()
    {
        int a = int.Parse(_paramA.text);
        int b = int.Parse(_paramB.text);
        int minX = int.Parse(_paramMinX.text);
        int maxX = int.Parse(_paramMaxX.text);
        
        if (_toggleDelay.isOn)
        {
            _eventProducer.RunSequenceAsync(a,b,minX,maxX, 10).Forget();
        }
        else
        {
            _eventProducer.RunAsync(a,b,minX,maxX).Forget();
        }
    }
}
