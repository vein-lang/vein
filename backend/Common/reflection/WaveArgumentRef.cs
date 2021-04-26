namespace wave.runtime
{
    using System;

    public class WaveArgumentRef
    {
        public WaveClass Type { get; set; }
        [Obsolete]
        public RuntimeToken Token => RuntimeToken.Create(Name);
        public string Name { get; set; }
        
        
        
        public static implicit operator WaveArgumentRef((WaveTypeCode code, string name) data)
        {
            var (code, name) = data;
            return new WaveArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
        public static implicit operator WaveArgumentRef((string name, WaveTypeCode code) data)
        {
            var (name, code) = data;
            return new WaveArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
    }
}