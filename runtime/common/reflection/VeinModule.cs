namespace vein.runtime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using extensions;
    using reflection;

    public class VeinModule
    {
        public VeinCore Types { get; }
        public ModuleNameSymbol Name { get; protected set; }
        public Version Version { get; internal set; } = new(1, 0, 0, 0);
        protected internal List<VeinModule> Deps { get; set; } = new();

        internal VeinModule(ModuleNameSymbol name, VeinCore types) => (Name, Types) = (name, types);
        internal VeinModule(ModuleNameSymbol name, Version ver, VeinCore types) => (Name, Version, Types) = (name, ver, types);

        public List<Aspect> aspects { get; } = new();
        public ConstStorage const_table { get; set; } = new();
        public readonly List<VeinClass> class_table = new();
        public readonly List<VeinAlias> alias_table = new();
        public readonly Dictionary<int, string> strings_table = new();
        public readonly Dictionary<int, QualityTypeName> types_table = new();
        public readonly Dictionary<int, VeinTypeArg> generics_table = new();
        public readonly Dictionary<int, FieldName> fields_table = new();
        
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
        public VeinClass TryFindType(NameSymbol typename, List<NamespaceSymbol> includes)
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
        /// Find type by pattern (without namespace) with namespace includes.
        /// </summary>
        /// <exception cref="TypeNotFoundException"></exception>
        public VeinClass FindType(Regex pattern, List<NamespaceSymbol> includes, bool throwWhenNotFound = true)
        {
            var result = class_table.Where(x => includes.Contains(x.FullName.Namespace)).
                FirstOrDefault(x => pattern.IsMatch(x.Name.name));
            if (result is not null)
                return result;
            foreach (var module in Deps)
            {
                result = module.FindType(pattern, includes, throwWhenNotFound);
                if (result is not null)
                    return result;
            }

            if (alias_table.Any(x => pattern.IsMatch(x.aliasName.Name.name) && includes.Contains(x.aliasName.Namespace)))
            {
                var alias = alias_table.First(x => pattern.IsMatch(x.aliasName.Name.name) && includes.Contains(x.aliasName.Namespace));

                if (alias is VeinAliasType type)
                    return type.type;
            }


            if (!throwWhenNotFound)
                return null;
            throw new TypeNotFoundException($"Type with pattern '{pattern}' not found in modules and dependency assemblies.");
        }

        /// <summary>
        /// Find type by name (without namespace) with namespace includes.
        /// </summary>
        /// <exception cref="TypeNotFoundException"></exception>
        public VeinClass FindType(NameSymbol typename, List<NamespaceSymbol> includes, bool throwWhenNotFound = true)
        {
            var result = class_table.Where(x => includes.Contains(x.FullName.Namespace)).
                FirstOrDefault(x => x.Name == typename);
            if (result is not null)
                return result;
            foreach (var module in Deps)
            {
                result = module.FindType(typename, includes, throwWhenNotFound);
                if (result is not null)
                    return result;
            }

            if (alias_table.Any(x => x.aliasName.Name.Equals(typename) && includes.Contains(x.aliasName.Namespace)))
            {
                var alias = alias_table.First(x => x.aliasName.Name.Equals(typename) && includes.Contains(x.aliasName.Namespace));

                if (alias is VeinAliasType type)
                    return type.type;
            }

            if (!throwWhenNotFound)
                return null;
            throw new TypeNotFoundException($"'{typename}' not found in '{includes.Select(x => x.@namespace).Join(',')}' namespaces,\nmaybe you missing use directive?");
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
                findExternally = Name != type.ModuleName;
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
    }
}
