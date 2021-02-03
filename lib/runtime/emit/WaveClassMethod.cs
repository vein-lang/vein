namespace wave.emit
{
    using System.Collections.Generic;

    public class WaveClassMethod
    {
        public WaveRuntimeType ReturnType { get; set; }
        public WaveClass Owner { get; set; }
        public List<WaveArgumentRef> Arguments { get; } = new();
        public string Name { get; set; }

        public int ArgLength => Arguments.Count;
        
        public MethodFlags Flags { get; set; }

        public bool IsStatic => Flags.HasFlag(MethodFlags.Static);
        public bool IsPrivate => Flags.HasFlag(MethodFlags.Private);
        public bool IsExtern => Flags.HasFlag(MethodFlags.Extern);
    }
}