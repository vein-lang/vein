namespace wave.emit
{
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

        internal MethodBuilder(ClassBuilder clazz, string name)
        {
            classBuilder = clazz;
            _name = name;
            _generator = new ILGenerator(this);
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
    }
}