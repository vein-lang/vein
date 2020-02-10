namespace wave
{
    public interface IArg<out I>
    {
        I Get();
    }
}