namespace wave.clr.emit
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using runtime;

    public class ManaClassBuilder : ManaClass, IBaker
    {
        internal ManaModuleBuilder moduleBuilder;
        internal TypeBuilder classBuilder;

        public List<string> Includes { get; set; } = new ();

        internal ManaClassBuilder WithIncludes(List<string> includes)
        {
            Includes.AddRange(includes);
            return this;
        }

        internal ManaClassBuilder(ManaModuleBuilder module, ManaClass clazz, TypeBuilder typeBuilder)
        {
            this.moduleBuilder = module;
            this.FullName = clazz.FullName;
            this.Parent = clazz.Parent;
            this.TypeCode = clazz.TypeCode;
            this.classBuilder = typeBuilder;
        }
        internal ManaClassBuilder(ManaModuleBuilder module, QualityTypeName name, TypeBuilder typeBuilder, ManaTypeCode parent = ManaTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parent = parent.AsType().AsClass();
            this.classBuilder = typeBuilder;
        }
        internal ManaClassBuilder(ManaModuleBuilder module, QualityTypeName name, TypeBuilder typeBuilder, ManaType parent)
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
        public ManaMethodBuilder DefineMethod(string name, ManaType returnType, MethodFlags flags, params ManaArgumentRef[] args)
        {
            var method = new ManaMethodBuilder(this, name, returnType, flags, args);
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
        public ManaField DefineField(string name, FieldFlags flags, ManaType fieldType)
        {
            var field = new ManaField(this, new FieldName(name, this.Name), flags, fieldType);
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