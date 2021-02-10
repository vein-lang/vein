namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class MethodBuilder : WaveMethod, IBaker
    {
        internal readonly ClassBuilder classBuilder;
        internal ModuleBuilder moduleBuilder 
            => classBuilder?.moduleBuilder;
        private readonly ILGenerator _generator;


        internal MethodBuilder(ClassBuilder clazz, string name, WaveType returnType, params WaveArgumentRef[] args)
        {
            classBuilder = clazz;
            ReturnType = returnType;
            Name = name;
            _generator = new ILGenerator(this);
            Arguments.AddRange(args);
        }

        public ILGenerator GetGenerator() => _generator;

        public byte[] BakeByteArray()
        {
            var idx = classBuilder.moduleBuilder.GetStringConstant(Name);
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);
            if (Flags.HasFlag(MethodFlags.Extern))
            {
                binary.Write(idx); // $method name
                binary.Write((byte)Flags); // $flags
                binary.Write(0); // body size
                binary.Write((byte)0); // stack size TODO
                binary.Write((byte)0); // locals size TODO
                binary.Write(classBuilder.moduleBuilder.GetTypeConstant(ReturnType.FullName));
                binary.Write(new byte[0]); // IL Body
                return mem.ToArray();
            }
            
            var body = _generator.BakeByteArray();
            
            binary.Write(idx); // $method name
            binary.Write((byte)Flags); // $flags
            binary.Write(body.Length); // body size
            binary.Write((byte)64); // stack size TODO
            binary.Write((byte)24); // locals size TODO
            binary.Write(classBuilder.moduleBuilder.GetTypeConstant(ReturnType.FullName));
            WriteArguments(binary);
            binary.Write(body); // IL Body

            return mem.ToArray();
        }
        
        private void WriteArguments(BinaryWriter binary)
        {
            binary.Write(ArgLength);
            foreach (var argument in Arguments)
            {
                binary.Write(moduleBuilder.GetStringConstant(argument.Name));
                binary.Write(moduleBuilder.GetTypeConstant(argument.Type.FullName));
            }
        }

        public string BakeDebugString()
        {
            var str = new StringBuilder();
            var args = Arguments.Select(x => $"{x.Name}: {x.Type.Name}").Join(',');
            if (Flags.HasFlag(MethodFlags.Extern))
            {
                str.AppendLine($".method extern {Name} ({args}) {Flags.EnumerateFlags().Join(' ').ToLowerInvariant()};");
                return str.ToString();
            }
            var body = _generator.BakeDebugString();
            
            str.AppendLine($".method '{Name}' ({args}) {Flags.EnumerateFlags().Join(' ').ToLowerInvariant()}");
            str.AppendLine("{");
            str.AppendLine($"\t.size {_generator.ILOffset}");
            str.AppendLine($"\t.maxstack 0x{64:X8}");
            str.AppendLine($"\t.locals 0x{24:X8}");
            str.AppendLine($"\t");
            str.AppendLine($"{body.Split("\n").Select(x => $"\t{x}").Join("\n")}");
            str.AppendLine("}");
            return str.ToString();
        }

        #region Arg&Locals manage (NEED REFACTORING)

        public ulong? FindArgumentField(FieldName @ref)
            => getArg(@ref)?.type?.Token?.Value;
        public int? GetArgumentIndex(FieldName @ref)
            => getArg(@ref)?.idx;

        public ulong? FindLocalField(FieldName @ref) =>
            getLocal(@ref)?.arg?.Token?.Value;
        public int? GetLocalIndex(FieldName @ref) 
            => getLocal(@ref)?.idx;
        private (int idx, WaveArgumentRef arg)? getLocal(FieldName @ref)
        {
            var (key, value) = Locals
                .FirstOrDefault(x
                    => x.Value.Name.Equals(@ref.Name, StringComparison.CurrentCultureIgnoreCase));
            return value != null ? (key, value) : default;
        }
        private (int idx, WaveArgumentRef type)? getArg(FieldName @ref)
        {
            var result = Arguments.Select((x, i) => (i, x)).FirstOrDefault(x => x.x.Name.Equals(@ref.Name, StringComparison.CurrentCultureIgnoreCase));
            return result.x is null ? default((int idx, WaveArgumentRef type)?) : result;
        }
        #endregion
    }
}