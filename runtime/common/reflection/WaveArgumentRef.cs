namespace vein.runtime
{
    using System;

    public class ManaArgumentRef
    {
        public ManaClass Type { get; set; }
        public string Name { get; set; }



        public static implicit operator ManaArgumentRef((VeinTypeCode code, string name) data)
        {
            var (code, name) = data;
            return new ManaArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
        public static implicit operator ManaArgumentRef((string name, VeinTypeCode code) data)
        {
            var (name, code) = data;
            return new ManaArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
    }
}
