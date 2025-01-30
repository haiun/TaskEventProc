namespace TaskEvent
{
    /*
     * 이벤트가 실행되거나 완료됨을 통지합니다.
     */
    public interface ITaskEventPresenter
    {
        void OnProcessed(ICommand command, ICommandResult result);
        void OnComplete(int producerId, ICommand command, ICommandResult result);
    }
}
