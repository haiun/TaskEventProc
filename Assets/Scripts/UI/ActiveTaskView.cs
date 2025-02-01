using TMPro;
using UnityEngine;

public class ActiveTaskView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _progressText;

    public void SetTaskProgress(int taskIndex, int taskCount)
    {
        _progressText.text = $"Progress: {taskIndex}/{taskCount}";
    }
}
