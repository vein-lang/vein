namespace wave.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;

    public class MethodBuilder
    {
        internal readonly ClassBuilder classBuilder;
        private readonly string _name;
        private MethodFlags _flags;
        private readonly ILGenerator _generator;
        private readonly List<WaveArgumentRef> _args = new();
        private readonly Dictionary<int, WaveArgumentRef> _locals = new();

        internal MethodBuilder(ClassBuilder clazz, string name, params WaveArgumentRef[] args)
        {
            classBuilder = clazz;
            _name = name;
            _generator = new ILGenerator(this);
            this._args.AddRange(args);
        }

        public void SetFlags(MethodFlags flags) => _flags = flags;


        public ILGenerator GetGenerator() => _generator;
        
        internal byte[] BakeByteArray()
        {
            if (_generator.ILOffset == 0 && !_flags.HasFlag(MethodFlags.Extern))
                return null;
            var idx = classBuilder.moduleBuilder.GetStringConstant(_name);
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);
            var body = _generator.BakeByteArray();
            
            binary.Write(idx); // $method name
            binary.Write((byte)_flags); // $flags
            binary.Write(body.Length); // body size
            binary.Write((byte)64); // stack size TODO
            binary.Write((byte)24); // locals size TODO
            binary.Write(body); // IL Body

            return mem.ToArray();
        }
        
        internal string BakeDebugString()
        {
            if (_generator.ILOffset == 0 && !_flags.HasFlag(MethodFlags.Extern))
                return "<empty>";
            var body = _generator.BakeDebugString();
            var str = new StringBuilder();

            str.AppendLine($".method {_name} {_flags.EnumerateFlags().Join(' ').ToLowerInvariant()}");
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
            var (key, value) = _locals
                .FirstOrDefault(x
                    => x.Value.Name.Equals(@ref.name, StringComparison.CurrentCultureIgnoreCase));
            return value != null ? (key, value) : default;
        }
        private (int idx, WaveArgumentRef type)? getArg(FieldName @ref)
        {
            var result = _args.Select((x, i) => (i, x)).FirstOrDefault(x => x.x.Name.Equals(@ref.name, StringComparison.CurrentCultureIgnoreCase));
            if (result.x is null)
                return default;
            return result;
        }
        #endregion
    }
}