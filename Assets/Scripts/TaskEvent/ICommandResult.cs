namespace TaskEvent
{
    public interface ICommandResult
    {
        
    }

    public class NullResult : ICommandResult
    {
        public static readonly NullResult Default = new NullResult();
    }

    public class ErrorResult : ICommandResult
    {
        public static readonly ErrorResult Default = new ErrorResult();
    }

    public class SetNumberResult : ICommandResult
    {
        public int BeforeNumber;
        public int AfterNumber;

        public SetNumberResult(int beforeNumber, int afterNumber)
        {
            BeforeNumber = beforeNumber;
            AfterNumber = afterNumber;
        }
    }
    
    public class AddNumberResult : ICommandResult
    {
        public int BeforeNumber;
        public int AfterNumber;

        public AddNumberResult(int beforeNumber, int afterNumber)
        {
            BeforeNumber = beforeNumber;
            AfterNumber = afterNumber;
        }
    }
}
