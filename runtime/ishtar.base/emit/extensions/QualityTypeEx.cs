namespace ishtar.emit.extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using vein.runtime;


    public delegate nint TypeNameGetter(int typeIndex);
    public static class QualityTypeEx
    {
        public static QualityTypeName ReadTypeName(this BinaryReader bin, VeinModule module)
        {
            var typeIndex = bin.ReadInt32();

            return module.types_table.GetValueOrDefault(typeIndex) ??
                   throw new Exception($"TypeName by index '{typeIndex}' not found in '{module.Name}' module.");
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

        public static void PutTypeName(this ILGenerator gen, QualityTypeName type)
        {
            Func<QualityTypeName, int> getConst = gen._methodBuilder.moduleBuilder.InternTypeName;

            var key = getConst(type);
            gen.PutInteger4(key);
        }

        public static void PutTypeNameInto(this ILGenerator gen, QualityTypeName type, BinaryWriter writer)
        {
            Func<QualityTypeName, int> getConst = gen._methodBuilder.moduleBuilder.InternTypeName;

            var key = getConst(type);
            writer.Write(key);
        }
    }
}
