namespace TaskEvent
{
    public interface ICommand
    {
        
    }

    public class SetNumber : ICommand
    {
        public int Number;
    }
    
    public class AddNumber : ICommand
    {
        public int Number;
    }
}