namespace wave.llvm.emit
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using exceptions;
    using insomnia;
    using LLVMSharp;
    using runtime;
    using Serilog;

    public class ManaModuleBuilder : ManaModule, IBaker
    {
        private ILogger logger => Journal.Get(nameof(ManaModule));
        internal LLVMModuleRef @ref;

        public ManaModuleBuilder(string name) : base(name) 
            => @ref = LLVM.ModuleCreateWithName(name);

        public ManaModuleBuilder(string name, Version ver) : base(name, ver) 
            => @ref = LLVM.ModuleCreateWithName(name);


        /// <summary>
        /// Define class by name.
        /// </summary>
        /// <remarks>
        /// 'assemblyName%global::namespace/className' - VALID
        /// <br/>
        /// 'global::namespace/className' - VALID
        /// <br/>
        /// 'namespace/className' - INVALID, need 'global::' prefix.
        /// <br/>
        /// 'className' - INVALID, need describe namespace.
        /// </remarks>
        /// <exception cref="IncompleteClassNameException">See 'remarks'.</exception>
        public ClassBuilder DefineClass(string classFullname)
        {
            if (!classFullname.Contains("/"))
                throw new IncompleteClassNameException("Class name not contained namespace.");
            var typename = default(QualityTypeName);
            if (classFullname.Contains("%"))
            {
                if (!classFullname.StartsWith($"{Name}%"))
                    throw new IncompleteClassNameException($"Class name contains incorrect assembly name.");
                typename = new QualityTypeName(classFullname);
            }
            else
                typename = new QualityTypeName($"{Name}%{classFullname}");

            if (typename.TryGet(x => x.Namespace) is null)
                throw new IncompleteClassNameException($"Class name has incorrect format.");
            if (!typename.Namespace.StartsWith("global::"))
                throw new IncompleteClassNameException($"Class namespace not start with 'global::'.");

            return DefineClass(typename);
        }
        /// <summary>
        /// Define class by name.
        /// </summary>
        public ClassBuilder DefineClass(QualityTypeName name)
        {
            if (class_table.Any(x => x.FullName.Equals(name)))
                throw new DuplicateNameException($"Class '{name}' already defined.");
            InternString(name.Name);
            InternString(name.Namespace);
            InternString(name.AssemblyName);
            var c = new ClassBuilder(this, name);
            class_table.Add(c);
            return c;
        }

        private int _intern<T>(Dictionary<int, T> storage, T val)
        {
            var (key, value) = storage.FirstOrDefault(x => x.Value.Equals(val));

            if (value is not null)
                return key;
            
            storage[key = storage.Count] = val;
            return key;
        }

        /// <summary>
        /// Intern string constant into module storage and return string index.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public int InternString(string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof (str));
            var key = _intern(strings_table, str);


            logger.Information("String constant '{str}' baked by index: {key}", str, key);
            return key;
        }
        /// <summary>
        /// Intern TypeName constant into module storage and return TypeName index.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public int InternTypeName(QualityTypeName name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof (name));
            var key = _intern(types_table, name);

            logger.Information("TypeName '{name}' baked by index: {key}", name, key);
            return key;
        }
        /// <summary>
        /// Intern FieldName constant into module storage and return FieldName index.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public int InternFieldName(FieldName name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof (name));

            var key = _intern(fields_table, name);

            logger.Information("FieldName '{name}' baked by index: {key}", name, key);
            return key;
        }
        
        internal (int, QualityTypeName) GetMethodToken(ManaMethod method) => 
            (this.InternString(method.Name), method.Owner.FullName);

        #region Implementation of IBaker

        public byte[] BakeByteArray()
        {
            throw new NotImplementedException();
        }

        public string BakeDebugString()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}