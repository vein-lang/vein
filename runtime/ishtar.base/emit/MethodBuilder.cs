namespace ishtar.emit;

using System;
using System.IO;
using System.Linq;
using System.Text;
using extensions;
using vein.extensions;
using vein.runtime;
using static vein.runtime.MethodFlags;

public class MethodBuilder : VeinMethod, IBaker
{
    internal readonly ClassBuilder classBuilder;
    internal VeinModuleBuilder moduleBuilder
        => classBuilder?.moduleBuilder;
    private readonly ILGenerator _generator;


    internal MethodBuilder(ClassBuilder clazz, string name, VeinClass returnType, params VeinArgumentRef[] args)
        : base(name, 0, returnType, clazz, args)
    {
        classBuilder = clazz;
        Owner = clazz;
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
        if (IsExtern || IsAbstract)
        {
            binary.Write(idx); // $method name
            binary.Write((short)Flags); // $flags
            binary.Write(0); // body size
            binary.Write((byte)0); // stack size
            binary.Write((byte)0); // locals size
            binary.WriteTypeName(ReturnType.FullName, moduleBuilder);
            binary.WriteArguments(Signature, moduleBuilder);
            binary.Write(Array.Empty<byte>()); // IL Body
            return mem.ToArray();
        }

        var body = _generator.BakeByteArray();

        binary.Write(idx); // $method name
        binary.Write((short)Flags); // $flags
        binary.Write(body.Length); // body size
        binary.Write(_generator.GetStackSize());
        binary.Write((byte)_generator.LocalsSize); // locals size
        binary.WriteTypeName(ReturnType.FullName, moduleBuilder);
        binary.WriteArguments(Signature, moduleBuilder);
        binary.Write(body); // IL Body

        return mem.ToArray();
    }

    /// <summary>
    /// Bake debug view of byte code.
    /// </summary>
    public string BakeDebugString()
    {
        var str = new StringBuilder();
        var args = Signature.ToTemplateString();
        if (Flags.HasFlag(Extern))
        {
            if (Signature.IsGeneric)
                str.AppendLine($".method extern {RawName}[{Signature.Arguments.Where(x => x.IsGeneric).Select(x => x.TypeArg!.ToTemplateString()).Join(',')}] {args} {Flags.EnumerateFlags([None, Extern]).Join(' ').ToLowerInvariant()}");
            else
                str.AppendLine($".method extern {RawName} {args} {Flags.EnumerateFlags([None, Extern]).Join(' ').ToLowerInvariant()}");
            return str.ToString();
        }
        var body = _generator.BakeDebugString();

        foreach (var exClass in _generator.GetEffectedExceptions())
            str.AppendLine($"@effect {exClass.FullName.NameWithNS};");

        str.AppendLine($".method {(IsSpecial ? "special " : "")}'{RawName}' [{Signature.Arguments.Where(x => x.IsGeneric).Select(x => x.TypeArg!.ToTemplateString()).Join(',')}] {args} {Flags.EnumerateFlags([None, Extern]).Join(' ').ToLowerInvariant()}");
        if (Flags.HasFlag(Abstract))
            return str.ToString();
        str.AppendLine("{");
        str.AppendLine($"\t.size {_generator.ILOffset}");
        str.AppendLine($"\t.maxstack 0x{_generator.GetStackSize():X8}");
        str.AppendLine($"\t.locals 0x{_generator.LocalsSize:X8}");
        if (!string.IsNullOrEmpty(body))
        {
            str.AppendLine($"\t");
            str.AppendLine($"{body.Split("\n").Select(x => $"\t{x}").Join("\n").TrimEnd('\n')}");
        }
        str.AppendLine("}");
        return str.ToString();
    }

    #region Arg&Locals manage (NEED REFACTORING)

    internal int? GetArgumentIndex(FieldName @ref)
        => getArg(@ref)?.idx;

    internal int? GetLocalIndex(FieldName @ref)
        => getLocal(@ref)?.idx;
    private (int idx, VeinArgumentRef arg)? getLocal(FieldName @ref)
    {
        var (key, value) = Locals
            .FirstOrDefault(x
                => x.Value.Name.Equals(@ref.Name, StringComparison.CurrentCultureIgnoreCase));
        return value != null ? (key, value) : default;
    }
    private (int idx, VeinArgumentRef type)? getArg(FieldName @ref)
    {
        var result = Signature.Arguments.Select((x, i) => (i, x)).FirstOrDefault(x => x.x.Name.Equals(@ref.Name, StringComparison.CurrentCultureIgnoreCase));
        return result.x is null ? default((int idx, VeinArgumentRef type)?) : result;
    }
    #endregion

    public override string ToString() => $"{RawName}{Signature.ToTemplateString()}";
}
