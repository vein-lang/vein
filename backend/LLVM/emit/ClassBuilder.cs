namespace wave.llvm.emit
{
    using System.Collections.Generic;
    using runtime;

    public class ClassBuilder : WaveClass, IBaker
    {
        internal WaveModuleBuilder moduleBuilder;

        public List<string> Includes { get; set; } = new ();

        internal ClassBuilder WithIncludes(List<string> includes)
        {
            Includes.AddRange(includes);
            return this;
        }

        internal ClassBuilder(WaveModuleBuilder module, WaveClass clazz)
        {
            this.moduleBuilder = module;
            this.FullName = clazz.FullName;
            this.Parent = clazz.Parent;
            this.TypeCode = clazz.TypeCode;
        }
        internal ClassBuilder(WaveModuleBuilder module, QualityTypeName name, WaveTypeCode parent = WaveTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsType().AsClass();
        }
        internal ClassBuilder(WaveModuleBuilder module, QualityTypeName name, WaveType parent)
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
        public MethodBuilder DefineMethod(string name, WaveType returnType, params WaveArgumentRef[] args)
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
        public MethodBuilder DefineMethod(string name, MethodFlags flags, WaveType returnType, params WaveArgumentRef[] args)
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
        public WaveField DefineField(string name, FieldFlags flags, WaveType fieldType)
        {
            var field = new WaveField(this, new FieldName(name, this.Name), flags, fieldType);
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