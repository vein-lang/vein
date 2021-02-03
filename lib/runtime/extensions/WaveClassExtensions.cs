namespace wave.emit
{
    public static class WaveClassExtensions
    {
        public static WaveRuntimeType AsType(this WaveClass @class) =>
            new()
            {
                Data = @class,
                TypeCode = WaveTypeCode.TYPE_CLASS
            };
        public static WaveRuntimeType AsType(this WaveClass @class, WaveTypeCode code) =>
            new()
            {
                Data = @class,
                TypeCode = code
            };
    }
}