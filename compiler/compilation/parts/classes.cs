namespace vein.compilation;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using exceptions;
using ishtar;
using ishtar.emit;
using MoreLinq;
using runtime;
using syntax;

public partial class CompilationTask
{
    public void LinkMetadata((ClassBuilder @class, MemberDeclarationSyntax member) x)
    {
        if (x.member is not ClassDeclarationSyntax)
            return;

        var (@class, member) = (x.@class, x.member as ClassDeclarationSyntax);
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
                case { IsAlias: true }:
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

        return classes;
    }

    public ClassBuilder CompileClass(ClassDeclarationSyntax member, DocumentDeclaration doc, VeinCore types)
    {
        void _defineClass(ClassBuilder clz)
        {
            KnowClasses.Add(member.Identifier, clz);
            if (member.Aspects.FirstOrDefault(x => x.IsAlias)?.Args?.SingleOrDefault().Value is not StringLiteralExpressionSyntax alias)
                return;
            KnowClasses.Add(new IdentifierExpression(alias.Value), clz);
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
