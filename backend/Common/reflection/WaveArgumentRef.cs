namespace mana.runtime
{
    using System;

    public class ManaArgumentRef
    {
        public ManaClass Type { get; set; }
        [Obsolete]
        public RuntimeToken Token => RuntimeToken.Create(Name);
        public string Name { get; set; }



        public static implicit operator ManaArgumentRef((ManaTypeCode code, string name) data)
        {
            var (code, name) = data;
            return new ManaArgumentRef
            {
                Name = name,
                Type = code.AsClass()
            };
        }
        public static implicit operator ManaArgumentRef((string name, ManaTypeCode code) data)
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