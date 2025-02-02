using TMPro;
using UnityEngine;

/*
 * 실행중인 작업진행 상황을 표시합니다.
 */
public class ActiveTaskView : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _progressText;

    public void SetTaskProgress(int taskIndex, int taskCount)
    {
        _progressText.text = $"Progress: {taskIndex}/{taskCount}";
    }
}
