namespace vein.compilation;

using ishtar.emit;
using runtime;
using syntax;
using System;

public partial class CompilationTask
{
    public (VeinProperty prop, PropertyDeclarationSyntax member)
        CompileProperty(PropertyDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
    {
        var propType = member.Type.IsSelf ?
            clazz :
            FetchType(clazz, member.Type, doc);

        if (propType is null)
        {
            Log.Defer.Error($"[red bold]Unknown type detected. Can't resolve [italic]{member.Type.Identifier}[/][/] \n\t" +
                            PleaseReportProblemInto(),
                member.Identifier, doc);
            return default;
        }

        if (member is { Setter: { IsEmpty: true }, Getter: { IsEmpty: true } })
            return (clazz.DefineAutoProperty(member.Identifier, GenerateFieldFlags(member), propType), member);
        return (clazz.DefineEmptyProperty(member.Identifier, GenerateFieldFlags(member), propType), member);
    }

    public void GenerateProp((VeinProperty prop, PropertyDeclarationSyntax member) t)
    {
        if (t == default) return;

        var (prop, member) = t;
        var doc = member.OwnerClass.OwnerDocument;

        if (prop.Owner is not ClassBuilder clazz)
        {
            Log.Defer.Error($"[red bold]Internal error in[/] [orange bold]GenerateProp[/]\n\t{PleaseReportProblemInto()}",
                member, doc);
            return;
        }

        if (prop.Setter is not null || prop.Getter is not null)
            return; // skip auto property (already generated).

        VeinArgumentRef[] getArgList(bool isSetter)
        {
            var val_ref = new VeinArgumentRef("value", prop.PropType);
            var this_ref = new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, prop.Owner);
            if (prop.IsStatic && !isSetter)
                return Array.Empty<VeinArgumentRef>();
            if (prop.IsStatic && isSetter)
                return [val_ref];
            if (!prop.IsStatic && isSetter)
                return [this_ref, val_ref];
            if (!prop.IsStatic && !isSetter)
                return [this_ref];
            throw new ArgumentException();
        }

        if (member.Setter is not null)
        {
            var args = getArgList(true);
            prop.Setter = clazz.DefineMethod($"set_{prop.Name}",
                VeinProperty.ConvertShadowFlags(prop.Flags), prop.PropType, args);

            GenerateBody((MethodBuilder)prop.Setter, member.Setter.Body, doc);
        }

        if (member.Getter is not null || member.IsShortform())
        {
            var args = getArgList(false);
            prop.Getter = clazz.DefineMethod($"get_{prop.Name}",
                VeinProperty.ConvertShadowFlags(prop.Flags), prop.PropType, args);

            if (member.Getter is not null)
                GenerateBody((MethodBuilder)prop.Getter, member.Getter.Body, doc);
            if (member.IsShortform())
                GenerateBody((MethodBuilder)prop.Getter, new(new ReturnStatementSyntax(member.Expression)), doc);
        }
    }
}
