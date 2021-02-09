namespace wave.emit
{
    public static class WaveClassExtensions
    {
        public static WaveType AsType(this WaveClass @class) =>
            @class.Type;
    }
}