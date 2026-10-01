namespace ishtar.emit.extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using vein.extensions;
    using vein.runtime;


    public delegate nint TypeNameGetter(int typeIndex);
    public static class QualityTypeEx
    {
        // TypeSpec tags for the recursive complex-type encoding. A type reference is one of:
        //   0 Concrete  — a closed class (types_table index)
        //   1 Param     — a bare generic parameter (generics_table index)
        //   2 Instance  — a constructed generic: open-definition index + arg count + nested TypeSpecs
        // The Instance case is recursive, so nested / "transit" generics (List<List<T>>, Array<T>
        // inside List<T>) are preserved on disk instead of being erased to the open definition.
        internal const byte TS_CONCRETE = 0;
        internal const byte TS_PARAM = 1;
        internal const byte TS_INSTANCE = 2;

        public static VeinComplexType ReadComplexType(this BinaryReader bin, VeinModule module, bool dropUnresolved = true)
        {
            var tag = bin.ReadByte();
            switch (tag)
            {
                case TS_PARAM:
                    return bin.ReadGenericTypeName(module);
                case TS_INSTANCE:
                {
                    var definition = module.FindType(bin.ReadTypeName(module), true, dropUnresolved);
                    var count = bin.ReadInt32();
                    var args = new List<VeinComplexType>(count);
                    for (var i = 0; i < count; i++)
                        args.Add(bin.ReadComplexType(module, dropUnresolved));
                    return VeinComplexType.Instance(definition, args);
                }
                case TS_CONCRETE:
                    return module.FindType(bin.ReadTypeName(module), true, dropUnresolved);
                default:
                    throw new InvalidOperationException($"Unknown TypeSpec tag '{tag}' in '{module.Name}' module.");
            }
        }


        public static QualityTypeName ReadTypeName(this BinaryReader bin, VeinModule module)
        {
            var typeIndex = bin.ReadInt32();

            return module.types_table.GetValueOrDefault(typeIndex) ??
                   throw new Exception($"TypeName by index '{typeIndex}' not found in '{module.Name}' module.");
        }

        public static List<VeinTypeArg> ReadGenericsTypeName(this BinaryReader bin, VeinModule module)
        {
            var size = bin.ReadInt32();
            var list = new List<VeinTypeArg>();

            var magic1 = bin.ReadInt64();

            if (magic1 != 228)
                throw new InvalidOperationException($"Magic number invalid");

            foreach (var _ in ..size)
                list.Add(bin.ReadGenericTypeName(module));


            var magic2 = bin.ReadInt64();

            if (magic2 != 428)
                throw new InvalidOperationException($"Magic number invalid");

            return list;
        }

        public static VeinTypeArg ReadGenericTypeName(this BinaryReader bin, VeinModule module)
        {
            var typeIndex = bin.ReadInt32();

            return module.generics_table.GetValueOrDefault(typeIndex) ??
                   throw new Exception($"VeinTypeArg by index '{typeIndex}' not found in '{module.Name}' module.");
        }

        public static nint ReadTypeName(this BinaryReader bin, TypeNameGetter getter)
        {
            var typeIndex = bin.ReadInt32();

            return getter(typeIndex);
        }

        public static void WriteTypeName(this BinaryWriter bin, QualityTypeName type, VeinModuleBuilder module)
        {
            var key = module.InternTypeName(type);

            bin.Write(key);
        }

        public static void WriteComplexType(this BinaryWriter bin, VeinComplexType type, VeinModuleBuilder module)
        {
            if (type.IsGenericInstance)
            {
                bin.Write(TS_INSTANCE);
                bin.WriteTypeName(type.Definition.FullName, module);
                bin.Write(type.TypeArguments.Count);
                foreach (var arg in type.TypeArguments)
                    bin.WriteComplexType(arg, module);
            }
            else if (type.IsGeneric)
            {
                bin.Write(TS_PARAM);
                bin.WriteGenericTypeName(type.TypeArg, module);
            }
            else
            {
                bin.Write(TS_CONCRETE);
                bin.WriteTypeName(((VeinClass)type).FullName, module);
            }
        }

        public static void WriteGenericsTypeName(this BinaryWriter bin, List<VeinTypeArg> types, VeinModuleBuilder module)
        {
            bin.Write(types.Count);

            bin.Write((long)228); // magic number

            foreach (var arg in types)
            {
                bin.WriteGenericTypeName(arg, module);
            }

            bin.Write((long)428); // magic number
        }

        public static void WriteGenericTypeName(this BinaryWriter bin, VeinTypeArg type, VeinModuleBuilder module)
        {
            var key = module.InternGenericTypeName(type);

            bin.Write(key);
        }

        public static void PutTypeName(this ILGenerator gen, QualityTypeName type)
        {
            Func<QualityTypeName, int> getConst = gen._methodBuilder.moduleBuilder.InternTypeName;

            var key = getConst(type);
            gen.PutInteger4(key);
        }

        public static void PutTypeArg(this ILGenerator gen, VeinTypeArg type)
        {
            Func<VeinTypeArg, int> getConst = gen._methodBuilder.moduleBuilder.InternGenericTypeName;

            var key = getConst(type);
            gen.PutInteger4(key);
        }

        public static void PutTypeNameInto(this ILGenerator gen, QualityTypeName type, BinaryWriter writer)
        {
            Func<QualityTypeName, int> getConst = gen._methodBuilder.moduleBuilder.InternTypeName;

            var key = getConst(type);
            writer.Write(key);
        }


        public static void WriteArguments(this BinaryWriter binary, VeinMethodSignature signature, VeinModuleBuilder module)
        {
            binary.Write(signature.ArgLength);
            foreach (var argument in signature.Arguments)
            {
                binary.Write(module.InternString(argument.Name));
                binary.WriteComplexType(argument.ComplexType, module);
            }
        }
    }
}
