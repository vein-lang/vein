namespace vein.runtime
{
    using System;

    public class VeinArgumentRef
    {
        public const string THIS_ARGUMENT = "<this>";

        public VeinClass Type { get; set; }
        public string Name { get; set; }

        public VeinArgumentRef() { }
        public VeinArgumentRef(string name, VeinClass clazz)
            => (Name, Type) = (name, clazz);


        public static VeinArgumentRef Create(VeinCore types, (string name, VeinTypeCode code) data)
        {
            var (name, code) = data;
            return new VeinArgumentRef
            {
                Name = name,
                Type = code.AsClass()(types)
            };
        }

        public static VeinArgumentRef Create(VeinCore types, (VeinTypeCode code, string name) data) =>
            Create(types, (data.name, data.code));
    }
}
