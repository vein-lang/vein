namespace wave.compilation
{
    using emit;
    using Spectre.Console;
    using stl;
    using syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using static emit.MethodFlags;
    using static Spectre.Console.AnsiConsole;

    public class Compiler
    {
        public static Compiler Process(FileInfo[] entity)
        {
            var c = new Compiler();
            
            return Status()
                .Spinner(Spinner.Known.Dots8Bit)
                .Start("Processing...", ctx =>
                {
                    c.ctx = ctx;
                    try
                    {
                        c.ProcessFiles(entity);
                    }
                    catch (Exception e)
                    {
                        MarkupLine("failed compilation.");
                        WriteException(e);
                    }
                    return c;
                });
        }

        private readonly WaveSyntax syntax = new();
        private StatusContext ctx;
        private readonly Dictionary<FileInfo, string> Sources = new ();
        private readonly Dictionary<FileInfo, DocumentDeclaration> Ast = new();
        public readonly List<string> warnings = new ();
        public readonly List<string> errors = new ();
        private WaveModuleBuilder module;

        private void ProcessFiles(FileInfo[] files)
        {
            foreach (var file in files)
            {
                ctx.WaveStatus($"Read [grey]'{file.Name}'[/]...");
                Sources.Add(file, File.ReadAllText(file.FullName));
            }

            foreach (var (key, value) in Sources)
            {
                ctx.WaveStatus($"Compile [grey]'{key.Name}'[/]...");
                try
                {
                    var result = syntax.CompilationUnit.ParseWave(value);
                    result.FileEntity = key;
                    Ast.Add(key, result);
                }
                catch (WaveParseException e)
                {
                    errors.Add($"[red bold]{e.Message.Trim().EscapeMarkup()}[/] \n\t" +
                               $"at '[orange bold]{e.Position.Line} line, {e.Position.Column} column[/]' \n\t" +
                               $"in '[orange bold]{key}[/]'.");
                }
            }

            module = new WaveModuleBuilder("wcorlib");
            
            foreach (var (key, value) in Ast)
            {
                ctx.WaveStatus($"Linking [grey]'{key.Name}'[/]...");
                CompileInto(value);
            }
        }
        public void CompileInto(DocumentDeclaration doc)
        {
            foreach (var member in doc.Members)
            {
                if (member is ClassDeclarationSyntax clazz)
                {
                    ctx.WaveStatus($"Regeneration class [grey]'{clazz.Identifier}'[/]");
                    CompileInto(clazz, doc);
                }
                else
                    warnings.Add($"[grey]Member[/] [yellow underline]'{member.GetType().Name}'[/] [grey]is not supported.[/]");
            }
        }

        public void CompileInto(ClassDeclarationSyntax member, DocumentDeclaration doc)
        {
            var clazz = module.DefineClass($"global::{doc.Name}/{member.Identifier}");

            var owner = member.Inheritance.FirstOrDefault();

            if (member.Identifier != "Object")
            {
                // TODO
                owner ??= new TypeSyntax("Object");

                clazz.Parent = FetchType(owner, doc)?.AsClass();

                if (clazz.Parent is null)
                    return;
            }

            foreach (var memberMember in member.Members)
            {
                if (memberMember is MethodDeclarationSyntax method)
                {
                    ctx.WaveStatus($"Regeneration method [grey]'{method.Identifier}'[/]");
                    CompileInto(method, clazz, doc);
                }
                else
                    warnings.Add($"[grey]Member[/] '[yellow underline]{memberMember.GetType().Name}[/]' [grey]is not supported.[/]");
            }
        }

        public void CompileInto(MethodDeclarationSyntax member, WaveClass clazz, DocumentDeclaration doc)
        {
            var retType = FetchType(member.ReturnType, doc);

            if (retType is null)
                return;

            var args = GenerateArgument(member, doc);
            
            var method = clazz.DefineMethod(member.Identifier, retType, GenerateMethodFlags(member), args);
        }


        private WaveType FetchType(TypeSyntax typename, DocumentDeclaration doc)
        {
            ctx.WaveStatus("Collecting metadata...");
            var retType = module.TryFindType(typename.Identifier, doc.Includes);

            if (retType is null) 
                errors.Add($"[red bold]Cannot resolve type[/] '[purple underline]{typename.Identifier}[/]' \n\t" +
                           $"at '[orange bold]{typename.transform.pos.Line} line, {typename.transform.pos.Column} column[/]' \n\t" +
                           $"in '[orange bold]{doc.FileEntity}[/]'.");
            return retType;
        }


        private WaveArgumentRef[] GenerateArgument(MethodDeclarationSyntax method, DocumentDeclaration doc)
        {
            if (method.Parameters.Count == 0)
                return Array.Empty<WaveArgumentRef>();
            return method.Parameters.Select(parameter => new WaveArgumentRef
                {Type = FetchType(parameter.Type, doc), Name = parameter.Identifier})
                .ToArray();
        }

        private MethodFlags GenerateMethodFlags(MethodDeclarationSyntax method)
        {
            var flags = (MethodFlags)0;

            var annotation = method.Annotations;
            var mods = method.Modifiers;

            foreach (var kind in annotation)
            {
                switch (kind)
                {
                    case WaveAnnotationKind.Virtual:
                        flags &= Virtual;
                        continue;
                    case WaveAnnotationKind.Special:
                    case WaveAnnotationKind.Native:
                        continue;
                    case WaveAnnotationKind.Readonly:
                    case WaveAnnotationKind.Getter:
                    case WaveAnnotationKind.Setter:
                        errors.Add($"In [orange]'{method.Identifier}'[/] method [red bold]{kind}[/] is not supported [orange bold]annotation[/].");
                        continue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var mod in mods)
            {
                switch (mod)
                {
                    case "public":
                        flags &= Public;
                        continue;
                    case "extern":
                        flags &= Extern;
                        continue;
                    case "private":
                        flags &= Private;
                        continue;
                    case "static":
                        flags &= Static;
                        continue;
                    case "protected":
                        flags &= Protected;
                        continue;
                    case "internal":
                        flags &= Internal;
                        continue;
                    default:
                        errors.Add($"In [orange]'{method.Identifier}'[/] method [red bold]{mod}[/] is not supported [orange bold]modificator[/].");
                        continue;
                }
            }


            if (flags.HasFlag(Private) && flags.HasFlag(Public))
                errors.Add($"Modificator [red bold]public[/] cannot be combined with [red bold]private[/] in [orange]'{method.Identifier}'[/] method.");


            return flags;
        }
    }

    public static class WaveStatusContextEx
    {
        /// <summary>Sets the status message.</summary>
        /// <param name="context">The status context.</param>
        /// <param name="status">The status message.</param>
        /// <returns>The same instance so that multiple calls can be chained.</returns>
        public static StatusContext WaveStatus(this StatusContext context, string status)
        {
            if (context == null)
                throw new ArgumentNullException(nameof (context));
            Thread.Sleep(400); // so, i need it :(
            context.Status = status;
            return context;
        }
    }
}