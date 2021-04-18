namespace wave.ishtar.emit.extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using wave.runtime;

    public static class QualityTypeEx
    {
        public static QualityTypeName ReadTypeName(this BinaryReader bin, WaveModule module)
        {
            var typeIndex = bin.ReadInt32();
            
            return module.types_table.GetValueOrDefault(typeIndex) ?? 
                   throw new Exception($"TypeName by index '{typeIndex}' not found in '{module.Name}' module.");
        }
        
        public static void WriteTypeName(this BinaryWriter bin, QualityTypeName type, WaveModuleBuilder module)
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
    }
}