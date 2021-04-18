namespace wave.clr.emit
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using runtime;

    public class WaveClassBuilder : WaveClass, IBaker
    {
        internal WaveModuleBuilder moduleBuilder;
        internal TypeBuilder classBuilder;

        public List<string> Includes { get; set; } = new ();

        internal WaveClassBuilder WithIncludes(List<string> includes)
        {
            Includes.AddRange(includes);
            return this;
        }

        internal WaveClassBuilder(WaveModuleBuilder module, WaveClass clazz, TypeBuilder typeBuilder)
        {
            this.moduleBuilder = module;
            this.FullName = clazz.FullName;
            this.Parent = clazz.Parent;
            this.TypeCode = clazz.TypeCode;
            this.classBuilder = typeBuilder;
        }
        internal WaveClassBuilder(WaveModuleBuilder module, QualityTypeName name, TypeBuilder typeBuilder, WaveTypeCode parent = WaveTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsType().AsClass();
            this.classBuilder = typeBuilder;
        }
        internal WaveClassBuilder(WaveModuleBuilder module, QualityTypeName name, TypeBuilder typeBuilder, WaveType parent)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.classBuilder = typeBuilder;
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
        public WaveMethodBuilder DefineMethod(string name, WaveType returnType, MethodFlags flags, params WaveArgumentRef[] args)
        {
            var method = new WaveMethodBuilder(this, name, returnType, flags, args);
            Methods.Add(method);
            method.Owner = this;
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