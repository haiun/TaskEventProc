namespace TaskEvent
{
    public interface ICommandResult
    {
        
    }

    public class NullResult : ICommandResult
    {
        public static readonly NullResult Default = new();
    }

    public class ErrorResult : ICommandResult
    {
        public static readonly ErrorResult Default = new();
    }

    /*
     * SetNumber의 실행결과입니다.
     */
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
    
    /*
     * AddNumber의 실행결과입니다.
     */
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
