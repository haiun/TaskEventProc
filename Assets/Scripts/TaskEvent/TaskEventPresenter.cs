namespace TaskEvent
{
    /*
     * 이벤트가 실행되거나 완료됨을 통지합니다.
     */
    public interface ITaskEventPresenter
    {
        /*
         * TaskEventProcessor에서 실행이 완료됨을 통지합니다.
         */
        void OnProcessed(ICommand command, ICommandResult result);
        
        /*
         * TaskEventProducer에서 실행결과가 반환됨을 통지합니다.
         */
        void OnComplete(int producerId, ICommand command, ICommandResult result);
    }
}
