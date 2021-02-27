namespace wave.emit.extensions
{
    using System;
    using System.IO;

    public static class QualityTypeEx
    {
        public static QualityTypeName ReadTypeName(this BinaryReader bin, WaveModule module)
        {
            var asmIdx = bin.ReadInt32();
            var nameIdx = bin.ReadInt32();
            var nsIdx = bin.ReadInt32();
            
            return QualityTypeName.Construct(asmIdx, nameIdx, nsIdx, module);
        }
        
        public static void WriteTypeName(this BinaryWriter bin, QualityTypeName type, WaveModuleBuilder module)
        {
            bin.Write(module.GetStringConstant(type.AssemblyName));
            bin.Write(module.GetStringConstant(type.Name));
            bin.Write(module.GetStringConstant(type.Namespace));
        }
        
        public static void PutTypeName(this ILGenerator gen, QualityTypeName type)
        {
            Func<string, int> getConst = gen._methodBuilder.moduleBuilder.GetStringConstant;

            var asmIdx = getConst(type.AssemblyName);
            var nameIdx = getConst(type.Name);
            var nsIdx = getConst(type.Namespace);
            
            gen.PutInteger4(asmIdx);
            gen.PutInteger4(nameIdx);
            gen.PutInteger4(nsIdx);
        }
    }
}