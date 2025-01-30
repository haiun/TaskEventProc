using System.Threading;

namespace TaskEvent
{
    /*
     * 이벤트를 타입별로 실행하고 결과를 누산합니다.
     */
    public class TaskEventProcessor : TaskEventConsumer
    {
        private readonly ITaskEventPresenter _taskEventPresenter;
        public int AccumulatedValue { get; private set; }
    
        public TaskEventProcessor(int accumulatedValue, ITaskEventPresenter taskEventPresenter, CancellationToken ct) : base(ct)
        {
            _taskEventPresenter = taskEventPresenter;
            AccumulatedValue = accumulatedValue;
        }

        protected override void ProcessEventQueueItemImpl(TaskEventQueueItem queueItem)
        {
            ICommandResult commandResult = NullResult.Default;
            switch (queueItem.Command)
            {
                case SetNumber setNumber:
                {
                    int beforeNumber = AccumulatedValue;
                    AccumulatedValue = setNumber.Number;
                    commandResult = new SetNumberResult(beforeNumber, AccumulatedValue);
                    break;
                }

                case AddNumber addNumber:
                {
                    int beforeNumber = AccumulatedValue;
                    AccumulatedValue = beforeNumber + addNumber.Number;
                    commandResult = new AddNumberResult(beforeNumber, AccumulatedValue);
                    break;
                }
            }

            _taskEventPresenter?.OnProcessed(queueItem.Command, commandResult);
            queueItem.TaskCompletionSource?.TrySetResult(commandResult);
        }
    }
}