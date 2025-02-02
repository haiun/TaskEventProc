namespace TaskEvent
{
    public interface ICommand
    {
        
    }

    /*
     * 숫자로 설정합니다.
     */
    public class SetNumber : ICommand
    {
        public int Number;
    }
    
    /*
     * 숫자를 더합니다.
     */
    public class AddNumber : ICommand
    {
        public int Number;
    }
}