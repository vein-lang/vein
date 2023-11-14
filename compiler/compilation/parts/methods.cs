namespace vein.compilation;

using ishtar.emit;
using System.Collections.Generic;
using ishtar;
using Spectre.Console;
using vein.runtime;
using vein.syntax;
using static runtime.VeinTypeCode;

public partial class CompilationTask
{
    private MethodFlags GenerateMethodFlags(MethodDeclarationSyntax method)
    {
        var flags = (MethodFlags)0;

        var aspects = method.Aspects;
        var mods = method.Modifiers;

        foreach (var aspect in aspects)
        {
            switch (aspect)
            {
                //case VeinAnnotationKind.Virtual:
                //    flags |= MethodFlags.Virtual;
                //    continue;
                case { IsSpecial: true }:
                    flags |= MethodFlags.Special;
                    continue;
                case { IsNative: true }:
                    continue;
                //case VeinAnnotationKind.Readonly:
                //case VeinAnnotationKind.Getter:
                //case VeinAnnotationKind.Setter:
                //    Log.Defer.Error(
                //        $"In [orange]'{method.Identifier}'[/] method [red bold]{annotation.Name}[/] " +
                //        $"is not supported [orange bold]annotation[/].",
                //        method.Identifier, method.OwnerClass.OwnerDocument);
                //    continue;
                default:
                    var a = FindAspect(aspect, method.OwnerDocument);
                    if (a is not null)
                        continue;

                    Log.Defer.Error(
                        $"In [orange]'{method.Identifier}'[/] aspect [red bold]{aspect.Name}[/] " +
                        $"not found, maybe skip use?.",
                        aspect, method.OwnerClass.OwnerDocument);
                    continue;
            }
        }

        foreach (var mod in mods)
        {
            switch (mod.ModificatorKind.ToString().ToLower())
            {
                case "public":
                    flags |= MethodFlags.Public;
                    continue;
                case "extern":
                    flags |= MethodFlags.Extern;
                    continue;
                case "private":
                    flags |= MethodFlags.Private;
                    continue;
                case "static":
                    flags |= MethodFlags.Static;
                    continue;
                case "protected":
                    flags |= MethodFlags.Protected;
                    continue;
                case "internal":
                    flags |= MethodFlags.Internal;
                    continue;
                case "override":
                    flags |= MethodFlags.Override;
                    continue;
                case "virtual":
                    flags |= MethodFlags.Virtual;
                    continue;
                case "abstract":
                    flags |= MethodFlags.Virtual;
                    continue;
                default:
                    Log.Defer.Error(
                        $"In [orange]'{method.Identifier}'[/] method [red bold]{mod.ModificatorKind}[/] " +
                        $"is not supported [orange bold]modificator[/].",
                        method.Identifier, method.OwnerClass.OwnerDocument);
                    continue;
            }
        }


        if (flags.HasFlag(MethodFlags.Private) && flags.HasFlag(MethodFlags.Public))
            Log.Defer.Error(
                $"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] " +
                $"in [orange]'{method.Identifier}'[/] method.",
                method.ReturnType, method.OwnerClass.OwnerDocument);


        return flags;
    }

    public (
           List<(MethodBuilder method, MethodDeclarationSyntax syntax)> methods,
           List<(VeinField field, FieldDeclarationSyntax syntax)> fields,
           List<(VeinProperty field, PropertyDeclarationSyntax syntax)> props)
           LinkMethods((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        var methods = new List<(MethodBuilder method, MethodDeclarationSyntax syntax)>();
        var fields = new List<(VeinField field, FieldDeclarationSyntax syntax)>();
        var props = new List<(VeinProperty field, PropertyDeclarationSyntax syntax)>();

        if (x.member is not ClassDeclarationSyntax clazzSyntax)
            return (methods, fields, props);
        var (@class, _) = x;
        var doc = clazzSyntax.OwnerDocument;

        foreach (var member in clazzSyntax.Members)
        {
            switch (member)
            {
                case IPassiveParseTransition transition when member.IsBrokenToken:
                    var e = transition.Error;
                    //var pos = member.Transform.pos;
                    //var err_line = member.Transform.DiffErrorFull(doc);
                    //errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/] \n\t" +
                    //           $"at '[orange bold]{pos.Line} line, {pos.Column} column[/]' \n\t" +
                    //           $"in '[orange bold]{doc.FileEntity}[/]'." +
                    //           $"{err_line}");
                    Log.Defer.Error($"[red bold]{e.Message.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/]", member, doc);
                    break;
                case MethodDeclarationSyntax method:
                    Status.VeinStatus($"Regeneration method [grey]'{method.Identifier}'[/]");
                    method.OwnerClass = clazzSyntax;
                    methods.Add(CompileMethod(method, @class, doc));
                    break;
                case FieldDeclarationSyntax field:
                    Status.VeinStatus($"Regeneration field [grey]'{field.Field.Identifier}'[/]");
                    field.OwnerClass = clazzSyntax;
                    fields.Add(CompileField(field, @class, doc));
                    break;
                case PropertyDeclarationSyntax prop:
                    Status.VeinStatus($"Regeneration property [grey]'{prop.Identifier}'[/]");
                    prop.OwnerClass = clazzSyntax;
                    props.Add(CompileProperty(prop, @class, doc));
                    break;
                default:
                    Log.Defer.Warn($"[grey]Member[/] '[yellow underline]{member.GetType().Name}[/]' [grey]is not supported.[/]", member, doc);
                    break;
            }
        }
        return (methods, fields, props);
    }

    public (MethodBuilder method, MethodDeclarationSyntax syntax)
        CompileMethod(MethodDeclarationSyntax member, ClassBuilder clazz, DocumentDeclaration doc)
    {
        var retType = member.ReturnType.IsSelf ? clazz :
             FetchType(member.ReturnType, doc);
        member.OwnerDocument = doc;
        if (retType is null)
            return default;

        var args = GenerateArgument(member, doc);

        if (member.IsConstructor())
        {
            member.Identifier = new IdentifierExpression("ctor");

            if (args.Length == 0)
                return (clazz.GetDefaultCtor() as MethodBuilder, member);
            var ctor = clazz.DefineMethod("ctor", GenerateMethodFlags(member), clazz, args);
            CompileAspectFor(member, doc, ctor);
            return (ctor, member);
        }

        var method = clazz.DefineMethod(member.Identifier.ExpressionString, GenerateMethodFlags(member), retType, args);
        
        if (clazz.IsInterface)
        {
            method.Flags |= MethodFlags.Abstract;
            method.Flags |= MethodFlags.Public;
        }

        CompileAspectFor(member, doc, method);

        return (method, member);
    }
}
