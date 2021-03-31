namespace insomnia.emit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class WaveModule
    {
        public string Name { get; protected set; }
        public Version Version { get; protected set; } = new (1, 0, 0, 0);
        protected internal List<WaveModule> Deps { get; set; } = new();

        internal WaveModule(string name) => Name = name;
        internal WaveModule(string name, Version ver) => (Name, Version) = (name, ver);


        protected internal ConstStorage const_table { get; set; } = new();
        protected internal readonly List<WaveClass> class_table = new();
        protected internal readonly Dictionary<int, string> strings_table = new();
        protected internal readonly Dictionary<int, QualityTypeName> types_table = new();
        protected internal readonly Dictionary<int, FieldName> fields_table = new();

        public string GetConstStringByIndex(int index) =>
            strings_table.GetValueOrDefault(index) ?? 
            throw new AggregateException($"String by index  '{index}' not found in module '{Name}'.");

        public QualityTypeName GetTypeNameByIndex(int index) =>
            types_table.GetValueOrDefault(index) ?? 
            throw new AggregateException($"TypeName by index '{index}' not found in module '{Name}'.");

        public FieldName GetFieldNameByIndex(int index) =>
            fields_table.GetValueOrDefault(index) ?? 
            throw new AggregateException($"FieldName by index '{index}' not found in module '{Name}'.");


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
            var result = class_table.Where(x => includes.Contains(x.FullName.Namespace)).
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
                return class_table.First(filter).AsType();
            var result = class_table.FirstOrDefault(filter)?.AsType();
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

        internal void WriteToConstStorage<T>(FieldName field, T value) 
            => const_table.Stage(field, value);

        public T ReadFromConstStorage<T>(FieldName field)
            => (T)const_table.Get(field);
    }
}