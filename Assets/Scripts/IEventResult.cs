namespace TaskEventResult
{
    public interface IEventResult
    {
        
    }

    public class NullResult : IEventResult
    {
        public static readonly NullResult Default = new NullResult();
    }

    public class ErrorResult : IEventResult
    {
        public static readonly ErrorResult Default = new ErrorResult();
    }

    public class SetNumberResult : IEventResult
    {
        public int BeforeNumber;
        public int AfterNumber;

        public SetNumberResult(int beforeNumber, int afterNumber)
        {
            BeforeNumber = beforeNumber;
            AfterNumber = afterNumber;
        }
    }
    
    public class AddNumberResult : IEventResult
    {
        public int BeforeNumber;
        public int AfterNumber;

        public AddNumberResult(int beforeNumber, int afterNumber)
        {
            BeforeNumber = beforeNumber;
            AfterNumber = afterNumber;
        }
    }

    public class PrintNumberResult : IEventResult
    {
        public int Number;

        public PrintNumberResult(int number)
        {
            Number = number;
        }
    }
}
