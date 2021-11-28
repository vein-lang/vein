namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using reflection;

    public class VeinModule
    {
        public string Name { get; protected set; }
        public Version Version { get; protected set; } = new(1, 0, 0, 0);
        protected internal List<VeinModule> Deps { get; set; } = new();

        internal VeinModule(string name) => Name = name;
        internal VeinModule(string name, Version ver) => (Name, Version) = (name, ver);

        protected internal List<Aspect> aspects { get; } = new();
        protected internal ConstStorage const_table { get; set; } = new();
        protected internal readonly List<VeinClass> class_table = new();
        protected internal readonly Dictionary<int, string> strings_table = new();
        protected internal readonly Dictionary<int, QualityTypeName> types_table = new();
        protected internal readonly Dictionary<int, FieldName> fields_table = new();

        protected internal readonly MutexStorage Interlocker = new ();

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
        public VeinClass TryFindType(string typename, List<string> includes)
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
        public VeinClass FindType(string typename, List<string> includes, bool throwWhenNotFound = true)
        {
            var result = class_table.Where(x => includes.Contains(x.FullName.Namespace)).
                FirstOrDefault(x => x.Name.Equals(typename));
            if (result is not null)
                return result;
            foreach (var module in Deps)
            {
                result = module.FindType(typename, includes, throwWhenNotFound);
                if (result is not null)
                    return result;
            }

            if (!throwWhenNotFound)
                return null;
            throw new TypeNotFoundException($"'{typename}' not found in modules and dependency assemblies.");
        }

        /// <summary>
        /// Find type by typename.
        /// </summary>
        /// <exception cref="TypeNotFoundException"></exception>
        /// <remarks>
        /// Support find in external deps.
        /// </remarks>
        public VeinClass FindType(QualityTypeName type, bool findExternally = false, bool dropUnresolvedException = true)
        {
            if (!findExternally)
                findExternally = this.Name != type.AssemblyName;
            var result = class_table.FirstOrDefault(filter);

            if (result is not null)
                return result;

            bool filter(VeinClass x) => x!.FullName.Equals(type);

            VeinClass createResult()
            {
                if (dropUnresolvedException)
                    throw new TypeNotFoundException($"'{type}' not found in modules and dependency assemblies.");
                return new UnresolvedVeinClass(type);
            }

            if (!findExternally)
                return createResult();

            foreach (var module in Deps)
            {
                result = module.FindType(type, true, dropUnresolvedException);
                if (result is not null)
                    return result;
            }

            return createResult();
        }

        internal void WriteToConstStorage<T>(FieldName field, T value)
            => const_table.Stage(field, value);

        public T ReadFromConstStorage<T>(FieldName field)
            => (T)const_table.Get(field);



        public class MutexStorage
        {
            public object INIT_ARRAY_BARRIER = new ();
        }
    }
}
