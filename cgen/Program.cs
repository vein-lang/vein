using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ishtar;
using lang.c;
using LLVMSharp.Interop;
using vein;
using vein.runtime;
using static ishtar.libuv.LibUV;

using var writer = new StreamWriter("../../../../include/ishtar.h");

var asm = typeof(VirtualMachine).Assembly;


CTypes.OverridesTypes.Add(typeof(LLVMModuleRef), "void*");
CTypes.OverridesTypes.Add(typeof(LLVMValueRef), "void*");
CTypes.OverridesTypes.Add(typeof(LLVMExecutionEngineRef), "void*");
CTypes.OverridesTypes.Add(typeof(uv_thread_t), "void*");
CTypes.OverridesTypes.Add(typeof(uv_sem_t), "void*");
CTypes.OverridesTypes.Add(typeof(GCHandle), "void*");
CTypes.OverridesTypes.Add(typeof(SmartPointer<>), "void*");
CTypes.OverridesTypes.Add(typeof(SmartPointer<stackval>), "void*");

var license =
    """
    /* Copyright Yuuki Wesp and other Vein Runtime contributors. All rights reserved.
    *
    * Permission is hereby granted, free of charge, to any person obtaining a copy
    * of this software and associated documentation files (the "Software"), to
    * deal in the Software without restriction, including without limitation the
    * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
    * sell copies of the Software, and to permit persons to whom the Software is
    * furnished to do so, subject to the following conditions:
    *
    * The above copyright notice and this permission notice shall be included in
    * all copies or substantial portions of the Software.
    *
    * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
    * IN THE SOFTWARE.
    */
    """;

writer.WriteLine(license);



writer.WriteLine("");
writer.WriteLine("#ifndef ISHTAR_H");
writer.WriteLine("#define ISHTAR_H");

writer.WriteLine("");
writer.WriteLine($"#define ISHTAR_VERSION \"{GlobalVersion.FullSemVer}\"");
writer.WriteLine($"#define ISHTAR_VERSION_MAJOR {GlobalVersion.Major}");
writer.WriteLine($"#define ISHTAR_VERSION_MINOR {GlobalVersion.Minor}");
writer.WriteLine($"#define ISHTAR_VERSION_PATH {GlobalVersion.Patch}");
writer.WriteLine($"#define ISHTAR_VERSION_COMMIT_SHA \"{GlobalVersion.Sha}\"");
writer.WriteLine($"#define ISHTAR_VERSION_BRANCH \"{GlobalVersion.BranchName}\"");
writer.WriteLine($"#define ISHTAR_VERSION_COMMIT_DATE \"{GlobalVersion.CommitDate}\"");
writer.WriteLine("");


writer.WriteLine("#ifdef __cplusplus");
writer.WriteLine("extern \"C\" {");
writer.WriteLine("#endif");

writer.WriteLine("#include <stdint.h>");
writer.WriteLine("");


writer.WriteLine("typedef struct decimal_t { uint64_t h; uint64_t l; };");
writer.WriteLine("typedef uint16_t half_t;");

GenerateEnumDeclarations(asm, writer);
GenerateEnumDeclarations(typeof(MethodFlags).Assembly, writer);
GenerateEnumDeclarations(typeof(OpCodeValue).Assembly, writer);
GenerateStructDeclarations(asm, writer);

while (CTypes.RequiredTypeToGen.TryDequeue(out var type))
{
    try
    {
        GenerateStruct(type, writer);
    }
    catch
    {
    }
}

GenerateFunctionPrototypes(asm, writer);


writer.WriteLine("#ifdef __cplusplus");
writer.WriteLine("}");
writer.WriteLine("#endif");
writer.WriteLine("#endif /*ISHTAR_H*/");

static void GenerateFunctionPrototypes(Assembly assembly, StreamWriter writer)
{
    var methods = assembly.GetTypes()
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        .Where(m => m.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null);

    foreach (var method in methods)
    {
        var returnType = method.ReturnType.ToCType(true);
        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.ToCType(true)} {p.Name}"));
        writer.WriteLine($"extern {returnType} {method.Name}({parameters});");
    }
}

