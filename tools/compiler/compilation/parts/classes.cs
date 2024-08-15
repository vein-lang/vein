namespace vein.compilation;

using exceptions;
using extensions;
using MoreLinq;
using vein.reflection;
using static runtime.MethodFlags;

public partial class CompilationTask
{
    public void LinkMetadata((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax clazz)
            return;

        var (@class, member) = (x.@class, clazz);
        var doc = member.OwnerDocument;
        @class.Flags = GenerateClassFlags(member);

        if (member.IsInterface)
        {
            @class.Flags |= ClassFlags.Abstract;
            @class.Flags |= ClassFlags.Interface;
            @class.Flags |= ClassFlags.Public;
        }

        var owners = member.Inheritances;

        // ignore core base types
        if (member.Identifier.ExpressionString is "Object" or "ValueType") // TODO
            return;
        if (!owners.Any() && !@class.Parents.Any())
        {
            // fallback transform
            if (member.IsStruct)
            {
                owners.Add(new TypeSyntax(NameSymbol.ValueType)
                    .SetPos<TypeSyntax>(member.Identifier.Transform));
            }
            else
            {
                owners.Add(new TypeSyntax(NameSymbol.Object)
                    .SetPos<TypeSyntax>(member.Identifier.Transform));
            }
        }

        owners
            .Select(owner => FetchType(@class, owner, doc))
            .Where(p => !@class.Parents.Contains(p))
            .Pipe(p => @class.Parents.Add(p)).Consume();
        @class
            .Parents
            .ToArray() // copy emumerable 
            .SelectMany(x => x.Parents)
            .Where(x => @class.Parents.Any(z => z.FullName == x.FullName))
            .Pipe(v => @class.Parents.Remove(v))
            .Consume();
    }
    private ClassFlags GenerateClassFlags(ClassDeclarationSyntax clazz)
    {
        var flags = (ClassFlags) 0;

        var annotations = clazz.Aspects;
        var mods = clazz.Modifiers;

        foreach (var annotation in annotations)
        {
            var kind = annotation;
            switch (annotation)
            {

                //Log.Defer.Error(
                //    $"Cannot apply [orange bold]annotation[/] [red bold]{kind}[/] to [orange]'{clazz.Identifier}'[/] " +
                //    $"class/struct/interface declaration.",
                //    clazz.Identifier, clazz.OwnerDocument);
                //continue;
                case { IsSpecial: true }:
                    flags |= ClassFlags.Special;
                    continue;
                case { IsNative: true }:
                case { IsForwarded: true }:
                case { IsAspectUsage: true }:
                    continue;
                //case VeinAnnotationKind.Readonly when !clazz.IsStruct:
                //    Log.Defer.Error(
                //        $"[orange bold]Aspect[/] [red bold]{kind}[/] can only be applied to a structure declaration.",
                //        clazz.Identifier, clazz.OwnerDocument);
                //    continue;
                //case VeinAnnotationKind.Readonly when clazz.IsStruct:
                //    // TODO
                //    continue;

                default:
                    var a = FindAspect(annotation, clazz.OwnerDocument);
                    if (a is not null)
                        continue;

                    Log.Defer.Error(
                        $"In [orange]'{clazz.Identifier}'[/] class/struct/interface [red bold]{kind}[/] " +
                        $"is not found [orange bold]aspect[/], maybe skip use?",
                        annotation, clazz.OwnerDocument);
                    continue;
            }
        }

        foreach (var mod in mods)
        {
            switch (mod.ModificatorKind.ToString().ToLower())
            {
                case "public":
                    flags |= ClassFlags.Public;
                    continue;
                case "private":
                    flags |= ClassFlags.Private;
                    continue;
                case "static":
                    flags |= ClassFlags.Static;
                    continue;
                case "internal":
                    flags |= ClassFlags.Internal;
                    continue;
                case "abstract":
                    flags |= ClassFlags.Abstract;
                    continue;
                default:
                    Log.Defer.Error($"In [orange]'{clazz.Identifier}'[/] class/struct/interface " +
                                    $"[red bold]{mod.ModificatorKind}[/] is not supported [orange bold]modificator[/].",
                        clazz.Identifier, clazz.OwnerDocument);

                    continue;
            }
        }

        return flags;
    }

    public List<(ClassBuilder clazz, MemberDeclarationSyntax member)>
        LinkClasses((FileInfo, DocumentDeclaration doc) tuple)
        => LinkClasses(tuple.doc, Types.Storage);

    public void GenerateLinksForAliases(DocumentDeclaration doc)
    {

    }

