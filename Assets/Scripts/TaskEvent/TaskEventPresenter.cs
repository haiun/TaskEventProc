using TaskEvent;
using TaskEventResult;

public interface ITaskEventPresenter
{
    void OnProcessed(ICommand command, ICommandResult result);
    void OnComplete(int producerId, ICommand command, ICommandResult result);
}