static void GenerateEnumDeclarations(Assembly assembly, StreamWriter writer)
{
    var enumTypes = assembly.GetTypes()
        .Where(t => t.IsEnum);

    foreach (var enumType in enumTypes)
    {
        try
        {
            GenerateEnum(enumType, writer);
        }
        catch (Exception e)
        {
            
        }
    }
} static void GenerateStructDeclarations(Assembly assembly, StreamWriter writer)
{
    var types = assembly.GetTypes()
        .Where(t => t.IsValueType);

    foreach (var type in types)
    {
        if (type.IsEnum)
        {
            Console.WriteLine($"Skipped type '{type}' because it enum");
            continue;
        }
        if (type.IsGenericType)
        {
            Console.WriteLine($"Skipped type '{type}' because it IsGenericType");
            continue;
        }
        if (!IsUnmanaged(type))
        {
            Console.WriteLine($"Skipped type '{type}' because it not unmanaged");
            continue;
        }
        try
        {
            GenerateStruct(type, writer);
        }
        catch (Exception e)
        {
            
        }
    }
}
static bool IsUnmanaged(Type type)
{
    // primitive, pointer or enum -> true
    if (type.IsPrimitive || type.IsPointer || type.IsFunctionPointer || type.IsEnum)
        return true;
    // not a struct -> false
    if (!type.IsValueType)
        return false;
    // otherwise check recursively
    return type
        .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        .Where(x => !x.FieldType.Name.StartsWith("SmartPointer"))
        .All(f => IsUnmanaged(f.FieldType));
}

static void GenerateEnum(Type enumType, StreamWriter streamWriter)
{
    var underlyingType = enumType.ToCType(false);
    streamWriter.WriteLine($"typedef enum {underlyingType} {{");

    var enumValues = Enum.GetValues(enumType).Cast<Enum>();
    foreach (var value in enumValues)
    {
        var name = value.ToString();
        var intValue = Convert.ChangeType(value, Enum.GetUnderlyingType(enumType));
        streamWriter.WriteLine($"    {ToSnakeCase(name.ToLowerInvariant()).ToUpperInvariant()} = {intValue},");
    }

    streamWriter.WriteLine($"}};");
    streamWriter.WriteLine("");
}
static void GenerateStruct(Type type, StreamWriter streamWriter)
{
    if (type.Name.StartsWith("<>"))
    {
        Console.WriteLine($"Skipped type '{type}' because it hidden type");
        return;
    }
    if (type.Namespace!.StartsWith("System"))
    {
        Console.WriteLine($"Skipped type '{type}' because it system type");
        return;
    }
    if (CTypes.ProcessedType.Contains(type))
    {
        Console.WriteLine($"Skipped type '{type}' because it already processed");
        return;
    }
    if (type.IsGenericType)
    {
        Console.WriteLine($"Skipped type '{type}' because it generic type");
        return;
    }
    if (type.IsEnum)
    {
        Console.WriteLine($"Skipped type '{type}' because it enum type");
        return;
    }
    CTypes.ProcessedType.Add(type);
    if (type.UnderlyingSystemType == typeof(SmartPointer<>))
        return;

    var exportName = type.GetCustomAttribute<CTypeExport>()?.Name ?? type.Name;

    streamWriter.WriteLine($"typedef struct {exportName} {{");

    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    var groupedFields = fields.GroupBy(f => f.GetCustomAttribute<FieldOffsetAttribute>()?.Value ?? -1);

    foreach (var group in groupedFields)
    {
        if (group.Key == -1)
        {
            foreach (var field in group)
            {
                var fieldName = GetFieldName(field);
                streamWriter.WriteLine($"    {field.FieldType.ToCType(true)} {fieldName};");
            }
        }
        else
        {
            streamWriter.WriteLine($"    union {{ /* Offset: {group.Key} */");
            foreach (var field in group)
            {
                var fieldName = GetFieldName(field);
                streamWriter.WriteLine($"        {field.FieldType.ToCType(true)} {fieldName};");
            }
            streamWriter.WriteLine($"    }};");
        }
    }

    streamWriter.WriteLine($"}} {exportName};");
    streamWriter.WriteLine("");
}

static string GetFieldName(FieldInfo field)
{
    var fieldName = field.Name;
    if (fieldName.Contains("k__BackingField"))
        fieldName = fieldName.Replace("k__BackingField", "").Trim('<', '>');
    if (fieldName.Contains(">P"))
        fieldName = fieldName.Replace(">P", "").Trim('<', '>');

    fieldName = ToSnakeCase(fieldName);

    if (fieldName.Equals("register"))
        fieldName = $"_{fieldName}";
    if (fieldName.Equals("class"))
        fieldName = $"_{fieldName}";
    if (fieldName.Equals("union"))
        fieldName = $"_{fieldName}";

    return fieldName;
}


static string ToSnakeCase(string text)
{
    if (text == null)
    {
        throw new ArgumentNullException(nameof(text));
    }
    if (text.Length < 2)
    {
        return text.ToLowerInvariant();
    }
    var sb = new StringBuilder();
    sb.Append(char.ToLowerInvariant(text[0]));
    for (int i = 1; i < text.Length; ++i)
    {
        char c = text[i];
        if (char.IsUpper(c))
        {
            sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        else
        {
            sb.Append(c);
        }
    }
    return sb.ToString();
}
