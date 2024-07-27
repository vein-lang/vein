namespace lang.c;


[AttributeUsage(AttributeTargets.Class)]
public class CHeaderInclude(string header) : Attribute
{
    public string Header { get; } = header;
}

[AttributeUsage(AttributeTargets.Class)]
public class CHeaderExport(string name) : Attribute
{
    public string Name { get; } = name;
}


[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum)]
public class CTypeExport(string name) : Attribute
{
    public string Name { get; } = name;
}


[AttributeUsage(AttributeTargets.Field)]
public class CTypeOverride(string name) : Attribute
{
    public string Name { get; } = name;
}


[AttributeUsage(AttributeTargets.Field)]
public class CIfDefined(string name) : Attribute
{
    public string Name { get; } = name;
}


public static class CTypes
{
    public static readonly Dictionary<Type, string> OverridesTypes = new();
    public static readonly Queue<Type> RequiredTypeToGen = new();
    public static readonly HashSet<Type> RequiredTypeToGen_hashset = new();
    public static readonly List<Type> ProcessedType = new();

    public static void RequireType(Type t)
    {
        RequiredTypeToGen.Enqueue(t);
        RequiredTypeToGen_hashset.Add(t);
    }

    public static void ScanToType(Type t)
    {
        foreach (var field in t.GetFields())
        {
            var el = field.FieldType!;
            if (field.FieldType.IsPointer)
            {
                el = field.FieldType.GetElementType();
            }

            if (ProcessedType.Contains(el))
                continue;
            if (RequiredTypeToGen_hashset.Add(el))
                RequiredTypeToGen.Enqueue(el);
        }
    }

    public static string ToCType(this Type type, bool allowDowngrade)
    {
        if (OverridesTypes.TryGetValue(type, out string? cType))
            return cType;
        if (type.GetCustomAttributes(typeof(CTypeExport), false).FirstOrDefault() is CTypeExport exportType)
            return exportType.Name;
        if (type == typeof(void)) return "void";
        if (type == typeof(int)) return "int32_t";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "const char*";
        if (type == typeof(short)) return "int16_t";
        if (type == typeof(long)) return "int64_t";
        if (type == typeof(ulong)) return "uint64_t";
        if (type == typeof(uint)) return "uint32_t";
        if (type == typeof(ushort)) return "uint16_t";
        if (type == typeof(nint)) return "size_t";
        if (type == typeof(nuint)) return "usize_t";
        if (type == typeof(byte)) return "uint8_t";
        if (type == typeof(sbyte)) return "int8_t";
        if (type == typeof(Half)) return "half_t";
        if (type == typeof(decimal)) return "decimal_t";
        if (type.IsPointer)
        {
            var el = type.GetElementType();
            if (el is null)
                return "void*";
            ScanToType(el);

            if (el.IsGenericType)
                return "void*";
            if (el.GetCustomAttributes(typeof(CTypeExport), false).FirstOrDefault() is CTypeExport pe)
                 return $"{pe.Name}*";
            return "void*";
        }
        if (type.IsFunctionPointer)
            return GetFunctionPointerType(type);

        ScanToType(type);

        if (type is { IsValueType: true, IsPrimitive: false, IsEnum: false })
            return type.Name;

        if (allowDowngrade && type.IsEnum)
        {
            return type.GetEnumUnderlyingType().ToCType(false);
        }

        
        throw new NotSupportedException($"Type {type.FullName} is not support");
    }

    private static string GetFunctionPointerType(Type type)
    {
        return "void*";
        //var methodSig = type.GetMethod("Invoke");
        //var returnType = methodSig.ReturnType.ToCType(false);
        //var parameterTypes = string.Join(", ", methodSig.GetParameters().Select(p => p.ParameterType.ToCType(false)));
        //return $"{returnType} (*)({parameterTypes})";
    }
}
