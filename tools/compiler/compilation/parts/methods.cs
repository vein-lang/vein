namespace vein.compilation;

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
                        aspect, method!.OwnerClass!.OwnerDocument);
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
                    flags |= MethodFlags.Abstract;
                    continue;
                case "async":
                    flags |= MethodFlags.Async;
                    continue;
                default:
                    Log.Defer.Error(
                        $"In [orange]'{method.Identifier}'[/] method [red bold]{mod.ModificatorKind}[/] " +
                        $"is not supported [orange bold]modificator[/].",
                        method.Identifier, method!.OwnerClass!.OwnerDocument);
                    continue;
            }
        }


        if (flags.HasFlag(MethodFlags.Private) && flags.HasFlag(MethodFlags.Public))
            Log.Defer.Error(
                $"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] " +
                $"in [orange]'{method.Identifier}'[/] method.",
                method.ReturnType, method.OwnerClass!.OwnerDocument);
        if (flags.HasFlag(MethodFlags.Async) && !method.ReturnType.IsAsyncJob)
            Log.Defer.Error(
                $"Method [orange]'{method.Identifier}'[/] has marked as [red bold]async[/] " +
                $"but return type is not [red bold]Job[/] ",
                method.ReturnType, method.OwnerClass!.OwnerDocument);

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
                    Log.Defer.Error($"[red bold]{e!.Message!.Trim().EscapeMarkup()}, expected {e.FormatExpectations().EscapeMarkup().EscapeArgumentSymbols()}[/]", member, doc);
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
        // A method type-parameter used as the return type (e.g. `id<T>(...): T`) must bind to the method's
        // own generic parameter — FetchType only knows class parameters, so resolve it here like args.cs does.
        var returnGeneric = member.GenericTypes.FirstOrDefault(x => x.Typeword.Equals(member.ReturnType));
        VeinComplexType retType = member.ReturnType.IsSelf ? clazz
            : returnGeneric is not null ? returnGeneric.Typeword.ToTypeArg([])
            : FetchType(clazz, member.ReturnType, doc);
        member.OwnerDocument = doc;

        if (retType is null)
            return default;

        var args = GenerateArgument(clazz, member, doc);

        if (member.IsConstructor())
        {
            member.Identifier = new IdentifierExpression(VeinMethod.METHOD_NAME_CONSTRUCTOR);
            if (args.Length == 0)
            {
                if (clazz.GetDefaultCtor() is MethodBuilder ctorBuilder)
                    return (ctorBuilder, member);
                throw new InvalidOperationException();
            }
            var ctor = clazz.DefineMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR, GenerateMethodFlags(member), clazz, args);
            CompileAspectFor(member, doc, ctor);
            return (ctor, member);
        }

        var methodGenerics = GenerateMethodGenerics(member, doc);
        var method = clazz.DefineMethod(member.Identifier.ExpressionString, GenerateMethodFlags(member), retType, methodGenerics, args);

        if (clazz.IsInterface)
            method.Flags |= MethodFlags.Abstract | MethodFlags.Public;

        if (retType.IsGeneric || args.Any(x => x.IsGeneric) || methodGenerics.Count > 0)
            method.Flags |= MethodFlags.Generic;

        CompileAspectFor(member, doc, method);

        return (method, member);
    }

    /// <summary>
    /// Converts a method's declared type parameters (<c>foo&lt;T, U&gt;</c>) into ordered
    /// <see cref="VeinTypeArg"/>s carrying any <c>when T is ...</c> constraints, mirroring the class-level
    /// <c>GenerateGenerics</c>. The result is stored on the method signature so the body can resolve the
    /// parameters as types and call sites can substitute them.
    /// </summary>
    public List<VeinTypeArg> GenerateMethodGenerics(MethodDeclarationSyntax member, DocumentDeclaration doc)
    {
        var list = new List<VeinTypeArg>();

        foreach (var type in member.GenericTypes)
        {
            var constraints = member.TypeParameterConstraints
                .Where(x => x.GenericIndex.Typeword.Identifier == type.Typeword.Identifier)
                .ToList();

            list.Add(constraints.Count == 0
                ? new VeinTypeArg(type.Typeword.Identifier, new List<VeinBaseConstraint>())
                : new VeinTypeArg(type.Typeword.Identifier, ConvertConstraints(constraints, doc)));
        }

        return list;
    }
}