    private Queue<(DocumentDeclaration doc, VeinCore types)> aliasesQueue { get; } = new();

    public List<(ClassBuilder clazz, MemberDeclarationSyntax member)> LinkClasses(DocumentDeclaration doc, VeinCore types)
    {
        var classes = new List<(ClassBuilder clazz, MemberDeclarationSyntax member)>();

        foreach (var member in doc.Members)
        {
            if (member is ClassDeclarationSyntax clazz)
            {
                Status.VeinStatus($"Regeneration class [grey]'{clazz.Identifier.ExpressionString.EscapeMarkup()}'[/]");
                clazz.OwnerDocument = doc;
                var result = CompileClass(clazz, doc, types);

                Debug.WriteLine($"compiled class '{result.FullName.ToString().EscapeMarkup()}'");

                Context.Classes.Add(result.FullName, result);
                classes.Add((result, clazz));
            }
            else if (member is AspectDeclarationSyntax aspect)
            {
                Status.VeinStatus($"Regeneration aspect [grey]'{aspect.Identifier}'[/]");
                aspect.OwnerDocument = doc;
                var result = CompileAspect(aspect, doc);
                Context.Classes.Add(result.FullName, result);
                classes.Add((result, aspect));
            }
            else
                Log.Defer.Warn($"[grey]Member[/] [yellow underline]'{member.GetType().Name.EscapeMarkup()}'[/] [grey]is not supported.[/]", member, doc);
        }

        if (doc.Aliases.Any()) aliasesQueue.Enqueue((doc, types));

        return classes;
    }

    private List<(ClassBuilder clazz, MemberDeclarationSyntax member)> RegenerateAliases(
        (DocumentDeclaration doc, VeinCore types) data)
        => RegenerateAliases(data.doc, data.types);

    private List<(ClassBuilder clazz, MemberDeclarationSyntax member)> RegenerateAliases(DocumentDeclaration doc, VeinCore types)
    {
        var classes = new List<(ClassBuilder clazz, MemberDeclarationSyntax member)>();
        foreach (var alias in doc.Aliases)
        {
            if (alias.IsType)
            {
                var type = module.FindType(new NameSymbol(alias.Type!.Typeword.Identifier), doc.Includes);
                var fullName = new QualityTypeName(
                    new NameSymbol(alias.AliasName.ExpressionString),
                    new NamespaceSymbol(doc.Name),
                    module.Name);
                Context.Module.alias_table.Add(new VeinAliasType(fullName, type));

                Status.VeinStatus($"Regeneration type alias [grey]'{type.FullName.ToString().EscapeMarkup()}'[/] -> [grey]'{alias.AliasName.ExpressionString.EscapeMarkup()}'[/]");

                KnowClasses.Add(fullName, type);
            }
            else
            {
                var delegateClass = DefineDelegateClass(alias, doc, types);

                Status.VeinStatus($"Regeneration method alias [grey]'{delegateClass.FullName.ToString().EscapeMarkup()}'[/]");

                delegateClass.TypeCode = VeinTypeCode.TYPE_FUNCTION;
                Context.Classes.Add(delegateClass.FullName, delegateClass);
                classes.Add((delegateClass, null!));
            }
        }

        return classes;
    }

    public ClassBuilder DefineDelegateClass(AliasSyntax alias, DocumentDeclaration doc, VeinCore types)
    {
        var aliasName = new QualityTypeName(new NameSymbol(alias.AliasName.ExpressionString), new NamespaceSymbol(doc.Name),  module.Name);
        var multicastFnType = new QualityTypeName(NameSymbol.FunctionMulticast, NamespaceSymbol.Std, ModuleNameSymbol.Std);

        var @base = module.FindType(multicastFnType, true);

        var clazz = module.DefineClass(aliasName, @base)
            .WithIncludes(doc.Includes);

        alias.MethodDeclaration!.OwnerDocument = doc;

        var args = GenerateArgument(clazz, alias.MethodDeclaration!, doc);

        var retType = FetchType(clazz, alias.MethodDeclaration!.ReturnType, doc);
        var sig = new VeinMethodSignature(retType, args, new List<VeinTypeArg>() /*todo*/);
        Context.Module.alias_table.Add(new VeinAliasMethod(aliasName, sig));

        
        clazz.TypeCode = VeinTypeCode.TYPE_FUNCTION;

        var objType = VeinTypeCode.TYPE_OBJECT.AsClass(types);
        var rawType = VeinTypeCode.TYPE_RAW.AsClass(types);

        var ctorMethod = clazz.DefineMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR, clazz, [
            new VeinArgumentRef(VeinArgumentRef.THIS_ARGUMENT, clazz),
            new("fn", rawType),
            new("scope", objType)
        ]);


