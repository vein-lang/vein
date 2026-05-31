namespace vein.compilation;

using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    public void ValidateStruct((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax member)
            return;
        if (!member.IsStruct)
            return;

        var @class = x.@class;

        ValidateStructNoInheritance(@class, member);
        ValidateStructSealed(@class, member);
        ComputeStructLayout(@class, member);
    }

    private void ValidateStructNoInheritance(ClassBuilder @class, ClassDeclarationSyntax member)
    {
        foreach (var parent in @class.Parents)
        {
            // ValueType is the required base for all structs — always allowed
            if (parent.Name == NameSymbol.ValueType)
                continue;

            // Interfaces are allowed
            if (parent.IsInterface)
                continue;

            if (parent.IsStruct)
            {
                Log.Defer.Error(
                    $"Struct [red]'{@class.Name.name.EscapeMarkup()}'[/] cannot inherit from another struct [red]'{parent.Name.name.EscapeMarkup()}'[/]. Only interface implementations are allowed.",
                    member.Identifier, member.OwnerDocument);
            }
            else
            {
                Log.Defer.Error(
                    $"Struct [red]'{@class.Name.name.EscapeMarkup()}'[/] cannot inherit from class [red]'{parent.Name.name.EscapeMarkup()}'[/]. Structs can only implement interfaces.",
                    member.Identifier, member.OwnerDocument);
            }
        }
    }

    private void ValidateStructSealed(ClassBuilder @class, ClassDeclarationSyntax member)
    {
        if (@class.IsAbstract)
        {
            Log.Defer.Error(
                $"Struct [red]'{@class.Name.name.EscapeMarkup()}'[/] cannot be abstract.",
                member.Identifier, member.OwnerDocument);
        }
    }

    private void ComputeStructLayout(ClassBuilder @class, ClassDeclarationSyntax member)
    {
        @class.LayoutKind = VeinStructLayoutKind.Sequential;
        @class.PackSize = 0; // natural alignment

        var offset = 0;
        foreach (var field in @class.Fields.Where(f => !f.IsStatic))
        {
            var size = ComputeFieldSize(field);
            field.Size = size;

            // Align offset to field's natural alignment (min of field size and pointer size)
            var alignment = Math.Min(size, 8);
            if (alignment > 0)
                offset = (offset + alignment - 1) / alignment * alignment;

            field.Offset = offset;
            offset += size;
        }

        // Final struct size aligned to largest field alignment
        @class.StructSize = offset;
    }

    private int ComputeFieldSize(VeinField field)
    {
        var fieldClass = field.FieldType.Class;
        if (fieldClass is null)
            return 8; // pointer size fallback

        // Reference types always occupy pointer size in struct layout
        if (fieldClass.TypeCode is VeinTypeCode.TYPE_CLASS or VeinTypeCode.TYPE_STRING
            or VeinTypeCode.TYPE_OBJECT or VeinTypeCode.TYPE_ARRAY)
            return 8;

        // Primitives have known native sizes
        if (fieldClass.IsPrimitive)
            return fieldClass.TypeCode.GetNativeSize();

        // Nested bittable struct — use its computed size
        if (fieldClass.IsStruct)
            return fieldClass.StructSize > 0 ? fieldClass.StructSize : 8;

        // Fallback — pointer size
        return 8;
    }
}
