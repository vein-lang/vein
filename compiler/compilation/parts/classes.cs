namespace vein.compilation;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using exceptions;
using extensions;
using ishtar;
using ishtar.emit;
using MoreLinq;
using runtime;
using syntax;
using vein.reflection;
using static runtime.MethodFlags;

public partial class CompilationTask
{
    public void LinkMetadata((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax clazz)
            return;

        var (@class, member) = (x.@class, xMember: clazz);
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
                owners.Add(new TypeSyntax(new IdentifierExpression("ValueType"))
                    .SetPos<TypeSyntax>(member.Identifier.Transform));
            }
            else
            {
                owners.Add(new TypeSyntax(new IdentifierExpression("Object"))
                    .SetPos<TypeSyntax>(member.Identifier.Transform));
            }
        }

        owners
            .Select(owner => FetchType(owner, doc))
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
                Status.VeinStatus($"Regeneration class [grey]'{clazz.Identifier}'[/]");
                clazz.OwnerDocument = doc;
                var result = CompileClass(clazz, doc, types);

                Debug.WriteLine($"compiled class '{result.FullName}'");

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
                Log.Defer.Warn($"[grey]Member[/] [yellow underline]'{member.GetType().Name}'[/] [grey]is not supported.[/]");
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
                var type = FetchType(alias.Type!.Typeword, doc);
                Context.Module.alias_table.Add(new VeinAliasType($"{module.Name}%global::{doc.Name}/{alias.AliasName.ExpressionString}",
                type));

                Status.VeinStatus($"Regeneration type alias [grey]'{type}'[/] -> [grey]'{alias.AliasName.ExpressionString}'[/]");

                KnowClasses.Add(alias.AliasName, type);
            }
            else
            {
                var delegateClass = DefineDelegateClass(alias, doc, types);

                Status.VeinStatus($"Regeneration method alias [grey]'{delegateClass.FullName}'[/]");

                delegateClass.TypeCode = VeinTypeCode.TYPE_FUNCTION;
                Context.Classes.Add(delegateClass.FullName, delegateClass);
                classes.Add((delegateClass, null!));
            }
        }

        return classes;
    }

    public ClassBuilder DefineDelegateClass(AliasSyntax alias, DocumentDeclaration doc, VeinCore types)
    {
        var aliasName = new QualityTypeName(module.Name, alias.AliasName.ExpressionString, $"global::{doc.Name}");
        var multicastFnType = new QualityTypeName("std", "FunctionMulticast", $"global::std");

        var args = GenerateArgument(alias.MethodDeclaration!, doc);

        var retType = FetchType(alias.MethodDeclaration!.ReturnType, doc);
        var sig = new VeinMethodSignature(retType, args);
        Context.Module.alias_table.Add(new VeinAliasMethod(aliasName, sig));

        var @base = module.FindType(multicastFnType, true);

        var clazz = module.DefineClass(aliasName, @base)
            .WithIncludes(doc.Includes);


        clazz.TypeCode = VeinTypeCode.TYPE_FUNCTION;

        var objType = VeinTypeCode.TYPE_OBJECT.AsClass(types);
        var rawType = VeinTypeCode.TYPE_RAW.AsClass(types);

        var ctorMethod = clazz.DefineMethod(VeinMethod.METHOD_NAME_CONSTRUCTOR, VeinTypeCode.TYPE_VOID.AsClass(types), [
            new("fn", rawType),
            new("scope", objType)
        ]);


        var scope = clazz.DefineField("_scope", FieldFlags.Internal, objType);
        var ptrRef = clazz.DefineField("_fn", FieldFlags.Internal, rawType);

        var ctorGen = ctorMethod.GetGenerator();

        ctorGen.Emit(OpCodes.LDARG_0);
        ctorGen.Emit(OpCodes.STF, ptrRef);
        ctorGen.Emit(OpCodes.LDARG_1);
        ctorGen.Emit(OpCodes.STF, scope);
        ctorGen.Emit(OpCodes.RET);

        var method = clazz.DefineMethod("invoke", Internal | Special,
            sig.ReturnType,sig.Arguments.Where(VeinMethodSignature.NotThis).ToArray());

        var hasThis = sig.Arguments.All(VeinMethodSignature.NotThis);

        var generator = method.GetGenerator();


        if (hasThis)
            generator.Emit(OpCodes.LDF, scope);
        foreach (int i in ..method.Signature.ArgLength)
            generator.Emit(OpCodes.LDARG_S, i); // TODO optimization for LDARG_X

        generator.Emit(OpCodes.LDF, ptrRef);
        generator.Emit(OpCodes.CALL_SP);
        generator.Emit(OpCodes.RET);


        KnowClasses.Add(alias.AliasName, clazz);
        return clazz;
    }

    public ClassBuilder CompileClass(ClassDeclarationSyntax member, DocumentDeclaration doc, VeinCore types)
    {
        void _defineClass(ClassBuilder clz)
        {
            KnowClasses.Add(member.Identifier, clz);
        }


        if (member.IsForwardedType)
        {
            var result = types.All.
                FirstOrDefault(x =>
                    x.FullName.Name.Equals(member.Identifier.ExpressionString));

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

        var clazz = module.DefineClass($"global::{doc.Name}/{member.Identifier.ExpressionString}")
            .WithIncludes(doc.Includes);
        _defineClass(clazz);
        CompileAspectFor(member, doc, clazz);
        return clazz;
    }
}
