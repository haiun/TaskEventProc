using System.Threading;
using TaskEventResult;

public class TaskEventProcessor : TaskEventConsumer
{
    private readonly ITaskEventPresenter _taskEventPresenter;
    public int Number { get; private set; }
    
    public TaskEventProcessor(int number, ITaskEventPresenter taskEventPresenter, CancellationToken ct) : base(ct)
    {
        _taskEventPresenter = taskEventPresenter;
        Number = number;
    }

    protected override void ProcessEventQueueItemImpl(TaskEventQueueItem queueItem)
    {
        ICommandResult commandResult = NullResult.Default;
        switch (queueItem.Command)
        {
            case TaskEvent.SetNumber setNumber:
            {
                int beforeNumber = Number;
                Number = setNumber.Number;
                commandResult = new SetNumberResult(beforeNumber, Number);
                break;
            }

            case TaskEvent.AddNumber addNumber:
            {
                int beforeNumber = Number;
                Number = beforeNumber + addNumber.Number;
                commandResult = new AddNumberResult(beforeNumber, Number);
                break;
            }

            case TaskEvent.PrintNumber printNumber:
            {
                commandResult = new PrintNumberResult(Number);
                break;
            }
        }

        _taskEventPresenter?.OnProcessed(queueItem.Command, commandResult);
        queueItem.TaskCompletionSource?.TrySetResult(commandResult);
    }
}