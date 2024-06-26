namespace ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;
    using vein.exceptions;
    using vein.extensions;
    using vein.runtime;

    public class ClassBuilder : VeinClass, IBaker
    {
        internal VeinModuleBuilder moduleBuilder;

        public List<string> Includes { get; set; } = new();

        internal ClassBuilder WithIncludes(List<string> includes)
        {
            Includes.AddRange(includes);
            return this;
        }

        internal ClassBuilder(VeinModuleBuilder module, VeinClass clazz)
        {
            this.moduleBuilder = module;
            this.FullName = clazz.FullName;
            this.Parents.AddRange(clazz.Parents);
            this.TypeCode = clazz.TypeCode;
            this.Owner = module;
        }
        internal ClassBuilder(VeinModuleBuilder module, QualityTypeName name, VeinTypeCode parent = VeinTypeCode.TYPE_OBJECT)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parents.Add(parent.AsClass()(module.Types));
            this.Owner = module;
        }
        internal ClassBuilder(VeinModuleBuilder module, QualityTypeName name, VeinClass parent)
        {
            this.FullName = name;
            moduleBuilder = module;
            this.Parents.Add(parent);
            this.Owner = module;
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
        public MethodBuilder DefineMethod(string name, VeinClass returnType, params VeinArgumentRef[] args)
        {
            moduleBuilder.InternString(name);
            var method = new MethodBuilder(this, name, returnType, args);

            if (Methods.Any(x => x.Name == method.Name))
                throw new MethodAlreadyDefined($"Method '{method.Name}' in class '{Name}' already defined.");
            Methods.Add(method);
            return method;
        }
        /// <summary>
        /// Define method in current class.
        /// </summary>
        /// <remarks>
        /// Method name will be interned.
        /// </remarks>
        public MethodBuilder DefineMethod(string name, MethodFlags flags, VeinClass returnType, params VeinArgumentRef[] args)
        {
            var method = this.DefineMethod(name, returnType, args);
            method.Flags = flags;
            return method;
        }
        /// <summary>
        /// Define field in current class.
        /// </summary>
        /// <remarks>
        /// Field name will be interned.
        /// </remarks>
        public VeinField DefineField(string name, FieldFlags flags, VeinClass fieldType)
        {
            var field = new VeinField(this, new FieldName(name, this.Name), flags, fieldType);
            moduleBuilder.InternFieldName(field.FullName);
            if (Fields.Any(x => x.Name == name))
                throw new FieldAlreadyDefined($"Field '{name}' in class '{Name}' already defined.");
            Fields.Add(field);
            return field;
        }

        /// <summary>
        /// Define auto property in current class.
        /// </summary>
        public VeinProperty DefineAutoProperty(string name, FieldFlags flags, VeinClass propType)
        {
            var prop = new VeinProperty(this, new (name, this.Name), flags, propType);

            if (!IsAbstract)
            {
                prop.ShadowField = DefineField(VeinProperty.GetShadowFieldName(prop.FullName), FieldFlags.Special,
                    propType);
                if (flags.HasFlag(FieldFlags.Static))
                    prop.ShadowField.Flags |= FieldFlags.Static;
            }


            var args = prop.IsStatic
                ? Array.Empty<VeinArgumentRef>()
                :
                [
                    new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, prop.Owner)
                ];

            var getter =
                DefineMethod(VeinProperty.GetterFnName(name), VeinProperty.ConvertShadowFlags(flags), propType, args);
            if (!IsAbstract)
            {
                if (flags.HasFlag(FieldFlags.Static))
                    getter.GetGenerator()
                        .Emit(OpCodes.LDSF, prop.ShadowField)
                        .Emit(OpCodes.RET);
                else
                    getter.GetGenerator()
                        .Emit(OpCodes.LDARG_0)
                        .Emit(OpCodes.LDF, prop.ShadowField)
                        .Emit(OpCodes.RET);
            }
            else
                getter.Flags |= MethodFlags.Abstract;
            prop.Getter = getter;

            if (flags.HasFlag(FieldFlags.Readonly))
                return prop;
            args = prop.IsStatic
                ? [new VeinArgumentRef("value", prop.PropType)]
                :
                [
                    new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, prop.Owner),
                    new VeinArgumentRef("value", prop.PropType)
                ];
            var setter =
                DefineMethod(VeinProperty.SetterFnName(name), VeinProperty.ConvertShadowFlags(flags),
                    VeinTypeCode.TYPE_VOID.AsClass(propType),
                    args);
            if (!IsAbstract)
            {
                if (flags.HasFlag(FieldFlags.Static))
                    setter.GetGenerator()
                        .Emit(OpCodes.LDARG_0) // emit value ref
                        .Emit(OpCodes.STSF, prop.ShadowField)
                        .Emit(OpCodes.RET);
                else
                    setter.GetGenerator()
                        .Emit(OpCodes.LDARG_1) // emit value ref
                        .Emit(OpCodes.LDARG_0) // emit this
                        .Emit(OpCodes.STF, prop.ShadowField)
                        .Emit(OpCodes.RET);
            }
            else
                setter.Flags |= MethodFlags.Abstract;
            prop.Setter = setter;

            return prop;
        }

        public VeinProperty DefineEmptyProperty(string name, FieldFlags flags, VeinClass propType)
            => new(this, new(name, this.Name), flags, propType);

        byte[] IBaker.BakeByteArray()
        {
            if (Methods.Count == 0 && Fields.Count == 0)
                return Array.Empty<byte>();
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);

            binary.WriteTypeName(this.FullName, moduleBuilder);
            binary.Write((short)Flags);

            binary.Write((short)Parents.Count);
            foreach (var parent in Parents)
                binary.WriteTypeName(parent.FullName, moduleBuilder);
            binary.Write(Methods.Count);
            foreach (var method in Methods.OfType<IBaker>())
            {
                var body = method.BakeByteArray();
                binary.Write(body.Length);
                binary.Write(body);
            }
            binary.Write(Fields.Count);
            foreach (var field in Fields)
            {
                binary.Write(moduleBuilder.InternFieldName(field.FullName));
                binary.WriteTypeName(field.FieldType.FullName, moduleBuilder);
                binary.Write((short)field.Flags);
            }
            return mem.ToArray();
        }

        string IBaker.BakeDebugString()
        {
            var str = new StringBuilder();
            str.AppendLine($".namespace '{FullName.Namespace}'");
            if (IsInterface) str.Append($".interface ");
            else if (IsValueType) str.Append($".struct ");
            else str.Append($".class ");
            str.Append($"'{FullName.Name}' {Flags.EnumerateFlags(new[] { ClassFlags.None, ClassFlags.Interface }).Join(' ').ToLowerInvariant()}");
            str.AppendLine($" extends {Parents.Select(x => $"'{x.Name}'").Join(", ")}");
            str.AppendLine("{");
            foreach (var field in Fields)
            {
                var flags = field.Flags.EnumerateFlags(new [] {FieldFlags.None}).Join(' ').ToLowerInvariant();
                str.AppendLine($"\t.field '{field.Name}' as '{field.FieldType.Name}' {flags}");
            }
            foreach (var method in Methods.OfType<IBaker>().Select(method => method.BakeDebugString()))
                str.AppendLine($"{method.Split("\n").Select(x => $"\t{x}").Join("\n").TrimEnd('\n')}");
            str.AppendLine("}");
            return str.ToString();
        }

        #region Overrides of VeinClass

        protected override VeinMethod GetOrCreateTor(string name, bool isStatic = false)
        {
            var ctor = base.GetOrCreateTor(name, isStatic);
            if (ctor is not null)
                return ctor;

            var flags = MethodFlags.Public | MethodFlags.Special;

            if (isStatic)
                flags |= MethodFlags.Static;

            var returnType = isStatic ? VeinTypeCode.TYPE_VOID.AsClass(moduleBuilder) : this;
            var args = isStatic || name == "dtor" ? new VeinArgumentRef[0] :
                new VeinArgumentRef[1] { new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, this) };

            ctor = DefineMethod(name, flags, returnType, args);
            moduleBuilder.InternString(ctor.Name);

            return ctor;
        }

        #endregion

        public ulong? FindMemberField(FieldName field)
            => throw new NotImplementedException();
    }
}
