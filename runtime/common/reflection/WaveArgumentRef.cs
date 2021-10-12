namespace vein.runtime
{
    using System;

    public class VeinArgumentRef
    {
        public VeinClass Type { get; set; }
        public string Name { get; set; }

        public VeinArgumentRef() { }
        public VeinArgumentRef(string name, VeinClass clazz)
            => (Name, Type) = (name, clazz);


        public static implicit operator VeinArgumentRef((VeinTypeCode code, string name) data)
        {
            var (code, name) = data;
            return new VeinArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
        public static implicit operator VeinArgumentRef((string name, VeinTypeCode code) data)
        {
            var (name, code) = data;
            return new VeinArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
    }
}
