namespace wave
{
    public interface IArgs<out I>
    {
        I[] Get();
    }
}