        var scope = clazz.DefineField("_scope", FieldFlags.Internal, objType);
        var ptrRef = clazz.DefineField("_fn", FieldFlags.Internal, rawType);

        var ctorGen = ctorMethod.GetGenerator();

        ctorGen.Emit(OpCodes.LDARG_1); // load ref
        ctorGen.Emit(OpCodes.LDARG_0); // load this
        ctorGen.Emit(OpCodes.STF, ptrRef); // this.ptrRef = ref;
        ctorGen.Emit(OpCodes.LDARG_2); // load scope
        ctorGen.Emit(OpCodes.LDARG_0); // load this
        ctorGen.Emit(OpCodes.STF, scope); // this.scope = scope;
        ctorGen.Emit(OpCodes.LDARG_0); // load this
        ctorGen.Emit(OpCodes.RET); // return this


        var method = clazz.DefineMethod("invoke", Internal | Special,
            sig.ReturnType, new List<VeinArgumentRef> { new (VeinArgumentRef.THIS_ARGUMENT, clazz) }.Concat(sig.Arguments.Where(VeinMethodSignature.NotThis)).ToArray());

        var hasNotThis = sig.Arguments.All(VeinMethodSignature.NotThis);

        var generator = method.GetGenerator();


        if (!hasNotThis)
        {
            generator.EmitThis();
            generator.Emit(OpCodes.LDF, scope);
        }
        foreach (int i in ..method.Signature.Arguments.Where(VeinMethodSignature.NotThis).Count())
            generator.EmitLoadArgument(i + 1);

        generator.EmitThis();
        generator.Emit(OpCodes.LDF, ptrRef);
        generator.Emit(OpCodes.CALL_SP);
        generator.Emit(OpCodes.RET);

        KnowClasses.Add(aliasName, clazz);
        return clazz;
    }

    public ClassBuilder CompileClass(ClassDeclarationSyntax member, DocumentDeclaration doc, VeinCore types)
    {
        void _defineClass(ClassBuilder clz) => KnowClasses.Add(clz.FullName, clz);


        if (member.IsForwardedType)
        {
            var result = types.All.
                FirstOrDefault(x =>
                    x.FullName.Name.name.Equals(member.Identifier.ExpressionString));

            if (result is not null)
            {
                var clz = new ClassBuilder(module, result);
                module.class_table.Add(clz);

                clz.Includes.AddRange(doc.Includes);
                TypeForwarder.Indicate(types, clz);
                CompileAspectFor(member, doc, clz);
                _defineClass(clz);
                return clz;
            }

            throw new ForwardedTypeNotDefinedException(member.Identifier.ExpressionString);
        }

        
        var clazz = module.DefineClass(member.ClassName, doc.Namespace)
            .WithIncludes(doc.Includes);

        if (member.IsGeneric)
            GenerateGenerics(member, clazz, doc);

        _defineClass(clazz);
        CompileAspectFor(member, doc, clazz);
        return clazz;
    }

    public void GenerateGenerics(ClassDeclarationSyntax astClass, ClassBuilder clazz, DocumentDeclaration doc)
    {
        var list = new List<VeinTypeArg>();

        foreach (var type in astClass.GenericTypes)
        {
            var @const = astClass.TypeParameterConstraints
                .Where(x => x.GenericIndex.Typeword.Identifier == type.Typeword.Identifier)
                .ToList();

            list.Add(@const.Count == 0
                ? new VeinTypeArg(type.Typeword.Identifier, new List<VeinBaseConstraint>())
                : new VeinTypeArg(type.Typeword.Identifier, ConvertConstraints(@const, doc)));
        }

        clazz.TypeArgs.AddRange(list);
    }

    public List<VeinBaseConstraint> ConvertConstraints(List<TypeParameterConstraintSyntax> constraints, DocumentDeclaration doc)
    {
        var list = new List<VeinBaseConstraint>();

        foreach (var constraint in constraints)
        {
            if (constraint.IsBittable)
                list.Add(new VeinBaseConstraintConstBittable());
            else if (constraint.IsClass)
                list.Add(new VeinBaseConstraintConstClass());
            else 
                list.Add(new VeinBaseConstraintConstType(module.FindType(new NameSymbol(constraint.Constraint.Typeword.Identifier), doc.Includes)));
        }

        return list;
    }
}
