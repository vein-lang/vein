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

        internal WaveModule(string name) => Name = name;
        
        /// <summary>
        /// Get interned string from storage by index.
        /// </summary>
        /// <exception cref="AggregateException"></exception>
        public string GetConstByIndex(int index) 
            => strings.GetValueOrDefault(index) ?? 
               throw new AggregateException($"Index '{index}' not found in module '{Name}'.");

        /// <summary>
        /// Try find type by name (without namespace) with namespace includes.
        /// </summary>
        public WaveType TryFindType(string typename, List<string> includes)
        {
            try
            {
                return FindType(typename, includes);
            }
            catch (TypeNotFoundException)
            {
                return null;
            }
        }
        /// <summary>
        /// Find type by name (without namespace) with namespace includes.
        /// </summary>
        /// <exception cref="TypeNotFoundException"></exception>
        public WaveType FindType(string typename, List<string> includes)
        {
            var result = classList.Where(x => includes.Contains(x.FullName.Namespace)).
                FirstOrDefault(x => x.Name.Equals(typename))?.AsType();
            if (result is not null)
                return result;
            foreach (var module in Deps)
            {
                result = module.FindType(typename, includes);
                if (result is not null)
                    return result;
            }
            throw new TypeNotFoundException($"'{typename}' not found in modules and dependency assemblies.");
        }
        /// <summary>
        /// Find type by typename.
        /// </summary>
        /// <exception cref="TypeNotFoundException"></exception>
        /// <remarks>
        /// Support find in external deps.
        /// </remarks>
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