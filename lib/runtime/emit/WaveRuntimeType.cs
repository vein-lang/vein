namespace wave.emit
{
    public class WaveRuntimeType
    {
        public WaveTypeCode TypeCode;
        public object Data;

        public bool IsClass() => Data is WaveClass;
        public bool IsType() => Data is WaveRuntimeType;
    }
}