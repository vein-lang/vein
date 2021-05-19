namespace wave.llvm.emit
{
    using System.Collections.Generic;
    using runtime;

    public class ClassBuilder : ManaClass, IBaker
    {
        internal ManaModuleBuilder moduleBuilder;

        public List<string> Includes { get; set; } = new ();

        internal ClassBuilder WithIncludes(List<string> includes)
        {
            Includes.AddRange(includes);
            return this;
        }

        internal ClassBuilder(ManaModuleBuilder module, ManaClass clazz)
        {
            this.moduleBuilder = module;
            this.FullName = clazz.FullName;
            this.Parent = clazz.Parent;
            this.TypeCode = clazz.TypeCode;
        }
        internal ClassBuilder(ManaModuleBuilder module, QualityTypeName name, ManaTypeCode parent = ManaTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsType().AsClass();
        }
        internal ClassBuilder(ManaModuleBuilder module, QualityTypeName name, ManaType parent)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsClass();
        }
        /// <summary>
        /// Get class <see cref="QualityTypeName"/>.
        /// </summary>
        public QualityTypeName GetName() => this.FullName;
        
        /// <summary>
        /// Define method in current class.
        /// </summary>
        /// <remarks>
        /// Method name will be interned.
        /// </remarks>
        public MethodBuilder DefineMethod(string name, ManaType returnType, params ManaArgumentRef[] args)
        {
            moduleBuilder.InternString(name);
            var method = new MethodBuilder(this, name, returnType, args);
            Methods.Add(method);
            return method;
        }
        /// <summary>
        /// Define method in current class.
        /// </summary>
        /// <remarks>
        /// Method name will be interned.
        /// </remarks>
        public MethodBuilder DefineMethod(string name, MethodFlags flags, ManaType returnType, params ManaArgumentRef[] args)
        {
            var method = this.DefineMethod(name, returnType, args);
            method.Owner = this;
            method.Flags = flags;
            return method;
        }
        /// <summary>
        /// Define field in current class.
        /// </summary>
        /// <remarks>
        /// Field name will be interned.
        /// </remarks>
        public ManaField DefineField(string name, FieldFlags flags, ManaType fieldType)
        {
            var field = new ManaField(this, new FieldName(name, this.Name), flags, fieldType);
            moduleBuilder.InternFieldName(field.FullName);
            Fields.Add(field);
            return field;
        }

        #region Implementation of IBaker

        public byte[] BakeByteArray()
        {
            throw new System.NotImplementedException();
        }

        public string BakeDebugString()
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}