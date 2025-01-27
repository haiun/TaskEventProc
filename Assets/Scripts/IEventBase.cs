namespace TaskEvent
{
    public interface IEventBase
    {
        
    }

    public class SetNumber : IEventBase
    {
        public int Number;
    }
    
    public class AddNumber : IEventBase
    {
        public int Number;
    }

    public class PrintNumber : IEventBase
    {
        
    }
}