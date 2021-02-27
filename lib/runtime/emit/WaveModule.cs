namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class WaveModule
    {
        public string Name { get; protected set; }
        protected internal readonly Dictionary<int, string> strings = new();
        protected internal readonly List<WaveClass> classList = new();
        protected internal List<WaveModule> Deps { get; set; } = new();

        public WaveModule(string name) => Name = name;
        
        
        public string GetConstByIndex(int index) 
            => strings.GetValueOrDefault(index) ?? 
               throw new AggregateException($"Index '{index}' not found in module '{Name}'.");
        
        
        public WaveType FindType(QualityTypeName type, bool findExternally = false)
        {
            bool filter(WaveClass x) => x!.FullName.Equals(type);
            if (!findExternally)
                return classList.First(filter).AsType();
            var result = classList.FirstOrDefault(filter)?.AsType();
            if (result is not null)
                return result;
            
            foreach (var module in Deps)
            {
                result = module.FindType(type, true);
                if (result is not null)
                    return result;
            }

            throw new TypeNotFoundException($"'{type}' not found in modules and dependency assemblies.");
        }
    }
}