namespace mana.ishtar.emit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using extensions;
    using mana.extensions;
    using mana.runtime;
    using static runtime.MethodFlags;

    public class MethodBuilder : ManaMethod, IBaker
    {
        internal readonly ClassBuilder classBuilder;
        internal ManaModuleBuilder moduleBuilder
            => classBuilder?.moduleBuilder;
        private readonly ILGenerator _generator;


        internal MethodBuilder(ClassBuilder clazz, string name, ManaClass returnType, params ManaArgumentRef[] args)
            : base(name, 0, returnType, clazz, args)
        {
            classBuilder = clazz;
            _generator = new ILGenerator(this);
            clazz.moduleBuilder.InternString(Name);
        }
        /// <summary>
        /// Get body <see cref="ILGenerator"/>.
        /// </summary>
        public ILGenerator GetGenerator() => _generator;
        /// <summary>
        /// Bake byte code.
        /// </summary>
        public byte[] BakeByteArray()
        {
            var idx = classBuilder.moduleBuilder.InternString(Name);
            using var mem = new MemoryStream();
            using var binary = new BinaryWriter(mem);
            if (Flags.HasFlag(Extern))
            {
                binary.Write(idx); // $method name
                binary.Write((short)Flags); // $flags
                binary.Write(0); // body size
                binary.Write((byte)0); // stack size TODO
                binary.Write((byte)0); // locals size
                binary.WriteTypeName(ReturnType.FullName, moduleBuilder);
                WriteArguments(binary);
                binary.Write(new byte[0]); // IL Body
                return mem.ToArray();
            }

            var body = _generator.BakeByteArray();

            binary.Write(idx); // $method name
            binary.Write((short)Flags); // $flags
            binary.Write(body.Length); // body size
            binary.Write((byte)64); // stack size TODO
            binary.Write((byte)_generator.LocalsSize); // locals size
            binary.WriteTypeName(ReturnType.FullName, moduleBuilder);
            WriteArguments(binary);
            binary.Write(body); // IL Body

            return mem.ToArray();
        }

        private void WriteArguments(BinaryWriter binary)
        {
            binary.Write(ArgLength);
            foreach (var argument in Arguments)
            {
                binary.Write(moduleBuilder.InternString(argument.Name));
                binary.WriteTypeName(argument.Type.FullName, moduleBuilder);
            }
        }
        /// <summary>
        /// Bake debug view of byte code.
        /// </summary>
        public string BakeDebugString()
        {
            var str = new StringBuilder();
            var args = Arguments.Select(x => $"{x.Name}: {x.Type.Name}").Join(", ");
            if (Flags.HasFlag(Extern))
            {
                str.Append($".method extern {RawName} ({args}) {Flags.EnumerateFlags().Except(new[] { None, Extern }).Join(' ').ToLowerInvariant()}");
                str.AppendLine($" -> {ReturnType.FullName.Name};");
                return str.ToString();
            }
            var body = _generator.BakeDebugString();

            str.Append($".method {(IsSpecial ? "special" : "")} '{RawName}' ({args}) {Flags.EnumerateFlags().Except(new[] { None, Extern }).Join(' ').ToLowerInvariant()}");
            str.AppendLine($" -> {ReturnType.FullName.Name}");
            str.AppendLine("{");
            str.AppendLine($"\t.size {_generator.ILOffset}");
            str.AppendLine($"\t.maxstack 0x{64:X8}");
            str.AppendLine($"\t.locals 0x{_generator.LocalsSize:X8}");
            str.AppendLine($"\t");
            str.AppendLine($"{body.Split("\n").Select(x => $"\t{x}").Join("\n").TrimEnd('\n')}");
            str.AppendLine("}");
            return str.ToString();
        }

        #region Arg&Locals manage (NEED REFACTORING)

        internal int? GetArgumentIndex(FieldName @ref)
            => getArg(@ref)?.idx;

        internal int? GetLocalIndex(FieldName @ref)
            => getLocal(@ref)?.idx;
        private (int idx, ManaArgumentRef arg)? getLocal(FieldName @ref)
        {
            var (key, value) = Locals
                .FirstOrDefault(x
                    => x.Value.Name.Equals(@ref.Name, StringComparison.CurrentCultureIgnoreCase));
            return value != null ? (key, value) : default;
        }
        private (int idx, ManaArgumentRef type)? getArg(FieldName @ref)
        {
            var result = Arguments.Select((x, i) => (i, x)).FirstOrDefault(x => x.x.Name.Equals(@ref.Name, StringComparison.CurrentCultureIgnoreCase));
            return result.x is null ? default((int idx, ManaArgumentRef type)?) : result;
        }
        #endregion
    }
}
